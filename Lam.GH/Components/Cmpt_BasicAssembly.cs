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
    public class Cmpt_BasicAssembly : GH_Component
    {
        public Cmpt_BasicAssembly()
          : base("Create BasicAssembly", "BasicAssembly",
              "Create a BasicAssembly object from a single glulam. This just wraps a single glulam into the Assembly format.",
              "tasLam", "Create")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Single glulam.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Optional plane for the Assembly.", GH_ParamAccess.item, Plane.WorldXY);
            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("BasicAssembly", "BA", "BasicAssembly object.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Lam.Glulam g = null;
            Plane p = Plane.WorldXY;

            if (!DA.GetData("Glulam", ref g))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get Glulam.");
                return;
            }

            DA.GetData("Plane", ref p);

            BasicAssembly ba = new BasicAssembly(g, p);

            DA.SetData("BasicAssembly", new GH.GH_Assembly(ba));

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_Assembly_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{13c85cc7-3de1-4621-bdf6-86b46683d279}"); }
        }
    }
}