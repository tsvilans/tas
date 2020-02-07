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
    public class Cmpt_MapPlanesToGlulamSpace : GH_Component
    {
        public Cmpt_MapPlanesToGlulamSpace()
          : base("Map Planes To Glulam Space", "MapPl2Glulam",
              "Maps planes to free-form Glulam space.",
              "tasLam", "Modify")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam to map to.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Planes", "P", "Planes to map. ", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Mapped planes.", GH_ParamAccess.list);
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

            List<Plane> m_input_planes = new List<Plane>();

            if (!DA.GetDataList("Planes", m_input_planes))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No input points specified.");
                return;
            }

            Plane[] m_output_planes = new Plane[m_input_planes.Count];

            Plane m_plane;
            Plane m_temp;
            double t;

            for (int i = 0; i < m_input_planes.Count; ++i)
            {
                g.Centreline.ClosestPoint(m_input_planes[i].Origin, out t);
                m_plane = g.GetPlane(t);
                m_temp = m_input_planes[i];
                m_temp.Transform(Transform.PlaneToPlane(m_plane, Plane.WorldXY));
                m_temp.OriginZ = g.Centreline.GetLength(new Interval(g.Centreline.Domain.Min, t));

                m_output_planes[i] = m_temp;
            }

            DA.SetDataList("Planes", m_output_planes);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_Delaminate_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{e5abbe1d-009d-4963-a31f-46492130921a}"); }
        }
    }
}