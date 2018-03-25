using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Windows.Forms;
using GH_IO.Serialization;

using tas.Machine.Toolpaths;
using tas.Core.GH;
using tas.Core.Types;


namespace tas.Machine.GH
{


    public class tasTP_FlowlineFinish_Component : GH_Component
    {

        public tasTP_FlowlineFinish_Component()
          : base("tasToolpath: Finish Flowline", "tasTP: Flowline",
              "Finish strategy that follows UV coordinates of surface.",
              "tasTools", "Machining")
        {
        }

        ToolSettings Tool = new ToolSettings();
        tasTP_ToolSettings_Form form;
        Plane Workplane;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane of toolpath.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddGeometryParameter("Surfaces", "Srf", "Drive surfaces as Breps.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Boundary", "Bnd", "Boundary to constrain toolpath to.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Direction", "D", "Bitmask to control direction and starting point. Switches between u and v directions (bit 1) and start ends (bit 2).", GH_ParamAccess.item, 0);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "TP", "Output toolpath.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging info.", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            menu.Items.Add("Settings...", null);
            menu.ItemClicked += SettingsClicked;
        }

        private void SettingsClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "Settings..." || e.ClickedItem.Text == "Settings...")
            {
                form = new tasTP_ToolSettings_Form(this, Tool);
                if (form != null)
                    form.Show();
                return;
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve boundary = null;
            Workplane = Plane.WorldXY;
            List<Brep> Surfaces = new List<Brep>();
            DA.GetData("Workplane", ref Workplane);
            DA.GetDataList("Surfaces", Surfaces);
            DA.GetData("Boundary", ref boundary);

            int Switch = 0;
            DA.GetData("Direction", ref Switch);

            bool UV, StartEnd;
            UV = (Switch & 1) == 1;
            StartEnd = (Switch & (1 << 1)) == 2;

                if (Surfaces.Count < 1) return;

            List<PPolyline> Paths = new List<PPolyline>();
            for (int i = 0; i < Surfaces.Count; ++i)
            {
                Toolpath_Flowline2 fl = new Toolpath_Flowline2(Surfaces[i], UV, boundary);
                fl.StartEnd = StartEnd;
                fl.Tool = Tool;
                fl.Tolerance = 0.01;
                fl.Workplane = Workplane;

                //fl.MaxDepth = 30.0;

                fl.Calculate();
                Paths.AddRange(fl.GetPaths());
            }

            if (Paths != null)
                DA.SetDataList("Toolpath", GH_PPolyline.MakeGoo(Paths));
            DA.SetData("debug", "");

        }

        public override bool Write(GH_IWriter writer)
        {
            GH.GH_Writer.Write(writer, Tool);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            GH.GH_Writer.Read(reader, ref Tool);
            return base.Read(reader);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_FlowlineFinish_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{1af28954-d464-40df-8931-963af074b8fa}"); }
        }
    }
}

