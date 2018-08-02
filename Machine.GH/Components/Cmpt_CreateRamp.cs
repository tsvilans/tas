﻿using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core;
using tas.Core.Types;
using tas.Core.GH;


namespace tas.Machine.GH
{
    public class tasTP_CreateRamp_Component : GH_Component
    {
        public tasTP_CreateRamp_Component()
          : base("tasToolpath: Ramp", "tasTP: Ramp",
              "Create ramp that conforms to underlying polyline.",
              "tasTools", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Poly", "Input polyline.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Workplane", "WP", "Active workplane for ramp.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Ramp Height", "H", "Ramp height.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter("Ramp Length", "L", "Ramp length.", GH_ParamAccess.item, 18.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Poly", "Output ramp.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging output.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane p = new Plane();
            double height = 0.0, length = 0.0;
            // gather and convert, if necessary, input curves
            List<Curve> in_crvs = new List<Curve>();

            if (!DA.GetDataList("Polyline", in_crvs))
                return;
            if (in_crvs == null || in_crvs.Count < 1)
                return;

            List<Polyline> in_paths = Util.CurvesToPolylines(in_crvs, 1.0);

            DA.GetData("Workplane", ref p);
            DA.GetData("Ramp Height", ref height);
            DA.GetData("Ramp Length", ref length);

            string debug = "";
            List<PPolyline> ramps = new List<PPolyline>();
            for (int i = 0; i < in_paths.Count; ++i)
            {
                ramps.Add(Util.CreateRamp((PPolyline)in_paths[i], p, height, length));//, ref debug));
            }

            DA.SetDataList("Polyline", ramps.Select(x => new GH_PPolyline(x)));
            DA.SetData("debug", debug);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_Ramp_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{d202f06e-b40f-4bb9-80df-57a2833d82b5}"); }
        }
    }
}