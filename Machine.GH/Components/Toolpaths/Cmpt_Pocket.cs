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
    public class Cmpt_Pocket : ToolpathBase_Component
    {

        public Cmpt_Pocket()
          : base("Roughing - Pocket Machining", "Pocket",
              "Simple area clearance toolpath strategy.",
              "tasMachine", "Toolpaths")
        {
        }

        List<Curve> _curves;

        double _depth;

        string _debug = "";
        List<PPolyline> _paths;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddCurveParameter("Curves", "C", "Pocket curve.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Depth", "D", "Pocket depth.", GH_ParamAccess.item, 0.0);
            pManager.AddBooleanParameter("Face", "F", "Facing instead of pocket.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            _curves = new List<Curve>();

            if (DA.GetDataList("Curves", this._curves))
            {
                DA.GetData("Depth", ref this._depth);
                bool facing = false;
                DA.GetData("Face", ref facing);

                _debug = "";

                Toolpath_Pocket pocket = new Toolpath_Pocket(_curves, 0.01, facing);
                pocket.Tool = Tool;
                pocket.Workplane = Workplane;
                pocket.Depth = _depth;
                //pocket.MaxDepth = 30.0;

                pocket.Calculate();
                _paths = pocket.GetPaths();

                if (_paths != null)
                    DA.SetDataList("Paths", GH_PPolyline.MakeGoo(this._paths));
            }

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_Pocket_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{11e998fa-b1c8-4936-9496-d8ad3e36a543}"); }
        }
    }
}