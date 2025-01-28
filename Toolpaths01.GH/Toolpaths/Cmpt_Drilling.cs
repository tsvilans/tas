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
using Eto.Forms;

namespace tas.Machine.GH.Components
{
    public class Cmpt_Drilling : ToolpathBase_Component
    {

        public Cmpt_Drilling()
          : base("Drill", "Drill",
              "Flexible machining of holes. WARNING: holes larger than tool diameter "
                +"will use a helical milling strategy that is NOT suitable for drill bits!",
              "tasMachine", UiNames.StrategySection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Toolpaths01.GH.Properties.Resources.tasMachine_Hole;
        public override Guid ComponentGuid => new Guid("{C3BBECAA-A2DB-4941-A853-0C979B20A62A}");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);
            pManager[0].Optional = false;

            pManager.AddNumberParameter("Diameter", "Di", "Diameter of hole.", GH_ParamAccess.item, 6);
            pManager.AddNumberParameter("Depth", "De", "Hole depth.", GH_ParamAccess.item, 0.0);
            var passId = pManager.AddNumberParameter("Pass depth", "P", "Amount to drill for each pass.", GH_ParamAccess.item, 0);
            pManager[passId].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Workplane missing!.");
                return;
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Using default.");
            }

            double depth = 0, diameter = 0, passDepth = 0;

            DA.GetData("Depth", ref depth);
            DA.GetData("Diameter", ref diameter);
            DA.GetData("Pass depth", ref passDepth);

            if (passDepth <= 0) passDepth = depth;

            if (diameter < Tool.Diameter)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Tool diameter is larger than hole diameter!");
            }

            if ((diameter - Tool.Diameter) < 1e-6)
            {
                Message = "Standard";
                var drill = new Toolpath_StandardDrill(Workplane, depth, Tool, passDepth);
                drill.Calculate();

                DA.SetDataList("Paths", drill.GetPaths().Where(x => x != null).Select(x => new GH_tasPath(x)));
            }
            else
            {
                Message = "Helical";
                var hel = new Toolpath_HelicalDrill(new Circle(Workplane, diameter * 0.5), depth, Tool, passDepth, 36);
                hel.Calculate();
                DA.SetDataList("Paths", hel.GetPaths().Where(x => x != null).Select(x => new GH_tasPath(x)));
            }
        }
    }
}