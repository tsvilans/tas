using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using ClipperLib;
using StudioAvw.Geometry;

using System.Windows.Forms;
using GH_IO.Serialization;

using tas.Core.GH;
using tas.Core.Types;
using tas.Machine.Toolpaths;

namespace tas.Machine.GH.Toolpaths
{
    public class Cmpt_Pocket : GH_Component
    {

        public Cmpt_Pocket()
          : base("Roughing - Pocket Machining", "Pocket",
              "Simple area clearance toolpath strategy.",
              "tasTools", "Toolpaths")
        {
        }

        Plane _workplane;
        List<Curve> _curves;
        Curve _curve;

        ToolSettings Tool = new ToolSettings();
        tasTP_ToolSettings_Form form;

        double _depth;
        bool _calc = false;

        string _debug = "";
        List<PPolyline> _paths;

        // Plane Workplane, Mesh Stock, Mesh Geometry, double MaxDepth, 
        // double Stepover, double Stepdown, bool Calculate, ref object Toolpath
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane of toolpath.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddCurveParameter("Curves", "C", "Pocket curve.", GH_ParamAccess.list);
            pManager.AddNumberParameter("ToolDiameter", "TD", "Diameter of cutter.", GH_ParamAccess.item, 12.0);
            pManager.AddNumberParameter("Stepover", "StpO", "Stepover distance.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter("Stepdown", "StpD", "Stepdown distance.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter("Depth", "D", "Pocket depth.", GH_ParamAccess.item, 0.0);
            //pManager.AddBooleanParameter("Calculate", "Calc", "Calculate toolpath.", GH_ParamAccess.item, false);
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
            //DA.GetData("Calculate", ref this._calc);

            //if (this._calc)
            //{
                // get inputs
                this._workplane = new Plane();
                DA.GetData("Workplane", ref this._workplane);
                this._curve = null;
                _curves = new List<Curve>();

                if (DA.GetDataList("Curves", this._curves))
                {

                    DA.GetData("ToolDiameter", ref this.Tool.ToolDiameter);
                    DA.GetData("Stepover", ref this.Tool.StepOver);
                    DA.GetData("Stepdown", ref this.Tool.StepDown);
                    DA.GetData("Depth", ref this._depth);

                    this._debug = "";

                    Toolpath_Pocket pocket = new Toolpath_Pocket(_curves, 0.01);
                    pocket.Tool = Tool;
                    pocket.Workplane = this._workplane;
                    pocket.Depth = _depth;
                    //pocket.MaxDepth = 30.0;

                    pocket.Calculate();
                    this._paths = pocket.GetPaths();

                    if (this._paths != null)
                        DA.SetDataList("Toolpath", GH_PPolyline.MakeGoo(this._paths));
                }
            //}


            DA.SetData("debug", this._debug);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_Pocket_24x24;
            }
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

        public override Guid ComponentGuid
        {
            get { return new Guid("{11e998fa-b1c8-4936-9496-d8ad3e36a543}"); }
        }
    }
}