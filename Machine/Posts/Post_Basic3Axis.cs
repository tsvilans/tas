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
    /// Post to convert to G-code for basic 3-axis mill.
    /// </summary>
    public class Basic3AxisPost : MachinePost
    {

        #region Machine limits
        const double m_limit_min_x = 0;
        const double m_limit_max_x = 1100;
        const double m_limit_min_y = 0;
        const double m_limit_max_y = 600;
        const double m_limit_min_z = -100;
        const double m_limit_max_z = 0;

        public Interval LimitX { get { return new Interval(m_limit_min_x, m_limit_max_x); } }
        public Interval LimitY { get { return new Interval(m_limit_min_y, m_limit_max_y); } }
        public Interval LimitZ { get { return new Interval(m_limit_min_z, m_limit_max_z); } }
        #endregion

        public bool AlwaysWriteGCode = true;

        private bool CheckMachineLimits(double x, double y, double z)
        {
            return
              x >= m_limit_min_x &&
              x <= m_limit_max_x &&
              y >= m_limit_min_y &&
              y <= m_limit_max_y &&
              z >= m_limit_min_z &&
              z <= m_limit_max_z;
        }

        public override object Compute()
        {
            if (Paths.Count < 1)
            {
                this.Errors.Add("No paths to process...");
                return null;
            }

            for (int i = 0; i < Paths.Count; ++i)
            {
                if (!Tools.ContainsKey(Paths[i].Tool.Name))
                {
                    this.Errors.Add(string.Format("Tool '{0}' not found in post-processor tool library.", Paths[i].Tool.Name));
                    continue;
                }
            }

            List<string> Program = new List<string>();
            Errors = new List<string>();

            BoundingBox bbox = BoundingBox.Unset;

            if (StockModel != null)
                bbox = StockModel.GetBoundingBox(true);

            // Create headers
            
            Program.Add("%");
            Program.Add($"(Revision      : 1 )");
            Program.Add("");
            Program.Add($"(File name      : {Name} )");
            Program.Add($"(Programmed by  : {Author} )");
            Program.Add($"(Date           : {Date} )");
            Program.Add($"(Program length : {ProgramTime} )");
            Program.Add($"(Bounds min.    : {bbox.Min.X} {bbox.Min.Y} {bbox.Min.Z} )");
            Program.Add($"(Bounds max.    : {bbox.Max.X} {bbox.Max.Y} {bbox.Max.Z} )");
            Program.Add("");
            
            Program.Add("G0 Z12");

            int gValue = int.MaxValue;          // 0
            double xValue = double.MaxValue;    // 1
            double yValue = double.MaxValue;    // 2
            double zValue = double.MaxValue;    // 3
            double rValue = 0;                  // 4
            int fValue = int.MaxValue;          // 5

            int flags = 0;

            for (int i = 0; i < Paths.Count; ++i)
            {

                Toolpath TP = Paths[i];

                Program.Add("");

                // Tool change
                Program.Add($"M6 T{Tools[TP.Tool.Name].Number}");

                // Move to first waypoint
                Waypoint prev = new Waypoint(TP.Paths[0][0]);
                prev.Type = (int)WaypointType.RAPID;

                //if (prev.Type != (int)WaypointType.RAPID)
                //    throw new Exception("First waypoint must be rapid. Check code.");

                Program.Add($"G0 X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3}");

                bool write_feedrate = false;

                // Go through waypoints

                int currentFeedrate = 0;
                int tempFeedrate = currentFeedrate;

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    Path Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        write_feedrate = false;
                        flags = 0;

                        Waypoint wp = Subpath[k];

                        xValue = wp.Plane.Origin.X;
                        yValue = wp.Plane.Origin.Y;
                        zValue = wp.Plane.Origin.Z;

                        // Check limits
                        if (!CheckMachineLimits(xValue, yValue, zValue))
                            Errors.Add($"Waypoint outside of machine limits: toolpath {i} subpath {j} waypoint {k} : {wp}");

                        // Compose line
                        List<string> Line = new List<string>();

                        if (wp.Type != prev.Type || AlwaysWriteGCode)
                        {
                            flags = flags | 1;
                            if ((wp.Type & 1) == 1)
                                gValue = 0;
                            else if ((wp.Type & 4) == 1)
                                if ((wp.Type & 12) == 1)
                                    gValue = 3;
                                else
                                    gValue = 2;
                            else
                            {
                                gValue = 1;
                                write_feedrate = true;
                            }
                        }

                        // Set flags for motion
                        if (Math.Abs(wp.Plane.Origin.X - prev.Plane.Origin.X) > 0.00001)
                            flags = flags | (1 << 1);
                        if (Math.Abs(wp.Plane.Origin.Y - prev.Plane.Origin.Y) > 0.00001)
                            flags = flags | (1 << 2);
                        if (Math.Abs(wp.Plane.Origin.Z - prev.Plane.Origin.Z) > 0.00001)
                            flags = flags | (1 << 3);

                        // If Plunge move, set current feedrate to PlungeRate
                        if ((wp.Type & 2) != 0)
                            tempFeedrate = Tools[TP.Tool.Name].PlungeRate;
                        else
                            tempFeedrate = Tools[TP.Tool.Name].FeedRate;

                        // If new feedrate is different from old one, write F value
                        if (tempFeedrate == currentFeedrate)
                            write_feedrate = false;

                        currentFeedrate = tempFeedrate;


                        // If it is an arc move, then write R value
                        if ((wp.Type & 4) == 1)
                            flags = flags | (1 << 4);

                        if (write_feedrate)
                            flags = flags | (1 << 5);


                        // If there is no motion, skip this waypoint
                        if ((flags & 14) < 1) continue;


                        // Construct line
                        if ((flags & (1 << 0)) > 0)
                            Line.Add($"G{gValue}");
                        if ((flags & (1 << 1)) > 0)
                            Line.Add($"X{xValue:F3}");
                        if ((flags & (1 << 2)) > 0)
                            Line.Add($"Y{yValue:F3}");
                        if ((flags & (1 << 3)) > 0)
                            Line.Add($"Z{zValue:F3}");
                        if ((flags & (1 << 4)) > 0)
                            Line.Add($"R{rValue:F3}");
                        if ((flags & (1 << 5)) > 0)
                            Line.Add($"F{currentFeedrate}");

                        // Add line to program
                        Program.Add(string.Join(" ", Line));

                        // Update previous waypoint
                        prev = new Waypoint(wp);
                    }
                }


                Program.Add("");


                Program.Add("G0 Z12");

                Program.Add("");
            }

            Program.Add("G0 Z0");
            Program.Add("G0 X0 Y0");

            Program.Add("M30");

            return Program;
        }
    }
}