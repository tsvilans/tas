using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH.Toolpaths
{
    public class Cmpt_FlatFinish: GH_Component
    {

        public Cmpt_FlatFinish()
          : base("Finishing - Flats", "Flats",
              "Finish strategy for flat areas.",
              "tasTools", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane of toolpath.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddGeometryParameter("Boundary", "Bnd", "Boundary to constrain toolpath to.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry", "Geo", "Drive geometry as GeometryBase.", GH_ParamAccess.item);
            pManager.AddNumberParameter("ToolDiameter", "TD", "Diameter of cutter.", GH_ParamAccess.item, 6.0);
            pManager.AddBooleanParameter("Offset Tool", "OT", "Offset tool from edge to avoid adjacent surfaces.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Calculate", "Calc", "Calculate toolpath.", GH_ParamAccess.item, false);


        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Toolpath", "TP", "Output toolpath.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging info.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_FinishingFlats_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{ed1a8f89-2a6b-45f3-ac5d-f47b25f440be}"); }
        }
    }
}

