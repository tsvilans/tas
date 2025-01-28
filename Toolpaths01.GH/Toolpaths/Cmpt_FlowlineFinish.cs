using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Windows.Forms;
using GH_IO.Serialization;

using tas.Machine.Toolpaths;
using tas.Core.GH;
using tas.Core.Types;
using tas.Core;

namespace tas.Machine.GH.Components
{
    public class Cmpt_Flowline : ToolpathBase_Component
    {

        public Cmpt_Flowline()
          : base("Flowline", "Flowline",
              "Finish strategy that follows UV coordinates of surface.",
              "tasMachine", UiNames.StrategySection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Toolpaths01.GH.Properties.Resources.tasMachine_SurfaceOffset;
        public override Guid ComponentGuid => new Guid("{1af28954-d464-40df-8931-963af074b8fa}");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        bool ZigZag = true;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddGeometryParameter("Surfaces", "Srf", "Drive surfaces as Breps.", GH_ParamAccess.list);
            int bnd = pManager.AddGeometryParameter("Boundary", "Bnd", "Boundary to constrain toolpath to.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Direction", "D", "Bitmask to control direction and starting point. Switches between u and v directions (bit 1) and start ends (bit 2).", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("ZigZag", "Z", "Alternate start points of path.", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Tolerance", "T", "Tolerance for converting curves to polylines.", GH_ParamAccess.item, 0.01);
            pManager[bnd].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve boundary = null;
            double tolerance = 0.01;
            List<Brep> Surfaces = new List<Brep>();

            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            DA.GetDataList("Surfaces", Surfaces);
            DA.GetData("Boundary", ref boundary);
            DA.GetData("ZigZag", ref ZigZag);
            DA.GetData("Tolerance", ref tolerance);

            int Switch = 0;
            DA.GetData("Direction", ref Switch);

            bool UV, StartEnd;
            UV = (Switch & 1) == 1;
            StartEnd = (Switch & (1 << 1)) == 2;

                if (Surfaces.Count < 1) return;

            List<Path> Paths = new List<Path>();
            for (int i = 0; i < Surfaces.Count; ++i)
            {
                Toolpath_Flowline2 fl = new Toolpath_Flowline2(Surfaces[i], UV, boundary);
                fl.StartEnd = StartEnd;
                fl.Tool = Tool;
                fl.Tolerance = tolerance;
                fl.Workplane = Workplane;

                //fl.MaxDepth = 30.0;

                fl.Calculate();

                List<Path> paths = fl.GetPaths();
                if (ZigZag)
                {
                    Path zz_path = new Path();
                    for (int j = 0; j < paths.Count; ++j)
                    {
                        if (j.Modulus(2) > 0)
                            paths[j].Reverse();
                        zz_path.AddRange(paths[j]);                        
                    }
                    Paths.Add(zz_path);
                }
                else
                    Paths.AddRange(paths);
            }

            if (Paths != null)
                DA.SetDataList("Paths", GH_tasPath.MakeGoo(Paths));
            //DA.SetData("debug", "");

        }
    }
}

