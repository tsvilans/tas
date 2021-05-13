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
    public class ShopbotPost : MachinePost
    {
        const int DOF = 3;

        #region Machine limits

        public Interval LimitX { get { return m_limits[0]; } }
        public Interval LimitY { get { return m_limits[1]; } }
        public Interval LimitZ { get { return m_limits[2]; } }
        #endregion

        public ShopbotPost() : base(DOF)
        {
            PreComment = "'";
            PostComment = "";

            m_limits[0] = new Interval(0, 3710);
            m_limits[1] = new Interval(0, 1570);
            m_limits[2] = new Interval(0, 200);

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

            CreateHeader();


            Program.Add($"{PreComment}----------------------------------------------------------------");
            Program.Add($"IF %(25)=0 THEN GOTO UNIT_ERROR");
            Program.Add($"SA");
            Program.Add($"CN, 90");
            Program.Add($"&Tool = 0  'tool nul,  just in case ATC is active");
            Program.Add($"C6 'Return tool to home in x and y");
            Program.Add($"PAUSE 2");
            Program.Add($"{PreComment}----------------------------------------------------------------");

            /*
             LINEAR = M3
             RAPID = J2
             ARC = CG
             
             */

            // Working variables
            string MOVE_CODE = "M3";
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

            MachineTool ActiveTool;

            for (int i = 0; i < Paths.Count; ++i)
            {

                Toolpath TP = Paths[i];
                ActiveTool = Tools[TP.Tool.Name];

                Program.Add($"{PreComment}{PostComment}{EOL}");
                Program.Add($"{PreComment} START Toolpath: {TP.Name} {PostComment}{EOL}");
                Program.Add($"{PreComment}       Tool: {ActiveTool.Name} Diameter: {ActiveTool.Diameter} {PostComment}{EOL}");

                Program.Add($"{PreComment}{PostComment}{EOL}");

                Program.Add($"TR,{ActiveTool.SpindleSpeed}");

                // Tool change -> No tool change for the Shopbot
                // Program.Add($"M6 T{ActiveTool.Number}");

                // Move to first waypoint
                Waypoint prev = new Waypoint(TP.Paths[0][0]);
                prev.Type = (int)WaypointType.RAPID;

                //if (prev.Type != (int)WaypointType.RAPID)
                //    throw new Exception("First waypoint must be rapid. Check code.");

                Program.Add($"J2,{prev.Plane.Origin.X:F3},{prev.Plane.Origin.Y:F3},10");


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
                            if (wp.IsRapid())
                            {
                                MOVE_CODE = "J2";
                            }
                            else if (wp.IsArc())
                            {
                                if (wp.IsClockwise())
                                    MOVE_CODE = "CW";
                                else
                                    MOVE_CODE = "CCW";
                            }
                            else
                            {
                                MOVE_CODE = "M3";
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
                            tempFeedrate = ActiveTool.PlungeRate;
                        else
                            tempFeedrate = ActiveTool.FeedRate;

                        // If new feedrate is different from old one, write F value
                        if (tempFeedrate == currentFeedrate)
                            write_feedrate = false;

                        currentFeedrate = tempFeedrate;

                        if (write_feedrate)
                            Program.Add($"MS,{ActiveTool.FeedRate},{ActiveTool.PlungeRate}");

                        // If it is an arc move, then write R value
                        if (wp.IsArc())
                            flags = flags | (1 << (m_dof + 1));

                        if (write_feedrate)
                            flags = flags | (1 << (m_dof + 2));

                        // If there is no motion, skip this waypoint
                        if ((flags & m_NO_MOTION) < 1) continue;


                        #region Construct NC code
                        // TODO: Flesh this out. Shopbot is a bit different, so maybe
                        // just need to manually code in the different types of moves

                        if (wp.IsArc())
                        {
                            Program.Add($"CG,,{coords[0]:F3},{coords[1]:F3},{wp.Radius},0,T,1");
                        }
                        else
                        { 
                            if ((flags & 1) > 0 || true)
                                Line.Add($"{MOVE_CODE}");

                            for (int l = 0; l < m_dof; ++l)
                            {
                                if ((flags & (1 << 1 + l)) > 0 || true)
                                    Line.Add($"{coords[l]:F3}");
                            }
                        }

                        #endregion

                        // Add line to program
                        Program.Add(string.Join(",", Line));

                        // Update previous waypoint
                        prev = new Waypoint(wp);
                    }
                }
            }

            Program.Add($"{PreComment}----------------------------------------------------------------");
            Program.Add($"{PreComment}Turning router OFF");
            Program.Add("C7");
            Program.Add("END");

            Program.Add("UNIT_ERROR:");
            Program.Add("CN, 91");

            return Program;
        }
    }
}