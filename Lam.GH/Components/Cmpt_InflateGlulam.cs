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

namespace tas.Lam.GH
{
    public class Cmpt_InflateGlulam : GH_Component
    {

        public Cmpt_InflateGlulam()
          : base("Inflate Glulam", "InfGlulam",
              "Expand Glulam in all directions.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input Glulam.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Amount", "A", "Amount to inflate all sides.", GH_ParamAccess.item, 10.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "O", "Output inflated Glulam.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object raw_g = null;
            if (!DA.GetData("Glulam", ref raw_g))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Glulam input.");
                return;
            }

            GH_Glulam ghg = raw_g as GH_Glulam;
            if (ghg == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Glulam input.");
                return;
            }

            Glulam g = ghg.Value;

            double offset = 0.0;
            DA.GetData("Amount", ref offset);

            DA.SetData("Output", g.GetBoundingBrep(offset));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ecbb4b98-949a-4b63-8246-4c94b3482939"); }
        }
    }
}