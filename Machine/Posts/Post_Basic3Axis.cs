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
        const int DOF = 3;

        #region Machine limits

        public Interval LimitX { get { return m_limits[0]; } }
        public Interval LimitY { get { return m_limits[1]; } }
        public Interval LimitZ { get { return m_limits[2]; } }
        #endregion

        public Basic3AxisPost() : base(DOF)
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

            List<string> Program = new List<string>();
            Errors = new List<string>();

            BoundingBox bbox = BoundingBox.Unset;

            if (StockModel != null)
                bbox = StockModel.GetBoundingBox(true);

            // Create headers
            Program.Add("O01001;"); // Program number / name

            Program.Add($"{PreComment}{PostComment};");
            Program.Add($"{PreComment} Revision      : 1 {PostComment};");
            Program.Add($"{PreComment}{PostComment}");
            Program.Add($"{PreComment} File name      : {Name} {PostComment};");
            Program.Add($"{PreComment} Programmed by  : {Author} {PostComment};");
            Program.Add($"{PreComment} Date           : {Date} {PostComment};");
            Program.Add($"{PreComment} Program length : {ProgramTime} {PostComment}");
            Program.Add($"{PreComment} Bounds min.    : {bbox.Min.X} {bbox.Min.Y} {bbox.Min.Z} {PostComment};");
            Program.Add($"{PreComment} Bounds max.    : {bbox.Max.X} {bbox.Max.Y} {bbox.Max.Z} {PostComment};");
            Program.Add($"{PreComment}{PostComment};");

            Program.Add($"{PreComment}Tool #    Offset #    Name    Diameter    Length {PostComment};");

            foreach (var d in Tools)
            {
                MachineTool mt = d.Value;
                Program.Add($"{PreComment} {mt.Number}    {mt.OffsetNumber}    {mt.Name}    {mt.Diameter:0.0}    {mt.Length:0.000} {PostComment};");
            }
            Program.Add($"{PreComment}{PostComment};");



            Program.Add("G0 Z12");

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


                // Go through waypoints

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    Path Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        write_feedrate = false;
                        flags = 0;

                        Waypoint wp = Subpath[k];

                        PlaneToCoords(wp.Plane, ref coords);

                        // Check limits
                        if (!IsInMachineLimits(coords))
                            Errors.Add($"Waypoint outside of machine limits: toolpath {i} subpath {j} waypoint {k} : {wp}");

                        // Compose line
                        List<string> Line = new List<string>();

                        if (wp.Type != prev.Type || AlwaysWriteGCode)
                        {
                            flags = flags | 1;
                            if ((wp.Type & 1) == 1)
                                G_VALUE = 0;
                            else if ((wp.Type & 4) == 1)
                                if ((wp.Type & 12) == 1)
                                    G_VALUE = 3;
                                else
                                    G_VALUE = 2;
                            else
                            {
                                G_VALUE = 1;
                                write_feedrate = true;
                            }
                        }

                        #region Parse movement on axes
                        for (int l = 0; l < m_dof; ++l)
                        {
                            if (Math.Abs(coords[l] - pCoords[l]) > 0.00001)
                                flags = flags | (1 << (l + 1));
                        }
                        #endregion

                        // If Plunge move, set current feedrate to PlungeRate
                        if (wp.IsPlunge())
                            tempFeedrate = Tools[TP.Tool.Name].PlungeRate;
                        else
                            tempFeedrate = Tools[TP.Tool.Name].FeedRate;

                        // If new feedrate is different from old one, write F value
                        if (tempFeedrate == currentFeedrate)
                            write_feedrate = false;

                        currentFeedrate = tempFeedrate;


                        // If it is an arc move, then write R value
                        if (wp.IsArc())
                            flags = flags | (1 << 4);

                        if (write_feedrate)
                            flags = flags | (1 << 5);


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