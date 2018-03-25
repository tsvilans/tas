/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
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

using Grasshopper.Kernel;
using Grasshopper;
using Grasshopper.Kernel.Data;
using System.Linq;

namespace tas.Lam.GH
{
    public class Cmpt_MapPointsToGlulamSpace : GH_Component
    {
        public Cmpt_MapPointsToGlulamSpace()
          : base("Map Points To Glulam Space", "MapPt2Glulam",
              "Maps points to free-form Glulam space.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam to map to.", GH_ParamAccess.item);
            pManager.AddPointParameter("Points", "P", "Points to map. ", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Mapped points.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj = null;

            if (!DA.GetData("Glulam", ref obj))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No glulam blank connected.");
                return;
            }

            Glulam g;

            if (obj is GH_Glulam)
                g = (obj as GH_Glulam).Value;
            else
                g = obj as Glulam;

            if (g == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid glulam input.");
                return;
            }

            List<Point3d> m_input_points = new List<Point3d>();

            if (!DA.GetDataList("Points", m_input_points))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No input points specified.");
                return;
            }

            Point3d[] m_output_points = new Point3d[m_input_points.Count];

            Plane m_plane;
            Point3d m_temp;
            double t;
            for (int i = 0; i < m_input_points.Count; ++i)
            {
                g.Centreline.ClosestPoint(m_input_points[i], out t);
                m_plane = g.GetPlane(t);
                m_plane.RemapToPlaneSpace(m_input_points[i], out m_temp);
                m_temp.Z = g.Centreline.GetLength(new Interval(g.Centreline.Domain.Min, t));

                m_output_points[i] = m_temp;
            }

            DA.SetDataList("Points", m_output_points);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_Delaminate_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{9ad2a6f1-6e42-40ea-a6a4-09bf7a0f0be0}"); }
        }
    }
}