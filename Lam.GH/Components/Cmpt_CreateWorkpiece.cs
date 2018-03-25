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
    public class Cmpt_CreateWorkpiece : GH_Component
    {
        public Cmpt_CreateWorkpiece()
          : base("Create Glulam Workpiece", "Workpiece",
              "Create glulam workpiece.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Assembly", "A", "Glulam assembly.", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Name of workpiece.", GH_ParamAccess.item, "GlulamWorkpiece");
            pManager.AddPlaneParameter("Plane", "P", "Base plane for workpiece.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Workpiece", "WP", "Output GlulamWorkpiece.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Lam.Assembly assembly = null;
            Plane p = Plane.WorldXY;
            object input = null;

            DA.GetData("Plane", ref p);

            if (!DA.GetData("Assembly", ref input))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Couldn't get Assembly.");
                return;
            }

            if (input is GH_Assembly)
            {
                assembly = (input as GH_Assembly).Value;
            }
            else if (input is GH_Glulam)
            {
                assembly = new Lam.BasicAssembly((input as GH_Glulam).Value, Plane.WorldXY);
            }
            else
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong input type. Must be Glulam or Assembly.");
                return;
            }

            string name = "";
            DA.GetData("Name", ref name);

            Lam.GlulamWorkpiece wp = new Lam.GlulamWorkpiece(assembly, name);
            wp.Plane = p;

            DA.SetData("Workpiece", new GH_GlulamWorkpiece(wp));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_Workpiece_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{61756530-98ed-4037-a7c5-51d60deff17b}"); }
        }
    }
}