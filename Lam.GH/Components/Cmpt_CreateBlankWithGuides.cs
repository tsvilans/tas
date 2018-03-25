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

using tas.Core;

namespace tas.Lam.GH
{
    public class Cmpt_CreateBlankWithGuides : GH_Component
    {
        public Cmpt_CreateBlankWithGuides()
          : base("Blank with Guides", "Blank",
              "Create Glulam with guide curves.",
              "tasTools", "Glulam")
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
            pManager.AddCurveParameter("Curve", "C", "Axis curve of Glulam.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Guides", "G", "List of curves to guide the Y-axis of Glulam.", GH_ParamAccess.list);
            //pManager.AddNumberParameter("LamWidth", "Lw", "Width of lamellas.", GH_ParamAccess.item);
            //pManager.AddNumberParameter("LamHeight", "Lh", "Height of lamellas.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Width of Glulam.", GH_ParamAccess.item, 80.0);
            pManager.AddNumberParameter("Height", "H", "Height of Glulam.", GH_ParamAccess.item, 80.0);
            pManager.AddIntegerParameter("Samples", "S", "Number of samples.", GH_ParamAccess.item, 50);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam blank.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            List<Curve> guides = new List<Curve>();

            //double lw = 0, lh = 0;
            double w = 0, h = 0;
            int samples = 0;
            int flags = 0;
            List<Plane> planes = new List<Plane>();

            if (!DA.GetData("Curve", ref crv))
                return;
            if (!DA.GetDataList("Guides", guides))
                return;

            for (int i = 0; i < guides.Count; ++i)
            {
                Point3d pt = guides[i].PointAtStart;
                Vector3d vec = guides[i].TangentAtStart;

                double t;
                crv.ClosestPoint(pt, out t);

                Plane nor_plane = new Plane(crv.PointAt(t), crv.TangentAt(t));

                vec = vec.ProjectToPlane(nor_plane);

                Plane frame = new Plane(nor_plane.Origin, Vector3d.CrossProduct(crv.TangentAt(t), vec), vec);

                planes.Add(frame);
            }

            //DA.GetData("LamWidth", ref lw);
            //DA.GetData("LamHeight", ref lh);
            DA.GetData("Width", ref w);
            DA.GetData("Height", ref h);
            DA.GetData("Samples", ref samples);


            Lam.GlulamData data = null;
            if (calculate_data)
                data = Lam.GlulamData.FromCurveLimits(crv, w, h);
            else
                data = new Lam.GlulamData(1, 1, w, h);
            //data.LamHeight = lh > 0.001 ? lh : 20.0;
            //data.LamWidth = lw > 0.001 ? lw : 20.0;
            //data.NumHeight = h > 0 ? (int)Math.Ceiling(h / lh) : 4;
            //data.NumWidth = w > 0 ? (int)Math.Ceiling(w / lw) : 4;
            data.Samples = samples > 1 ? samples : 2;

            Lam.Glulam blank = Lam.Glulam.CreateGlulam(crv, planes.ToArray(), data);
            DA.SetData("Glulam", new GH.GH_Glulam(blank));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_BlankWithGuides_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("6e51b441-a15e-4202-bcd2-3dd450f417c0"); }
        }
    }
}