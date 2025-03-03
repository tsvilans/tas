﻿/*
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
    /// Post to convert to G-code for CMS 5-axis mill.
    /// </summary>
    public class CMSPost : MachinePost
    {
        const int DOF = 5;

        #region Machine limits

        public Interval LimitX { get { return m_limits[0]; } }
        public Interval LimitY { get { return m_limits[1]; } }
        public Interval LimitZ { get { return m_limits[2]; } }
        public Interval LimitB { get { return m_limits[3]; } }
        public Interval LimitC { get { return m_limits[4]; } }
        #endregion

        public CMSPost() : base(DOF)
        {
            PreComment = "(";
            PostComment = ")";

            m_limits[0] = new Interval(-2600, 0);
            m_limits[1] = new Interval(-1500, 0);
            m_limits[2] = new Interval(-975, 0);
            m_limits[3] = new Interval(-120, 120);
            m_limits[4] = new Interval(-330, 330);


            // Table
            //    length 3000 mm
            //    width 1200 mm

            m_axis_id[0] = 'X';
            m_axis_id[1] = 'Y';
            m_axis_id[2] = 'Z';

            m_axis_id[3] = 'B';
            m_axis_id[4] = 'C';
        }

        public override void PlaneToCoords(Plane plane, ref double[] coords)
        {
            coords[0] = plane.Origin.X;
            coords[1] = plane.Origin.Y;
            coords[2] = plane.Origin.Z;

            coords[3] = Rhino.RhinoMath.ToDegrees(Math.Acos(plane.ZAxis * Vector3d.ZAxis));
            coords[4] = Rhino.RhinoMath.ToDegrees(Math.Atan2(plane.ZAxis.Y, plane.ZAxis.X));
        }

        public void FlipWrist(ref double[] coords)
        {
            coords[3] = -coords[3];

            if (Math.Abs(coords[4] - 180) < Math.Abs(coords[4] + 180))
                coords[4] -= 180;
            else
                coords[4] += 180;
        }

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
        public double SikkerZ = 0; // original was -50
        public double SikkerZPlanskifte = 0; // original was 75.0
        #endregion

        #region Machine values
        public int GWorkOffset = 54;
        #endregion


        #region HackVariables
        List<bool> m_flipList = new List<bool>();
        #endregion

        private bool CheckAbsoluteLimits(Plane plane, MachineTool tool)
        {
            Point3d abs = GetMachinePosition(plane, tool);

            if (!m_limits[0].IncludesParameter(abs.X) || 
                !m_limits[1].IncludesParameter(abs.Y) ||
                !m_limits[2].IncludesParameter(abs.Z))
                return false;
            return true;
        }

        public Point3d GetMachinePosition(Plane plane, MachineTool tool)
        {
            double C = 0;

            if (plane.ZAxis * Vector3d.ZAxis > 0.99999)
                C = Math.Atan2(plane.XAxis.Y, plane.XAxis.X);
            else
                C = Math.Atan2(plane.ZAxis.Y, plane.ZAxis.X);

            double B = Math.Acos(plane.ZAxis * Vector3d.ZAxis);

            double OFFSET_X = 0.04;
            double OFFSET_Y = -59.889;
            double OFFSET_Z = 134.985;

            var p = plane.Origin + plane.ZAxis * (tool.Length + OFFSET_Z);
            p.Z = plane.Origin.Z + Math.Cos(B) * tool.Length - Math.Sin(B) * OFFSET_Z;


            double cx = p.X + OFFSET_X;
            double cy = p.Y + OFFSET_Y;

            double s = Math.Sin(C);
            double c = Math.Cos(C);

            // translate point back to origin:
            p.X -= cx;
            p.Y -= cy;

            // rotate point
            double xnew = p.X * c - p.Y * s;
            double ynew = p.X * s + p.Y * c;

            // translate point back:
            p.X = xnew + cx;
            p.Y = ynew + cy;

            return p;
        }

        public void AddFlips(List<bool> flips)
        {
            m_flipList = flips;
        }

        public void GetBC(Vector3d v, out double B, out double C, bool flip = false)
        {
            v.Unitize();
            B = Rhino.RhinoMath.ToDegrees(Math.Acos(v * Vector3d.ZAxis));
            C = Rhino.RhinoMath.ToDegrees(Math.Atan2(v.Y, v.X));

            if (flip)
            {
                B = -B;

                if (Math.Abs(C - 180) < Math.Abs(C + 180))
                    C -= 180;
                else
                    C += 180;
            }
        }

        public override object Compute()
        {
            if (Paths.Count < 1)
            {
                this.Errors.Add("No paths to process...");
                return null;
            }

            //List<string> Tools = new List<string>();
            for (int i = 0; i < Paths.Count; ++i)
            {
                if (!Tools.ContainsKey(Paths[i].Tool.Name))
                {
                    this.Errors.Add($"Tool '{Paths[i].Tool.Name}' not found in post-processor tool library.");
                    continue;
                }
            }

            Axes = new List<AxisValues>();
            Program = new List<string>();
            Errors = new List<string>();

            //bool HighSpeed = true;

            // Create headers
            Program.Add("%");
            Program.Add("O0001");

            if (StockModel != null)
                BoundingBox = StockModel.GetBoundingBox(true);
            else
                ComputeBounds();

            CreateHeader();

            Program.Add("");

            Program.Add($"{PreComment} * * * * * WORK PLANE * * * * * {PostComment}");
            Program.Add($"G{GWorkOffset} X{WorkOffset.X} Y{WorkOffset.Y} Z{WorkOffset.Z}");

            /* Old CMS defaults and settings */
            /*
            Program.Add($"{PreComment} * * * * * VARIABLES * * * * * {PostComment}");
            Program.Add($"#560 = {GWorkOffset}    {PreComment}ZERO POINT{PostComment}");
            Program.Add($"#561 = {WorkOffset.X}    {PreComment}OFFSET PROGRAM I X{PostComment}");
            Program.Add($"#562 = {WorkOffset.Y}    {PreComment}OFFSET PROGRAM I Y{PostComment}");
            Program.Add($"#563 = {WorkOffset.Z}    {PreComment}OFFSET PROGRAM I Z{PostComment}");

            Program.Add($"#564 = {MaterialThickness}    {PreComment}EMNE TYKKELSE{PostComment}");
            Program.Add($"#565 = {SikkerZ}    {PreComment}SIKKER Z)");
            Program.Add($"#566 = {SikkerZPlanskifte}    {PreComment}SIKKER Z VED PLANSKIFTE{PostComment}");
            Program.Add($"#563 = #563 + #564");
            Program.Add($"#569 = {60}");

            Program.Add("");
            */

            Program.Add("");

            Program.Add($"{PreComment} * * * * * START * * * * * {PostComment}");

            // Init gcode
            Program.Add("G90 G40 G80 G49 G69");

            // Reset offsets to 0
            Program.Add("G92.1 X0 Y0 Z0 B0 C0");

            // CMS pressure clamping closing (unnecessary)
            //Program.Add("M25");

            // Set to mm (G20 is inches)
            Program.Add("G21");

            // Go home. G0 = rapid, G53 = move in absolute coords
            Program.Add("G0 G53 Z0");
            Program.Add("G0 B0 C0");
            //Program.Add("G#560");

            // Set work offset
            //Program.Add("G#560 X#561 Y#562 Z#563");
            Program.Add($"G{GWorkOffset}");


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

            int prevTool = -1;

            for (int i = 0; i < Paths.Count; ++i)
            {
                bool flip = false;
                if (i < m_flipList.Count)
                {
                    flip = m_flipList[i];
                }
                Toolpath TP = Paths[i];

                // Add toolpath info
                // TODO: Add support for tool indexing to the whole thing
                Program.Add("");
                Program.Add("");
                Program.Add($"{PreComment} * * * * * PATH {i:D2} * * * * * {PostComment}");

                Program.Add($"{PreComment} Operation : {TP.Name} {PostComment}");
                Program.Add($"{PreComment} Tool no.  : {Tools[TP.Tool.Name].Number} {PostComment}");
                Program.Add($"{PreComment} Tool des. : {Tools[TP.Tool.Name].Name} {PostComment}");
                Program.Add($"{PreComment} Tool dia. : {Tools[TP.Tool.Name].Diameter} {PostComment}");

                // Tool change
                Program.Add($"M6 T{Tools[TP.Tool.Name].Number}");

                if (TP.Tool.Number != prevTool)
                {
                    // Start spindle
                    Program.Add($"M3 S{Tools[TP.Tool.Name].SpindleSpeed}");
                }
                prevTool = TP.Tool.Number;
                // Not sure what this does...
                /*
                Program.Add("#567 = #2255+135.0");
                Program.Add("#568 = 0 + SQRT[#567*#567+625]+#566-135");
                Program.Add("G#560");

                */
                Program.Add("");
                //Program.Add("G0 G53 Z0 B-90");

                if (HighSpeed)
                    Program.Add("G5.1 Q1");

                Program.Add($"G43.4 H{Tools[TP.Tool.Name].OffsetNumber}");


                // If toolpath is planar, lock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M32 M34");

                // Move to first waypoint
                //Waypoint prev = TP.Paths[0][0];
                //if (prev.Type != (int)WaypointType.RAPID)
                //    throw new Exception("First waypoint must be rapid. Check code.");

                Waypoint prev = new Waypoint(TP.Paths[0][0]);
                prev.Type = (int)WaypointType.RAPID;

                // Calculate B and C values
                //PlaneToCoords(prev.Plane, ref pCoords);
                //if (flip)
                //    FlipWrist(ref pCoords);

                PlaneToCoords(prev.Plane, ref pCoords);

                if (TP.FlipWrist)
                    FlipWrist(ref pCoords);

                // Vector3d axisFirst = prev.Plane.ZAxis;
                //axisFirst.Unitize();

                //prevB = Rhino.RhinoMath.ToDegrees(Math.Acos(axisFirst * Vector3d.ZAxis));
                //prevC = Rhino.RhinoMath.ToDegrees(Math.Atan2(axisFirst.Y, axisFirst.X));

                //Program.Add($"G{(int)prev.Type} X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3}  Z{prev.Plane.Origin.Z:F3} B{prevB:F3} C{prevC:F3}");
                //Program.Add($"G0 X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3} B{prevB:F3} C{prevC:F3} Z#568");
                Program.Add($"G0 X{pCoords[0]:F3} Y{pCoords[1]:F3}"); 
                Program.Add($"G0 B{pCoords[3]:F3} C{pCoords[4]:F3}");
                Program.Add($"G0 Z{pCoords[2]:F3}");

                double diff;
                // Go through waypoints

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    WPath Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        write_feedrate = false;
                        flags = 0;

                        Waypoint wp = Subpath[k];

                        PlaneToCoords(wp.Plane, ref coords);
                        //if (flip)
                        //    FlipWrist(ref coords);
                        if (TP.FlipWrist)
                            FlipWrist(ref coords);

                        // Deal with abrupt 180 to -180 switches
                        diff = coords[4] - pCoords[4];
                        if (diff > 270) coords[4] -= 360.0;
                        if (diff < -270) coords[4] += 360.0;

                        // Check limits
                        //if (!IsInMachineLimits(coords))
                        //    Errors.Add($"Waypoint outside of machine limits: toolpath {i} subpath {j} waypoint {k} : {wp}, {coords[3]}, {coords[4]}");

                        if (!this.CheckAbsoluteLimits(wp.Plane, TP.Tool))
                        {
                            var temp_pt = GetMachinePosition(wp.Plane, TP.Tool);
                            Errors.Add($"Target {k} in toolpath {j} ({TP.Name}) out of bounds: {temp_pt.X:0.00} {temp_pt.Y:0.00} {temp_pt.Z:0.00}");
                        }

                        // Compose line
                        List<string> Line = new List<string>();

                        #region Parse movement (G code)
                        if (wp.Type != prev.Type || AlwaysWriteGCode)
                        {
                            flags = flags | 1;

                            if (wp.IsRapid())
                            {
                                G_VALUE = 0;
                                write_feedrate = false;
                                tempFeedrate = int.MaxValue;
                            }
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
                            if (Math.Abs(coords[l] - pCoords[l]) > 0.001)
                                flags = flags | (1 << (l + 1));
                        }
                        #endregion

                        #region Write feedrate if different
                        // If Plunge move, set current feedrate to PlungeRate
                        if (write_feedrate)
                        {
                            if (wp.IsPlunge())
                                tempFeedrate = Tools[TP.Tool.Name].PlungeRate;
                            else
                                tempFeedrate = Tools[TP.Tool.Name].FeedRate;

                            // If new feedrate is different from old one, write F value
                            write_feedrate = tempFeedrate != currentFeedrate ? true : false;
                        }

                        currentFeedrate = tempFeedrate;
                        #endregion

                        // If it is an arc move, then write I J K values
                        if (wp.IsArc())
                            flags = flags | (1 << (m_dof + 1));

                        if (write_feedrate)
                            flags = flags | (1 << (m_dof + 2));

                        // If there is no motion, skip this waypoint
                        if ((flags & m_NO_MOTION) < 1) continue;

                        Axes.Add(new AxisValues(coords[0], coords[1], coords[2], coords[3], coords[4], currentFeedrate));

                        #region Construct NC code

                        if ((flags & 1) > 0)
                            Line.Add($"G{G_VALUE:00}");

                        for (int l = 0; l < m_dof; ++l)
                        {
                            if ((flags & (1 << (1 + l))) > 0)
                                Line.Add($"{m_axis_id[l]}{coords[l]:F3}");
                        }

                        if ((flags & (1 << (m_dof + 1))) > 0)
                            Line.Add($"R{wp.Radius:F3}");

                        if ((flags & (1 << (m_dof + 2))) > 0)
                            Line.Add($"F{currentFeedrate}");

                        #endregion

                        // Add line to program
                        Program.Add(string.Join(" ", Line) + ";");
                        prev = wp;
                        Array.Copy(coords, pCoords, coords.Length);
                    }
                }

                Program.Add("");

                // Stop spindle
                //Program.Add("M5");

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

                //Program.Add("G0 G53 Z0");
                //Program.Add("G0 G53 B-90");


                Program.Add("");

            }

            // Return home
            // TODO: Look at example, find out if G53 is global coords
            // and add if necessary
            //Program.Add("G53");
            Program.Add("G0 G53 Z0");
            Program.Add("M5");

            //if (prevB >= 0)
            //    Program.Add("G0 G53 B90");
            //else if (prevB < 0)
            //    Program.Add("G0 G53 B-90");

            // This is the funny dance
            //Program.Add("G0 G53 B-90");
            //Program.Add("G0 G53 C0");

            Program.Add("G0 G53 B0 C0");
            Program.Add("G92.1 X0 Y0 Z0 B0 C0");


            Program.Add("G0 G53 Y0");
            Program.Add("G0 G53 X0");
            //Program.Add("G0 G53 X-2500");
            //Program.Add("G0 G53 B0");
            //Program.Add("G0 G53 X-2600");

            Program.Add($"{PreComment} * * * * *  END  * * * * * {PostComment}");

            //Program.Add("M7"); // This should be to return the tool, but check
            //Program.Add("");
            Program.Add("M99");
            Program.Add("%");

            return Program;
        }
    }
}