using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
#if OBSOLETE
namespace tas.Machine.GH.Components
{
    public class Cmpt_FlatFinish: ToolpathBase_Component
    {

        public Cmpt_FlatFinish()
          : base("Finishing - Flats", "Flats",
              "Finish strategy for flat areas.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Boundary", "Bnd", "Boundary to constrain toolpath to.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry", "Geo", "Drive geometry as GeometryBase.", GH_ParamAccess.item);
            pManager.AddNumberParameter("ToolDiameter", "TD", "Diameter of cutter.", GH_ParamAccess.item, 6.0);
            pManager.AddBooleanParameter("Offset Tool", "OT", "Offset tool from edge to avoid adjacent surfaces.", GH_ParamAccess.item, false);
            //pManager.AddBooleanParameter("Calculate", "Calc", "Calculate toolpath.", GH_ParamAccess.item, false);


        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Toolpaths01.GH.Properties.Resources.tas_icons_FinishingFlats_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{ed1a8f89-2a6b-45f3-ac5d-f47b25f440be}"); }
        }
    }
}

#endif