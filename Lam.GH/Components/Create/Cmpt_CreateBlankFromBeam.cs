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
using System.Windows.Forms;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core;

namespace tas.Lam.GH
{
    public class Cmpt_CreateBlankFromBeam : GH_Component
    {
        public Cmpt_CreateBlankFromBeam()
          : base("Blank from Geo", "Blank",
              "Create Glulam from a mesh and guide curve.",
              "tasLam", "Create")
        {
        }

        private bool calculate_data = true;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            ToolStripMenuItem calc_item = new ToolStripMenuItem("Calculate data...", null, onCalcDataClick, "calc_data");
            menu.Items.Add(calc_item);
        }

        private void onCalcDataClick(object sender, EventArgs e)
        {
            ToolStripMenuItem calc_item = sender as ToolStripMenuItem;
            calculate_data = !calculate_data;
            calc_item.Checked = calculate_data;
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "G", "Beam geometry to create blank for.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Guide curve for blank axis.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Extra", "E", "Amount of extra material to expand blank by.", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam blank.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Dimensions", "D", "Precise dimensions of bounding blank, including extra material.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh_input = null;
            Curve c = null;
            double extra = 0.0;

            GeometryBase geo = null;

            if (!DA.GetData("Geo", ref geo))
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No input geometry.");
            if (!DA.GetData("Curve", ref c))
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No guide curve.");
            DA.GetData("Extra", ref extra);

            if (geo is Brep)
                mesh_input = Mesh.CreateFromBrep(geo as Brep, MeshingParameters.QualityRenderMesh).Amalgamate();
            else if (geo is Mesh)
                mesh_input = geo as Mesh;
            else
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to parse geometry.");

            double w, h, l;

            Glulam g = Glulam.CreateGlulamFromBeamGeometry2(c, mesh_input, out w, out h, out l, extra);
            
            DA.SetData("Glulam", new GH_Glulam(g));
            DA.SetDataList("Dimensions", new double[] { w, h, l });
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_GlulamFromBeam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{ad404b50-6aaf-48f3-bda9-c578a8774f9d}"); }
        }
    }
}