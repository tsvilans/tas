using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;
using tas.Core.Types;
using tas.Core.GH;

namespace tas.Machine.GH
{
    public class tasTP_OrientTargetsTowardsPoint_Component : GH_Component
    {
        public tasTP_OrientTargetsTowardsPoint_Component()
          : base("tasToolpath: Orient Targets", "tasTP: Orient Targets",
              "Orient targets towards a point, maintaining normal direction.",
              "tasTools", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "T", "Targets as list of planes or Oriented Polyline.", GH_ParamAccess.list);
            pManager.AddPointParameter("Point", "P", "Point to orient towards", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddNumberParameter("Twist", "Tw", "Amount of twist/rotational offset from orientation vector.", GH_ParamAccess.item, 0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "T", "Oriented targets.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging info.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> RawInput = new List<object>();
            List<object> RawOutput = new List<object>();

            double Twist = 0.0;
            Point3d Point = Point3d.Origin;
            string debug = "";

            DA.GetData("Point", ref Point);
            DA.GetData("Twist", ref Twist);
            DA.GetDataList("Targets", RawInput);

            bool IsOriPoly = false; // false == Planes, true == OrientedPolylines

            if (RawInput.Count < 1) return;

            debug += RawInput[0].ToString();

            if (RawInput[0] is Plane) IsOriPoly = false;
            else if (RawInput[0] is PPolyline || RawInput[0] is GH_PPolyline) IsOriPoly = true;

            if (IsOriPoly)
            {

                List<PPolyline> PathsIn = new List<PPolyline>();
                List<GH_PPolyline> PathsOut = new List<GH_PPolyline>();

                foreach (object O in RawInput)
                {
                    if (O is PPolyline)
                    {
                        PathsIn.Add(O as PPolyline);
                    }
                    else if (O is GH_PPolyline)
                    {
                        PathsIn.Add((O as GH_PPolyline).Value);
                    }
                }

                Vector3d ToRob;
                Vector3d ToRobProj;
                double dotx, doty;
                PPolyline OriPoly;
                List<Plane> NewPlanes = new List<Plane>();

                List<Line> Lines = new List<Line>();

                foreach (PPolyline OP in PathsIn)
                {
                    NewPlanes = new List<Plane>();
                    for (int i = 0; i < OP.Count; ++i)
                    {
                        Plane P = new Plane(OP[i]);
                        ToRob = new Vector3d(OP[i].Origin - Point);

                        ToRobProj = Util.ProjectToPlane(ToRob, OP[i]);

                        ToRobProj.Unitize();
                        dotx = Vector3d.Multiply(ToRobProj, P.XAxis);
                        doty = Vector3d.Multiply(ToRobProj, Vector3d.CrossProduct(P.XAxis, P.ZAxis));

                        double angle = doty < 0.0 ? Math.Acos(dotx) : -Math.Acos(dotx);
                        P.Transform(Transform.Rotation(angle + Twist, P.ZAxis, P.Origin));

                        NewPlanes.Add(P);
                    }

                    OriPoly = new PPolyline(NewPlanes);
                    RawOutput.Add(new GH_PPolyline(OriPoly));
                }

            }
            else
            {
                List<Plane> Planes = new List<Plane>();
                for (int i = 0; i < RawInput.Count; ++i)
                {
                    if (RawInput[i] is Plane)
                    {
                        Planes.Add((Plane)RawInput[i]);
                    }
                }

                Vector3d ToRob;
                Vector3d ToRobProj;
                double dotx, doty;

                List<Plane> NewPlanes = new List<Plane>();

                for (int i = 0; i < Planes.Count; ++i)
                {
                    Plane P = new Plane(Planes[i]);
                    ToRob = new Vector3d(Planes[i].Origin - Point);

                    ToRobProj = Util.ProjectToPlane(ToRob, Planes[i]);

                    ToRobProj.Unitize();
                    dotx = Vector3d.Multiply(ToRobProj, P.XAxis);
                    doty = Vector3d.Multiply(ToRobProj, Vector3d.CrossProduct(P.XAxis, P.ZAxis));

                    double angle = doty < 0.0 ? Math.Acos(dotx) : -Math.Acos(dotx);
                    P.Transform(Transform.Rotation(angle + Twist, P.ZAxis, P.Origin));

                    RawOutput.Add(new GH_Plane(P));
                }
            }

            DA.SetDataList("Targets", RawOutput);
            DA.SetData("debug", debug);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_OrientTargets_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{2f8c0c15-8c48-40d3-bfa2-94ee6bdfaaed}"); }
        }
    }
}