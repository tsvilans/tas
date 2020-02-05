using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Windows.Forms;

using tas.Core.GH;
using tas.Machine.Toolpaths;

using GH_IO.Serialization;

namespace tas.Machine.GH.Toolpaths
{
    public class AreaClearance2_Component : GH_Component
    {
        ToolSettings Tool = new ToolSettings();
        tasTP_ToolSettings_Form form;

        public AreaClearance2_Component()
          : base("Roughing - Area Clearance2", "Area Clearance2",
              "Area clearance toolpath strategy.",
              "tasTools", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane for area clearance strategy.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddMeshParameter("Geometry", "G", "Geometry to rough out.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Stock", "S", "Stock model.", GH_ParamAccess.list);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "P", "Toolpath as list of PPolyline objects.", GH_ParamAccess.list);
            pManager.AddGenericParameter("debug", "d", "Debugging output.", GH_ParamAccess.list);
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
            List<Mesh> Geo = new List<Mesh>();
            List<Mesh> Stock = new List<Mesh>();
            Plane CuttingPlane = Plane.WorldXY;
            string debug = "";

            DA.GetData("Workplane", ref CuttingPlane);
            if (!DA.GetDataList("Geometry", Geo)) return;
            if (!DA.GetDataList("Stock", Stock)) return;

            if (Geo == null || Stock == null) return;

            debug += "Creating Area Clearance strategy...\n";
            Toolpath_AreaClearance ac = new Toolpath_AreaClearance(Geo, Stock, Tool);

            if (ac.Tool.StepOver > ac.Tool.ToolDiameter) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Stepover exceeds tool diameter!");
            ac.Workplane = CuttingPlane;
            ac.RestHorizontal = 0.0;
            ac.RestVertical = 0.0;
            ac.CheckForUndercuts = true;
            //ac.MaxDepth = 30.0;

            debug += "Calculating...\n";
            ac.Calculate();

            debug += "Finished.\n";

            var paths = ac.GetPaths();
            debug += "Generated " + paths.Count.ToString() + " paths.\n";

            DA.SetDataList("Paths", GH_PPolyline.MakeGoo(paths));
            DA.SetData("debug", debug);
        }

        public override bool Write(GH_IWriter writer)
        {
            GH_Writer.Write(writer, Tool);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            GH_Writer.Read(reader, ref Tool);
            return base.Read(reader);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_AreaClearance_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c34d5894-7cec-404b-8cf1-89f5df913ba7}"); }
        }
    }
}