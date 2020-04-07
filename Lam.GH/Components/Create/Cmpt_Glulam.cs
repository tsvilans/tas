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
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Lam.GH
{
    public class Cmpt_Glulam : GH_Component
    {
        public Cmpt_Glulam()
          : base("Glulam", "Glulam",
              "Create glulam (updated).",
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
            pManager.AddCurveParameter("Curve", "Crv", "Glulam centreline curve.", GH_ParamAccess.item);
            //pManager.AddNumberParameter("LamWidth", "Lw", "Width of lamellas.", GH_ParamAccess.item, 20.0);
            //pManager.AddNumberParameter("LamHeight", "Lh", "Height of lamellas.", GH_ParamAccess.item, 20.0);
            pManager.AddPlaneParameter("Frames", "F", "Frames to orient cross-section to.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Width", "W", "Approximate width of glulam cross-section (actual width will" +
                "be a multiple of the lamella width).", GH_ParamAccess.item, 80.0);
            pManager.AddNumberParameter("Height", "H", "Approximate height of glulam cross-section (actual height will" +
                "be a multiple of the lamella height).", GH_ParamAccess.item, 80.0);
            //pManager.AddIntegerParameter("Samples", "S", "Number of curve samples.", GH_ParamAccess.item, 50);
            pManager.AddIntegerParameter("Flags", "F", "Bitmask to retrieve geometry, lamella geometry, lamella centrelines.", GH_ParamAccess.item, 0);

            pManager[1].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam object.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Geometry", "Geo", "Output geometry based on flags.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            //double lw = 0, lh = 0;
            double w = 0, h = 0;
            int samples = 0;
            int flags = 0;
            List<Plane> planes = new List<Plane>();
            Plane[] plane_array = null;

            if (!DA.GetData("Curve", ref crv))
                return;

            //DA.GetData("LamWidth", ref lw);
            //DA.GetData("LamHeight", ref lh);
            DA.GetData("Width", ref w);
            DA.GetData("Height", ref h);
            //DA.GetData("Samples", ref samples);
            DA.GetData("Flags", ref flags);

            if (!DA.GetDataList("Frames", planes))
            {
                //Plane p;
                //crv.PerpendicularFrameAt(crv.Domain.Min, out p);
                //plane_array = new Plane[] { p };
            }
            else
                plane_array = planes.ToArray();

            /*
            Lam.GlulamData data = new Lam.GlulamData();
            data.LamHeight = lh > 0.1 ? lh : 20.0;
            data.LamWidth = lw > 0.1 ? lw : 20.0;
            data.NumHeight = h > 0 ? (int)Math.Ceiling(h / lh) : 4;
            data.NumWidth = w > 0 ? (int)Math.Ceiling(w / lw) : 4;
            data.Samples = samples > 1 ? samples : 2;
            */
            Lam.GlulamData data = null;
            data = new GlulamData(crv, w, h, plane_array, (int)Math.Ceiling(crv.GetLength() / GlulamData.DefaultSampleDistance));

            /*
            if (calculate_data)
                data = Lam.GlulamData.FromCurveLimits(crv, w, h, plane_array);
            else
                data = new Lam.GlulamData(1, 1, w, h);
            */

            Lam.Glulam blank = Lam.Glulam.CreateGlulam(crv, plane_array, data);
            //blank.CalculateLamellaSizes(w, h);

            List<object> output = new List<object>();

            if ((flags & 1) > 0)
                output.Add(blank.GetBoundingMesh(0, blank.Data.InterpolationType));
            if ((flags & (1 << 1)) > 0)
                output.Add(blank.GetBoundingBrep());
            if ((flags & (1 << 2)) > 0)
                output.AddRange(blank.GetLamellaCurves());
            if ((flags & (1 << 3)) > 0)
                output.AddRange(blank.GetLamellaMeshes());
            if ((flags & (1 << 4)) > 0)
                output.AddRange(blank.GetLamellaBreps());

            DA.SetDataList("Geometry", output);
            DA.SetData("Glulam", new GH.GH_Glulam(blank));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_FreeformGlulam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{06e11075-db1c-4dd9-a3a2-a5b79eef66c9}"); }
        }
    }
}