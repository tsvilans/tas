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

using WPath = System.Collections.Generic.List<tas.Machine.Waypoint>;

namespace tas.Machine.Posts
{
    /// <summary>
    /// Post to convert to G-code for Haas 3-axis mill.
    /// </summary>
    public class HaasPost : tas.Machine.MachinePost
    {

        const int DOF = 3;

        #region Machine limits

        public Interval LimitX { get { return m_limits[0]; } }
        public Interval LimitY { get { return m_limits[1]; } }
        public Interval LimitZ { get { return m_limits[2]; } }
        #endregion

        public HaasPost() : base(DOF)
        {
            PreComment = "(";
            PostComment = ")";

            m_limits[0] = new Interval(0, 1016);
            m_limits[1] = new Interval(0, 508);
            m_limits[2] = new Interval(0, 406);

            // Spindle nose to table (max): 508 mm
            // Spindle nose to table (min): 102 mm

            // Table
            //    length 1467 mm
            //    width 368 mm

            m_axis_id[0] = 'X';
            m_axis_id[1] = 'Y';
            m_axis_id[2] = 'Z';
        }

        public override void PlaneToCoords(Plane plane, ref double[] coords)
        {
            coords[0] = plane.Origin.X;
            coords[1] = plane.Origin.Y;
            coords[2] = plane.Origin.Z;
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

            Program = new List<string>();
            Errors = new List<string>();

            BoundingBox = BoundingBox.Empty;

            if (StockModel != null)
                BoundingBox = StockModel.GetBoundingBox(true);

            // Working variables
            int G_VALUE = -1;
            int flags = 0;
            bool write_feedrate = true;

            // Initialize coordinates
            double[] coords = new double[DOF];
            for (int i = 0; i < DOF; ++i)
                coords[i] = double.MaxValue;

            double[] pCoords = new double[DOF];
            for (int i = 0; i < DOF; ++i)
                pCoords[i] = double.MaxValue;

            int currentFeedrate = 0;
            int tempFeedrate = int.MaxValue;

            EOL = " ;";

            // Create headers
            Program.Add("%");
            Program.Add($"O01001 ({Name})"); // Program number / name

            //bool HighSpeed = true;


            CreateHeader();

            /* G00 - Rapid mode
             * G17 - XY plane for circular interpolation
             * G40 - Cancel cutter compensation
             * G49 - Cancel Tool Length Compensation
             * G80 - Cancel canned cycles
             * G90 - Absolute coordinates
             * G98 - Return to initial start point
             */
            Program.Add("G00 G17 G40 G49 G80 G90 G98;"); // Safety line
            Program.Add("G00 G53 Z0;"); // Return to machine zero

            // Loop through Toolpaths
            for (int i = 0; i < Paths.Count; ++i)
            {

                Toolpath TP = Paths[i];

                Program.Add($"{PreComment}{PostComment}{EOL}");
                Program.Add($"{PreComment} START Toolpath: {TP.Name} {PostComment}{EOL}");
                Program.Add($"{PreComment}{PostComment}{EOL}");

                // Tool change
                // TODO: Change so that it only changes the tool if necessary, though the machine should ignore this anyway
                Program.Add($"T{Tools[TP.Tool.Name].Number} M06{EOL}");

                // Move to first waypoint
                Waypoint prev = new Waypoint(TP.Paths[0][0]);
                prev.Type = (int)WaypointType.RAPID;

                /* G00 - Rapid motion
                 * G90 - Absolute positioning (G91 is incremental)
                 * G21 - Metric programming (G20 is inch)
                 * G54 - First work offset
                 * S, M03 - Start spindle clockwise
                 */
                Program.Add($"G00 G90 G21 G54 X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3} S{TP.Tool.SpindleSpeed} M03{EOL}");
                Program.Add($"G43 H{Tools[TP.Tool.Name].OffsetNumber:00} M08{EOL}");



                // Loop through subpaths
                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    WPath Subpath = TP.Paths[j];

                    // Loop through individual waypoints
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        // Reset working variables
                        write_feedrate = false;
                        flags = 0;

                        Waypoint wp = Subpath[k];

                        // Convert waypoint targets to axis coordinates
                        PlaneToCoords(wp.Plane, ref coords);
                        PlaneToCoords(prev.Plane, ref pCoords);

                        // Check limits
                        if (!IsInMachineLimits(coords))
                            Errors.Add($"Waypoint outside of machine limits: toolpath {i} subpath {j} waypoint {k} : {wp}");

                        // Compose NC line
                        List<string> Line = new List<string>();

                        #region Parse movement (G code)
                        if (wp.Type != prev.Type || AlwaysWriteGCode)
                        {
                            flags = flags | 1;

                            if (wp.IsRapid())
                                G_VALUE = 0;
                            else if (wp.IsArc())
                            {
                                write_feedrate = true;
                                if (wp.IsClockwise())
                                    G_VALUE = 3;
                                else
                                    G_VALUE = 2;
                            }
                            else
                            {
                                G_VALUE = 1;
                                write_feedrate = true;
                            }
                        }
                        #endregion

                        #region Parse movement on axes
                        for (int l = 0; l < m_dof; ++l)
                        {
                            if (Math.Abs(coords[l] - pCoords[l]) > 0.00001)
                                flags = flags | (1 << (l + 1));
                        }
                        #endregion

                        #region Write feedrate if different
                        // If Plunge move, set current feedrate to PlungeRate
                        if (wp.IsPlunge())
                            tempFeedrate = Tools[TP.Tool.Name].PlungeRate;
                        else
                            tempFeedrate = Tools[TP.Tool.Name].FeedRate;

                        // If new feedrate is different from old one, write F value
                        if (tempFeedrate != currentFeedrate)
                            write_feedrate = true;

                        currentFeedrate = tempFeedrate;
                        #endregion

                        // If it is an arc move, then write I J K values
                        if (wp.IsArc())
                            flags = flags | (1 << m_dof + 1);


                        if (write_feedrate)
                            flags = flags | (1 << m_dof + 2);

                        // If there is no motion, skip this waypoint
                        if ((flags & m_NO_MOTION) < 1) continue;

                        #region Construct NC code

                        if ((flags & 1) > 0)
                            Line.Add($"G{G_VALUE:00}");

                        for (int l = 0; l < m_dof; ++l)
                        {
                            if ((flags & (1 << 1 + l)) > 0)
                                Line.Add($"{m_axis_id[l]}{coords[l]:F3}");
                        }

                        if ((flags & (1 << m_dof + 1)) > 0)
                            Line.Add($"R{wp.Radius:F3}");

                        if ((flags & (1 << m_dof + 2)) > 0)
                            Line.Add($"F{currentFeedrate}");

                        #endregion

                        // Add line to program
                        Program.Add(string.Join(" ", Line) + EOL);

                        // Update previous waypoint
                        prev = new Waypoint(wp);
                    }
                }


                Program.Add($"{PreComment}{PostComment}{EOL}");
                Program.Add($"G53 G49 G0 Z0.{EOL}");
                //Program.Add("G53 X0. Y0.");

                //Program.Add("G0 Z12");

                Program.Add($"{PreComment}{PostComment}{EOL}");
                Program.Add($"{PreComment} END Toolpath: {TP.Name} {PostComment}{EOL}");
                Program.Add($"{PreComment}{PostComment}{EOL}");
            }
            //Program.Add($"G00 Z10.");

            Program.Add($"{PreComment} End of program {PostComment}{EOL}");
            Program.Add($"G53 G49 G0 Z0.{EOL}");
            Program.Add($"G0 X0 Y0{EOL}");
            Program.Add($"M05{EOL}"); // Spindle stop
            Program.Add($"M30{EOL}");
            Program.Add("%");
            return Program;
        }
    }
}