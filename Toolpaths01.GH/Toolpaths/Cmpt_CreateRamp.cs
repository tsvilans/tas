using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core;
using Grasshopper.Kernel.Types;
using tas.Core.Util;

namespace tas.Machine.GH.Components
{
    public class Cmpt_CreateRamp : GH_Component
    {
        public Cmpt_CreateRamp()
          : base("Special - Ramp", "Ramp",
              "Create ramp that conforms to underlying polyline.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddGenericParameter("Path", "P", "Input polyline.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Workplane", "WP", "Active workplane for ramp.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Ramp Height", "H", "Ramp height.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter("Ramp Length", "L", "Ramp length.", GH_ParamAccess.item, 18.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Output ramp.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging output.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane p = new Plane();
            double height = 0.0, length = 0.0;
            // gather and convert, if necessary, input curves
            List<Curve> in_crvs = new List<Curve>();
            List<object> iObjs = new List<object>();

            if (!DA.GetDataList("Path", iObjs))
                return;
            if (iObjs == null || iObjs.Count < 1)
               return;

            List<Path> ppolys = new List<Path>();
            for (int i = 0; i < iObjs.Count; ++i)
            {
                if (iObjs[i] is Curve)
                    ppolys.Add(new Path(Misc.CurveToPolyline(iObjs[i] as Curve, 1.0)));
                else if (iObjs[i] is GH_Curve)
                    ppolys.Add(new Path(Misc.CurveToPolyline((iObjs[i] as GH_Curve).Value, 1.0)));
                else if (iObjs[i] is Path)
                    ppolys.Add(iObjs[i] as Path);
                else if (iObjs[i] is GH_tasPath)
                    ppolys.Add((iObjs[i] as GH_tasPath).Value);
            }

            //List<Polyline> in_paths = Util.CurvesToPolylines(in_crvs, 1.0);

            DA.GetData("Workplane", ref p);
            DA.GetData("Ramp Height", ref height);
            DA.GetData("Ramp Length", ref length);

            string debug = "";
            List<Path> ramps = new List<Path>();
            for (int i = 0; i < ppolys.Count; ++i)
            {
                ramps.Add(Path.CreateRamp(ppolys[i], p, height, length));//, ref debug));
            }

            DA.SetDataList("Path", ramps.Select(x => new GH_tasPath(x)));
            DA.SetData("debug", debug);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Toolpaths01.GH.Properties.Resources.tas_icons_Ramp_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{d202f06e-b40f-4bb9-80df-57a2833d82b5}"); }
        }
    }
}
