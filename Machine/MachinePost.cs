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

namespace tas.Machine
{

    /// <summary>
    /// Base class for toolpath post-processor. Inherit from this
    /// to create machine-specific posts.
    /// </summary>
    public abstract class MachinePost
    {
        public string PreComment = "%";
        public string PostComment = "";
        public string EOL = "";
        public string Name = "MachinePostBase";
        public string Author = "Author";
        public string Date = System.DateTime.Now.ToShortDateString() + " " + System.DateTime.Now.ToShortTimeString();
        public string ProgramTime = "X";
        public Mesh StockModel = null;
        public bool AlwaysWriteGCode = false;

        public Point3d WorkOffset = Point3d.Origin;
        public List<string> Program = new List<string>();
        public BoundingBox BoundingBox = BoundingBox.Empty;

        public List<Toolpath> Paths = new List<Toolpath>();

        protected readonly Interval[] m_limits;
        protected readonly char[] m_axis_id;
        protected readonly int m_dof;
        protected readonly int m_NO_MOTION;

        public MachinePost(int dof)
        {
            m_dof = dof;
            m_limits = new Interval[dof];
            m_axis_id = new char[dof];

            m_NO_MOTION = 0;
            for (int i = 1; i <= dof; ++i)
                m_NO_MOTION = m_NO_MOTION | (1 << i);
        }

        public abstract void PlaneToCoords(Plane plane, ref double[] coords);

        protected bool IsInMachineLimits(double[] coords)
        {
            if (coords.Length != m_dof) throw new Exception("Invalid DOF in IsInMachineLimits()!");

            for (int i = 0; i < m_dof; ++i)
                if (!m_limits[i].IncludesParameter(coords[i]))
                    return false;
            return true;
        }

        public abstract object Compute();
        public void AddPath(Toolpath p) => Paths.Add(p);
        public void AddPaths(ICollection<Toolpath> p) => Paths.AddRange(p);
        public void ClearPaths() => Paths.Clear();
        public Dictionary<string, MachineTool> Tools = new Dictionary<string, MachineTool>();
        public List<string> Errors = new List<string>();

        public void CreateHeader()
        {
            // Create headers
            Program.Add($"{PreComment}----------------------------------------------------------------{PostComment}{EOL}");
            Program.Add($"{PreComment} Revision      : 1 {PostComment}{EOL}");
            Program.Add($"{PreComment} File name      : {Name} {PostComment}{EOL}");
            Program.Add($"{PreComment} Programmed by  : {Author} {PostComment}{EOL}");
            Program.Add($"{PreComment} Date           : {Date} {PostComment}{EOL}");
            Program.Add($"{PreComment} Program length : {ProgramTime} {PostComment}");
            Program.Add($"{PreComment} Bounds min.    : {BoundingBox.Min.X:F3} {BoundingBox.Min.Y:F3} {BoundingBox.Min.Z:F3} {PostComment}{EOL}");
            Program.Add($"{PreComment} Bounds max.    : {BoundingBox.Max.X:F3} {BoundingBox.Max.Y:F3} {BoundingBox.Max.Z:F3} {PostComment}{EOL}");
            Program.Add($"{PreComment}{PostComment};");

            Program.Add($"{PreComment}Tool #    Offset #    Name    Diameter    Length {PostComment}{EOL}");

            foreach (var d in Tools)
            {
                MachineTool mt = d.Value;
                Program.Add($"{PreComment} {mt.Number}    {mt.OffsetNumber}    {mt.Name}    {mt.Diameter:0.0}    {mt.Length:0.000} {PostComment}{EOL}");
            }
            Program.Add($"{PreComment}----------------------------------------------------------------{PostComment}{EOL}");
        }

        public void AddTool(MachineTool t)
        {
            if (Tools.ContainsKey(t.Name))
                Tools[t.Name] = t;
            else
                Tools.Add(t.Name, t);
        }

    }



 
}
