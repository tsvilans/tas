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

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Lam.GH
{
    public class Cmpt_GetGlulamFace : GH_Component
    {

        public Cmpt_GetGlulamFace()
          : base("Get Glulam Face", "GFace",
              "Get a specific face of the Glulam. ",
              "tasLam", "Modify")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input Glulam.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Side", "S", "Side of Glulam to extract. Use bitmask flags to get multiple sides.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Faces", "F", "Glulam faces.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object raw_in = null;
            if (!DA.GetData("Glulam", ref raw_in))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Glulam specified.");
                return;
            }
            GH_Glulam ghg = raw_in as GH_Glulam;
            if (ghg == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input is not valid Glulam object.");
                return;
            }
            Glulam g = ghg.Value;

            int side = 0;
            DA.GetData("Side", ref side);

            Brep[] breps = g.GetGlulamFaces(side);

            DA.SetDataList("Faces", breps);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_GetGlulamSrf_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5ad108dc-b291-4cd6-bdb2-1568742fcbbe"); }
        }
    }
}