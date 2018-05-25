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
using System.Linq;
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

        #region Aarhus variables

        public double MaterialThickness;
        public bool HighSpeed = true;
        public double SikkerZ = -50;
        public double SikkerZPlanskifte = 75.0;
        #endregion

        #region Machine limits
        const double m_limit_min_x = 0;
        const double m_limit_max_x = 2600;
        const double m_limit_min_y = 0;
        const double m_limit_max_y = 1500;
        const double m_limit_min_z = 0;
        const double m_limit_max_z = 800;

        const double m_limit_min_b = -120;
        const double m_limit_max_b = 120;
        const double m_limit_min_c = -270;
        const double m_limit_max_c = 270;

        public Interval LimitX { get { return new Interval(m_limit_min_x, m_limit_max_x); } }
        public Interval LimitY { get { return new Interval(m_limit_min_y, m_limit_max_y); } }
        public Interval LimitZ { get { return new Interval(m_limit_min_z, m_limit_max_z); } }
        public Interval LimitB { get { return new Interval(m_limit_min_b, m_limit_max_b); } }
        public Interval LimitC { get { return new Interval(m_limit_min_c, m_limit_max_c); } }
        #endregion

        private bool InMachineLimits(double x, double y, double z, double b, double c)
        {
            //return (LimitX.IncludesParameter(x) && LimitY.IncludesParameter(y) && LimitZ.IncludesParameter(z) &&
            //    LimitB.IncludesParameter(b) && LimitC.IncludesParameter(c));
            return
                x > m_limit_min_x &&
                x < m_limit_max_x &&
                y > m_limit_min_y &&
                y < m_limit_max_y &&
                z > m_limit_min_z &&
                z < m_limit_max_z &&
                b > m_limit_min_b &&
                b < m_limit_max_b &&
                c > m_limit_min_c &&
                c < m_limit_max_c;
        }

        public override object Compute()
        {
            //List<string> Tools = new List<string>();
            int tool_number = Tools.Count + 1;
            for (int i = 0; i < Paths.Count; ++i)
            {
                if (!Tools.ContainsKey(Paths[i].ToolName))
                {
                    Tools.Add(Paths[i].ToolName, new Tool(Paths[i].ToolName, Paths[i].ToolDiameter, tool_number));
                    tool_number++;
                }
            }

            List<string> Program = new List<string>();
            Errors = new List<string>();

            /*
            // Dummy variables
            string Name = "Test";
            string Author = "Tom";
            string Date = System.DateTime.Now.ToShortDateString();
            string ProgramTime = "Not too long";
            double MaterialWidth = 200, MaterialHeight = 500, MaterialDepth = 25;
            */

            //bool HighSpeed = true;

            // Create headers
            Program.Add("%");
            Program.Add("O0001");
            Program.Add($"(Revision      : 1 )");
            Program.Add("");
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
            foreach (Tool t in Tools.Values)
            {
                Program.Add($"( {t.Number} ; {t.Diameter} ; {t.Length} ; {t.Name} )");
            }

            Program.Add("");
            Program.Add("");

            Program.Add("( * * * * * VARIABLES * * * * * )");
            Program.Add($"#560 = {55}    (ZERO POINT)");
            Program.Add($"#561 = {WorkOffset.X}    (OFFSET PROGRAM I X)");
            Program.Add($"#562 = {WorkOffset.Y}    (OFFSET PROGRAM I Y)");
            Program.Add($"#563 = {WorkOffset.Z}    (OFFSET PROGRAM I Z)");

            Program.Add($"#564 = {MaterialThickness}    (EMNE TYKKELSE)");
            Program.Add($"#565 = {SikkerZ}    (SIKKER Z)");
            Program.Add($"#566 = {SikkerZPlanskifte}    (SIKKER Z VED PLANSKIFTE)");
            Program.Add($"#563 = #563 + #564");
            Program.Add($"#569 = {60}");

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
            Program.Add("G#560");
            Program.Add("G52 X#561 Y#562 Z#563");


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
                Program.Add($"( Tool no.  : {Tools[TP.ToolName].Number} )");
                Program.Add($"( Tool des. : {Tools[TP.ToolName].Name} )");
                Program.Add($"( Tool dia. : {Tools[TP.ToolName].Diameter} )");

                // Tool change
                Program.Add($"M6 T{Tools[TP.ToolName].Number}");

                // Start spindle
                Program.Add($"M3 S{TP.SpindleSpeed}");
                Program.Add("#567 = #2255+135.0");
                Program.Add("#568 = 0 + SQRT[#567*#567+625]+#566-135");
                Program.Add("G#560");

                Program.Add("");

                if (HighSpeed)
                    Program.Add("G5.1 Q1");

                Program.Add($"G43.4 H{Tools[TP.ToolName].Number}");


                // If toolpath is planar, lock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M32 M34");

                // Move to first waypoint
                Waypoint prev = TP.Paths[0][0];
                if (prev.Type != (int)WaypointType.RAPID)
                    throw new Exception("First waypoint must be rapid. Check code.");

                // Calculate B and C values
                double B, C, prevB, prevC;
                Vector3d axisFirst = -prev.Plane.ZAxis;
                axisFirst.Unitize();

                prevB = Rhino.RhinoMath.ToDegrees(Math.Acos(axisFirst * -Vector3d.ZAxis));
                prevC = Rhino.RhinoMath.ToDegrees(Math.Atan2(axisFirst.Y, axisFirst.X));

                //Program.Add($"G{(int)prev.Type} X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3}  Z{prev.Plane.Origin.Z:F3} B{prevB:F3} C{prevC:F3}");
                //Program.Add($"G0 X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3} B{prevB:F3} C{prevC:F3} Z#568");
                Program.Add($"G0 X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3} B{prevB:F3} C{prevC:F3}");

                bool write_feedrate = false;
                double diff;
                // Go through waypoints

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    Path Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        write_feedrate = false;

                        Waypoint wp = Subpath[k];

                        // Calculate B and C values
                        Vector3d axis = -wp.Plane.ZAxis;
                        axis.Unitize();

                        B = Rhino.RhinoMath.ToDegrees(Math.Acos(axis * -Vector3d.ZAxis));
                        C = Rhino.RhinoMath.ToDegrees(Math.Atan2(axis.Y, axis.X));

                        // Deal with abrupt 180 to -180 switches
                        diff = C - prevC;
                        if (diff > 270) C -= 360.0;
                        if (diff < -270) C += 360.0;

                        // Check limits
                        if (!InMachineLimits(wp.Plane.Origin.X, wp.Plane.Origin.Y, wp.Plane.Origin.Z, B, C))
                            Errors.Add($"Waypoint outside of machine limits: toolpath {i} subpath {j} waypoint {k}");

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
                            {
                                Line.Add($"G1");
                                write_feedrate = true;
                            }

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

                        if (write_feedrate)
                            if ((wp.Type & 2) != 0)
                                Line.Add($"F{TP.PlungeRate}");
                            else
                                Line.Add($"F{TP.FeedRate}");

                        //if ((wp.Type & 3) != (prev.Type & 3))
                        //    if ((wp.Type & 2) != 0)
                        //        Line.Add($"F{TP.PlungeRate}");
                            //else if ((wp.Type & 2) == 0)
                            //    Line.Add($"F{TP.FeedRate}");

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
                //Program.Add("G49 G53 G69");
                Program.Add("G49");
                Program.Add("G69");

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
            Program.Add("G53");
            Program.Add("G0 Z0");
            Program.Add("G0 X0 Y0");

            Program.Add("( * * * * *  END  * * * * * )");

            //Program.Add("M7"); // This should be to return the tool, but check
            //Program.Add("");
            Program.Add("M99");
            Program.Add("%");


            return Program;
        }
    }
}