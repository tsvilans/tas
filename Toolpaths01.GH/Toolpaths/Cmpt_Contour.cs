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
using System.Windows.Forms.VisualStyles;
using Grasshopper.Kernel.Data;
using Grasshopper;
using System.IO;

namespace tas.Machine.GH.Components
{
    public class Cmpt_Contour : ToolpathBase_Component
    {

        public Cmpt_Contour()
          : base("Contour", "Contour",
              "Simple contour cutting strategy.",
              "tasMachine", UiNames.StrategySection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Toolpaths01.GH.Properties.Resources.tasMachine_Contour;
        public override Guid ComponentGuid => new Guid("{2D625B21-ECFC-41D6-B7E4-7C9253486829}");
        public override GH_Exposure Exposure => GH_Exposure.primary;


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager.AddCurveParameter("Curves", "C", "Pocket curve.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Depth", "D", "Pocket depth.", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Offset", "O", "Tool offset. Left (-1), none (0), or right (1).", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //base.RegisterOutputParams(pManager);
            pManager.AddGenericParameter("Paths", "P", "Paths describing the machining strategy.", GH_ParamAccess.tree);

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

            List<Curve> DriveCurves = new List<Curve>();
            double Depth = 0;
            double Tolerance = 0.01;
            int Offset = 0;

            if (!DA.GetDataList("Curves", DriveCurves)) return;


            DA.GetData("Depth", ref Depth);
            DA.GetData("Offset", ref Offset);

            if (Offset < 0) Offset = -1;
            else if (Offset > 0) Offset = 1;

            var contour = new Toolpath_Contour(DriveCurves, (PathOffset)Offset, Tolerance);
            contour.Tool = Tool;
            contour.Workplane = Workplane;
            contour.Depth = Depth;

            contour.Calculate();
            var paths = contour.GetPaths();

            if (paths == null) return;

            var tree = new DataTree<GH_tasPath>();

            for (int i = 0; i < paths.Count; ++i)
            {
                tree.Add(new GH_tasPath(paths[i]), new GH_Path(i % DriveCurves.Count));
            }

            DA.SetDataTree(0, tree);
            
        }
    }
}