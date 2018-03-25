/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017-2018 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System;
using System.Collections.Generic;

using Rhino.Geometry;

using Path = System.Collections.Generic.List<tas.Machine.Waypoint>;

namespace tas.Machine.Posts
{
    /// <summary>
    /// Post to convert to G-code for CMS 5-axis mill.
    /// </summary>
    public class CMSPost : MachinePost
    {

        #region CMS Antares G and M code descriptions

        //G CODES
        //   G0     - Rapid
        //   G1     - Feed
        //   G2     - Arc CW
        //   G3     - Arc CCW
        //   G40    - Cutter compensation OFF
        //   G43.4  - Cutter height offset (?)
        //   G49    - Cutter length offset OFF
        //   G5.1   - High speed
        //   G52    - Work coordinates offset
        //   G53    - Move in absolute coordinates
        //   G68.2  - Tilted work planes (https://www.linkedin.com/pulse/fanuc-g682-5-axis-tilted-work-planes-tim-markoski/)
        //   G69    - Coordinate rotation OFF
        //   G80    - Cancel canned cycles
        //   G90    - Absolute distance mode (using active coordinate system)
        //   G92.1  - Reset G92 offsets to 0

        //M CODES
        //   M05    - Spindle rotation stop
        //   M25    - Pressure clamping 1 closing
        //   M3     - Spindle CW rotation
        //   M31    - B axis unlocking
        //   M32    - B axis locking
        //   M33    - C axis unlocking
        //   M34    - C axis locking
        //   M6     - Tool-change activation
        //   M99    - End of subroutine
        #endregion

        public override object Compute()
        {
            List<string> Tools = new List<string>();
            for (int i = 0; i < Paths.Count; ++i)
            {
                if (!Tools.Contains(Paths[i].ToolName))
                    Tools.Add(Paths[i].ToolName);
            }

            List<string> Program = new List<string>();
            /*
            // Dummy variables
            string Name = "Test";
            string Author = "Tom";
            string Date = System.DateTime.Now.ToShortDateString();
            string ProgramTime = "Not too long";
            double MaterialWidth = 200, MaterialHeight = 500, MaterialDepth = 25;
            */

            bool HighSpeed = true;

            // Create headers
            Program.Add("%");
            Program.Add("O0001");
            Program.Add($"(File name      : {Name} )");
            Program.Add($"(Programmed by  : {Author} )");
            Program.Add($"(Date           : {Date} )");
            Program.Add($"(Program length : {ProgramTime} )");
            Program.Add($"(Material       : W{MaterialWidth} H{MaterialHeight} D{MaterialDepth} )");
            Program.Add("");
            Program.Add("");

            // Comment on tools
            Program.Add("( * * * * * TOOLS * * * * * )");
            Program.Add($"( Number ; Diameter ; Length ; Name )");
            for (int i = 0; i < Tools.Count; ++i)
            {
                double ToolDiameter = 12.0;
                double ToolLength = 55.0;

                Program.Add($"( {i} ; {ToolDiameter} ; {ToolLength} ; {Tools[i]} )");
            }

            Program.Add("");
            Program.Add("");

            Program.Add("( * * * * * START * * * * * )");

            // Init gcode
            Program.Add("G90 G40 G80 G49 G69");

            // Reset offsets to 0
            Program.Add("G92.1 X0 Y0 Z0 B0 C0");

            // CMS pressure clamping closing
            Program.Add("M25");

            // Set to mm (G20 is inches)
            Program.Add("G21");

            // Go home. G0 = rapid, G53 = move in absolute coords
            Program.Add("G0 G53 Z0");
            Program.Add("G0 B0 C0");

            // TODO: Check out the G codes in here...

            for (int i = 0; i < Paths.Count; ++i)
            {
                Toolpath TP = Paths[i];

                // Add toolpath info
                // TODO: Add support for tool indexing to the whole thing
                Program.Add("");
                Program.Add("");
                Program.Add($"( * * * * * PATH {i:D2} * * * * * )");

                Program.Add($"( Operation : {TP.Name} )");
                Program.Add($"( Tool no.  : {Tools.IndexOf(TP.ToolName)} )");
                Program.Add($"( Tool des. : {TP.ToolName} )");
                Program.Add($"( Tool dia. : {TP.ToolDiameter} )");

                // Tool change
                Program.Add($"M6 T{Tools.IndexOf(TP.ToolName)}");

                if (HighSpeed)
                    Program.Add("G5.1 Q1");

                Program.Add($"G43.4 H{Tools.IndexOf(TP.ToolName)}");


                // If toolpath is planar, lock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M32 M34");

                // Start spindle
                Program.Add($"M3 S{TP.SpindleSpeed}");
                Program.Add("");

                // Move to first waypoint
                Waypoint prev = TP.Paths[0][0];
                if (prev.Type != (int)WaypointType.RAPID)
                    throw new Exception("First waypoint must be rapid. Check code.");

                // Calculate B and C values
                double B, C, prevB, prevC;
                Vector3d axisFirst = -prev.Plane.ZAxis;
                axisFirst.Unitize();

                prevB = Rhino.RhinoMath.ToDegrees(Math.Acos(axisFirst * -Vector3d.ZAxis));
                prevC = Rhino.RhinoMath.ToDegrees(Math.Atan2(axisFirst.X, axisFirst.Y));

                Program.Add($"G{(int)prev.Type} X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3}  Z{prev.Plane.Origin.Z:F3} B{prevB:F3} C{prevC:F3}");


                // Go through waypoints

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    Path Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        Waypoint wp = Subpath[k];

                        // Calculate B and C values
                        Vector3d axis = -wp.Plane.ZAxis;
                        axis.Unitize();

                        B = Rhino.RhinoMath.ToDegrees(Math.Acos(axis * -Vector3d.ZAxis));
                        C = Rhino.RhinoMath.ToDegrees(Math.Atan2(axis.X, axis.Y));

                        // Compose line
                        List<string> Line = new List<string>();

                        if (wp.Type != prev.Type)
                        {
                            if ((wp.Type & 1) == 1)
                                Line.Add($"G0");
                            else if ((wp.Type & 4) == 1)
                                if ((wp.Type & 12) == 1)
                                    Line.Add($"G3");
                                else
                                    Line.Add($"G2");
                            else
                                Line.Add($"G1");

                            /*
                            switch ((WaypointType)wp.Type)
                            {
                                case (WaypointType.RAPID):
                                    Line.Add($"G0");
                                    break;
                                case (WaypointType.FEED):
                                    Line.Add($"G1");
                                    break;
                                case (WaypointType.PLUNGE):
                                    Line.Add($"G1");
                                    break;
                                case (WaypointType.ARC_CW):
                                    Line.Add($"G2");
                                    break;
                                case (WaypointType.ARC_CCW):
                                    Line.Add($"G3");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            */
                        }

                        if (Math.Abs(wp.Plane.Origin.X - prev.Plane.Origin.X) > 0.00001)
                            Line.Add($"X{wp.Plane.Origin.X:F3}");

                        if (Math.Abs(wp.Plane.Origin.Y - prev.Plane.Origin.Y) > 0.00001)
                            Line.Add($"Y{wp.Plane.Origin.Y:F3}");

                        if (Math.Abs(wp.Plane.Origin.Z - prev.Plane.Origin.Z) > 0.00001)
                            Line.Add($"Z{wp.Plane.Origin.Z:F3}");

                        if (Math.Abs(B - prevB) > 0.00001)
                            Line.Add($"B{B:F3}");

                        if (Math.Abs(C - prevC) > 0.00001)
                            Line.Add($"C{C:F3}");

                        if ((wp.Type & 4) == 1)
                            Line.Add($"R0.0");

                        if ((wp.Type & 3) != (prev.Type & 3))
                            if ((wp.Type & 2) != 0)
                                Line.Add($"F{TP.PlungeRate}");
                            else if ((wp.Type & 2) == 0)
                                Line.Add($"F{TP.FeedRate}");

                        // Add line to program
                        Program.Add(string.Join(" ", Line));

                        // Update previous waypoint
                        prev = new Waypoint(wp);
                        prevB = B;
                        prevC = C;
                    }
                }

                // Stop spindle
                Program.Add("M5");


                // TODO: Find out what these G codes do
                Program.Add("G49 G53 G69");

                if (HighSpeed)
                    Program.Add("G5.1 Q0");

                // TODO: Add another home position here?

                // If toolpath is planar, unlock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M31 M33");


                Program.Add("");

            }

            // Return home
            // TODO: Look at example, find out if G53 is global coords
            // and add if necessary
            Program.Add("G0 Z0");
            Program.Add("G0 X-2600 Y0");

            Program.Add("( * * * * *  END  * * * * * )");

            Program.Add("M7"); // This should be to return the tool, but check
            Program.Add("");
            Program.Add("M99");
            Program.Add("%");


            return Program;
        }
    }
}