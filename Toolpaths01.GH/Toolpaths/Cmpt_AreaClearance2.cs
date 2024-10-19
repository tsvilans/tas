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
          : base("Area Clearance", "Area Clr",
              "Area clearance toolpath strategy.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddMeshParameter("Geometry", "G", "Geometry to rough out.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Stock", "S", "Stock model.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Mesh> Geo = new List<Mesh>();
            List<Mesh> Stock = new List<Mesh>();
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

            if (Geo == null || Stock == null) return;

            debug += "Creating Area Clearance strategy...\n";
            Toolpath_AreaClearance ac = new Toolpath_AreaClearance(Geo, Stock, Tool);

            if (ac.Tool.StepOver > ac.Tool.Diameter) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Stepover exceeds tool diameter!");

            ac.Workplane = Workplane;
            ac.RestHorizontal = 0.0;
            ac.RestVertical = 0.0;
            ac.CheckForUndercuts = true;
            ac.Calculate();
            var paths = ac.GetPaths();

            DA.SetDataList("Paths", GH_tasPath.MakeGoo(paths));
            //DA.SetData("debug", debug);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Toolpaths01.GH.Properties.Resources.tas_icons_AreaClearance_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{c34d5894-7cec-404b-8cf1-89f5df913ba7}"); }
        }
    }
}