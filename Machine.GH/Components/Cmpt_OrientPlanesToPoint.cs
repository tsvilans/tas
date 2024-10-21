using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;

namespace tas.Machine.GH.Components
{
    public class Cmpt_OrientPlanesToPoint : GH_Component
    {
        public Cmpt_OrientPlanesToPoint()
          : base("Orient Targets", "Orient Targets",
              "Orient targets towards a point, maintaining normal direction.",
              "tasMachine", UiNames.PathSection)
        {
        }

        Point3d Focus = Point3d.Origin;
        double Twist = 0;

        Vector3d ToRob;
        Vector3d ToRobProj;
        double dotx, doty;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Planes", "P", "Planes to orient.", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "Point to orient towards.", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddNumberParameter("Twist", "Tw", "Amount of twist/rotational offset from orientation vector.", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Planes", "P", "Oriented planes.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging info.", GH_ParamAccess.item);
        }

        public Plane OrientPlane(Plane p)
        {
            ToRob = new Vector3d(p.Origin - Focus);

            ToRobProj = ToRob.ProjectToPlane(p);

            ToRobProj.Unitize();
            dotx = Vector3d.Multiply(ToRobProj, p.XAxis);
            doty = Vector3d.Multiply(ToRobProj, Vector3d.CrossProduct(p.XAxis, p.ZAxis));

            double angle = doty < 0.0 ? Math.Acos(dotx) : -Math.Acos(dotx);
            p.Transform(Transform.Rotation(angle + Twist, p.ZAxis, p.Origin));

            return p;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> RawInput = new List<object>();
            List<object> RawOutput = new List<object>();

            string debug = "";

            DA.GetData("Point", ref Focus);
            DA.GetData("Twist", ref Twist);
            DA.GetDataList("Planes", RawInput);

            if (RawInput.Count < 1) return;

            debug += RawInput[0].ToString();

            foreach (object obj in RawInput)
            {
                if (obj is Plane)
                    RawOutput.Add(new GH_Plane(OrientPlane((Plane)obj)));
                else if (obj is GH_Plane)
                {
                    RawOutput.Add(new GH_Plane(OrientPlane((obj as GH_Plane).Value)));
                }
                else if (obj is Path)
                {
                    Path pp = obj as Path;
                    for (int i = 0; i < pp.Count; ++i)
                    {
                        pp[i] = OrientPlane(pp[i]);
                    }
                    RawOutput.Add(new GH_tasPath(pp));
                }
                else if (obj is GH_tasPath)
                {
                    Path pp = (obj as GH_tasPath).Value;
                    for (int i = 0; i < pp.Count; ++i)
                    {
                        pp[i] = OrientPlane(pp[i]);
                    }
                    RawOutput.Add(new GH_tasPath(pp));
                }
            }

            DA.SetDataList("Planes", RawOutput);
            DA.SetData("debug", debug);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_OrientTargets_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{2f8c0c15-8c48-40d3-bfa2-94ee6bdfaaed}"); }
        }
    }
}