#if LEVEL2
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Machine.Toolpaths;

using GH_IO.Serialization;

namespace tas.Machine.GH.Components
{
    public class AreaClearance2_Component : ToolpathBase_Component
    {

        public AreaClearance2_Component()
          : base("Area Clearance 2", "Area Clr2",
              "Imprvoed area clearance toolpath strategy.",
              "tasMachine", UiNames.StrategySection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Toolpaths01.GH.Properties.Resources.tasMachine_AreaClearance2;

        public override Guid ComponentGuid => new Guid("{79739505-72AF-4F75-94DC-94B8B3D481D1}");

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddMeshParameter("Geometry", "G", "Geometry to rough out.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Stock", "S", "Stock model.", GH_ParamAccess.list);
            var boundsId = pManager.AddMeshParameter("Bounds", "B", "Containment boundary model.", GH_ParamAccess.list);

            pManager[boundsId].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> Geo = new List<Mesh>();
            List<Mesh> Stock = new List<Mesh>();
            List<Mesh> Bounds = new List<Mesh>();
            string debug = "";

            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            if (!DA.GetDataList("Geometry", Geo)) return;
            if (!DA.GetDataList("Stock", Stock)) return;
            if (!DA.GetDataList("Bounds", Bounds)) return;

            if (Geo == null || Stock == null) return;

            debug += "Creating Area Clearance strategy...\n";
            var ac = new Toolpath_AreaClearance2(Geo, Stock, Bounds, Tool);

            if (ac.Tool.StepOver > ac.Tool.Diameter) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Stepover exceeds tool diameter!");

            ac.Workplane = Workplane;
            ac.RestHorizontal = 0.0;
            ac.RestVertical = 0.0;
            ac.CheckForUndercuts = true;
            ac.Calculate();
            var paths = ac.GetPaths();

            // See Aanesland + Helen and Hard project from autumn 2024 for
            // a clean up and more quality checking/path simplifying after 
            // this part:

            /* 
             var outputPaths = new List<Path>();

            // Simplify and reverse paths
            // so that they are climb cuts
            for (int i = 0; i < paths.Count; ++i)
            {
            if (paths[i].Count < 2) continue;

            var polyline = new Polyline(paths[i].Select(x => x.Origin));
            polyline.RemoveNearlyEqualSubsequentPoints(5);
            polyline.MergeColinearSegments(0.05, true);

            if (polyline.Count < 2)
            {
                outputPaths.Add(paths[i]);
                continue;
            }

            var path = new Path(polyline, Workplane);
            path.Reverse();
            outputPaths.Add(path);
            }
            */

            DA.SetDataList("Paths", GH_tasPath.MakeGoo(paths));
            //DA.SetData("debug", debug);
        }
    }
}
#endif