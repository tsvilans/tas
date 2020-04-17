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
    public class Cmpt_OffsetGlulam : GH_Component
    {
        public Cmpt_OffsetGlulam()
          : base("Offset Glulam", "OfGlulam",
              "Offset Glulam in its local space.",
              "tasLam", "Modify")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam to offset.", GH_ParamAccess.item);
            pManager.AddNumberParameter("OffsetX", "X", "Amount to offset in X direction.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("OffsetY", "Y", "Amount to offset in Y direction.", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Offset Glulam.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Glulam ghg = null;

            if (!DA.GetData("Glulam", ref ghg))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not get Glulam.");
                return;
            }

            double x = 0.0, y = 0.0;
            DA.GetData("OffsetX", ref x);
            DA.GetData("OffsetY", ref y);

            Glulam g = ghg.Value;

            Curve crv = g.CreateOffsetCurve(x, y);


           // GlulamData data = GlulamData.FromCurveLimits(crv,g.Data.NumWidth * g.Data.LamWidth, g.Data.NumHeight * g.Data.LamHeight, g.GetAllPlanes());

            //data.Samples = g.Data.Samples;

            Glulam g2 = Glulam.CreateGlulam(crv, g.Orientation.Duplicate(), g.Data.Duplicate());

            DA.SetData("Glulam", new GH.GH_Glulam(g2));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_OffsetGlulam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("196aab65-78b2-414e-800d-5ef39b596824"); }
        }
    }
}