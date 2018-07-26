
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;

using tas.Core;
using tas.Core.Types;
using tas.Core.GH;

namespace tas.Machine.GH
{
    public class tasTP_OffsetClip : GH_Component
    {

        public tasTP_OffsetClip()
          : base("tasToolpath: Offset and clip", "tasTP: OffsetClip",
              "Offset toolpaths and clip with bounding mesh.",
              "tasTools", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpaths", "TP", "Toolpaths as OrientedPolylines", GH_ParamAccess.list);
            pManager.AddIntegerParameter("NumLayers", "N", "Number of times to offset.", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Distance", "D", "Offset distance between layers.", GH_ParamAccess.item, 6.0);
            pManager.AddMeshParameter("Mesh", "M", "Bounding mesh to clip toolpaths to.", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpaths", "TP", "Offset and clipped toolpaths.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> OP = new List<object>();
            Mesh M = null;
            int N = 0;
            double D = 0;

            DA.GetDataList("Toolpaths", OP);
            DA.GetData("NumLayers", ref N);
            DA.GetData("Distance", ref D);
            if (!DA.GetData("Mesh", ref M))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh not found.");
                return;
            }

            if (OP.Count < 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No paths collected.");
                return;
            }

            List<PPolyline> paths = new List<PPolyline>();
            for (int i = 0; i < OP.Count; ++i)
            {
                PPolyline op = (OP[i] as GH_PPolyline).Value;
                if (op == null) continue;
                paths.Add(op);
            }

            if (paths.Count < 1)
                return;

            List<PPolyline> layers = new List<PPolyline>();

            for (int i = N; i > 0; --i)
            {
                foreach (PPolyline op in paths)
                {
                    List<Plane> new_planes = new List<Plane>();
                    for (int j = 0; j < op.Count; ++j)
                    {
                        Plane p = op[j];
                        p.Origin = op[j].Origin + op[j].ZAxis * D * i;
                        new_planes.Add(p);
                    }

                    PPolyline layer = new PPolyline(new_planes);
                    var clipped = ClipPathWithMesh(new_planes, M);
                    layers.AddRange(clipped.Select(x => new PPolyline(x)));
                }
            }

            layers.AddRange(paths);

            DA.SetDataList("Toolpaths", layers.Select(x => new GH_PPolyline(x)).ToList());
        }

        List<List<Plane>> ClipPathWithMesh(List<Plane> path, Mesh m)
        {
            List<List<Plane>> new_paths = new List<List<Plane>>();
            List<Plane> np = new List<Plane>();

            bool new_path = true;
            bool inside = false;

            if (m.IsPointInside(path[0].Origin, 0.01, false))
            {
                inside = true;
                new_path = false;
                np.Add(path[0]);
            }


            for (int i = 1; i < path.Count; ++i)
            {
                if (m.IsPointInside(path[i].Origin, 0.01, false))
                {
                    inside = true;
                    if (new_path)
                    {
                        if (np != null && np.Count > 0)
                            new_paths.Add(np);
                        np = new List<Plane>();
                        np.AddRange(IntersectSegment(m, path[i - 1], path[i], ref inside));
                        new_path = false;
                    }
                    np.Add(path[i]);
                }
                else
                {
                    if (inside == true)
                    {
                        np.AddRange(IntersectSegment(m, path[i - 1], path[i], ref inside));
                    }
                    inside = false;
                    new_path = true;
                }
            }

            if (np != null && np.Count > 0)
                new_paths.Add(np);

            return new_paths;
        }

        private List<Plane> IntersectSegment(Mesh m, Plane p1, Plane p2, ref bool inside)
        {
            Line l = new Line(p1.Origin, p2.Origin);

            int[] face_ids;
            Point3d[] pi = Rhino.Geometry.Intersect.Intersection.MeshLine(m, l, out face_ids);

            List<Plane> planes = new List<Plane>();

            if (pi == null || pi.Length < 1)
                return planes;

            //if (pi.Length % 2 == 0)
            //  inside = false;

            double d1, d2, t;

            for (int i = 0; i < pi.Length; ++i)
            {
                d1 = pi[i].DistanceTo(p1.Origin);
                d2 = pi[i].DistanceTo(p2.Origin);
                t = d1 / (d1 + d2);

                planes.Add(Util.Interpolation.InterpolatePlanes2(p1, p2, t));
            }

            return planes;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // return Resources.IconForThisComponent;
                return null;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("4130c839-b946-4382-8a8f-98bb56a5e3d1"); }
        }
    }
}