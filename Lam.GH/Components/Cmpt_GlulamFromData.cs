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
using System.Linq;

using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Lam.GH
{
    public class Cmpt_GlulamFromData : GH_Component
    {
        public Cmpt_GlulamFromData()
          : base("Glulam from Data", "Glulam",
              "Create glulam using curve, frames, and glulam data.",
              "tasTools", "Glulam")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "Crv", "Glulam centreline curve.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Frames", "F", "Frames to orient cross-section to.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Data", "D", "Glulam data.", GH_ParamAccess.item);

            pManager[1].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam object.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            List<Plane> planes = new List<Plane>();

            if (!DA.GetData("Curve", ref crv))
                return;

            DA.GetDataList("Frames", planes);
      
            Lam.GlulamData data = null;
            object iData = null;

            DA.GetData("Data", ref iData);
            if (iData is GlulamData)
                data = iData as GlulamData;
            else if (iData is GH_GlulamData)
                data = (iData as GH_GlulamData).Value;
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get GlulamData.");

            Glulam blank = Glulam.CreateGlulam(crv, planes.ToArray(), data);

            DA.SetData("Glulam", new GH_Glulam(blank));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_FreeformGlulam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{1ce3257c-3b1c-477b-bcaf-ae2efe485d5f}"); }
        }
    }
}