
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;


using Grasshopper;
using Grasshopper.Kernel.Data;
using tas.Core.Util;

namespace tas.Machine.GH
{
    public class tasTP_OffsetClip : GH_Component
    {

        public tasTP_OffsetClip()
          : base("Offset and clip", "OffsetClip",
              "Offset toolpaths and clip with bounding mesh.",
              "tasMachine", "Machining")
        {
        }

        bool JoinBrokenPaths = true;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpaths", "TP", "Toolpaths as OrientedPolylines", GH_ParamAccess.list);
            pManager.AddIntegerParameter("NumLayers", "N", "Number of times to offset.", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Distance", "D", "Offset distance between layers.", GH_ParamAccess.item, 6.0);
            pManager.AddMeshParameter("Mesh", "M", "Bounding mesh to clip toolpaths to.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Join", "J", "Don't retract between paths on the same layer.", GH_ParamAccess.item, true);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[4].Optional = true;

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
            DA.GetData("Join", ref JoinBrokenPaths);

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

            List<Path> paths = new List<Path>();
            for (int i = 0; i < OP.Count; ++i)
            {
                Path op = (OP[i] as GH_tasPath).Value;
                if (op == null) continue;
                paths.Add(op);
            }

            if (paths.Count < 1)
                return;

            DataTree<Path> layers = new DataTree<Path>();
            //List<PPolyline> layers = new List<PPolyline>();
            GH_Path path;
            int path_counter = 0;
            for (int i = N; i > 0; --i)
            {
                path = new GH_Path(path_counter);
                path_counter++;

                foreach (Path op in paths)
                {
                    List<Plane> new_planes = new List<Plane>();
                    for (int j = 0; j < op.Count; ++j)
                    {
                        Plane p = op[j];
                        p.Origin = op[j].Origin + op[j].ZAxis * D * i;
                        new_planes.Add(p);
                    }

                    Path layer = new Path(new_planes);
                    var clipped = ClipPathWithMesh(new_planes, M);
                    if (clipped.Count < 1) continue;

                    if (JoinBrokenPaths)
                    {
                        Path joined = new Path();
                        for (int j = 0; j < clipped.Count; ++j)
                        {
                            List<Plane> temp = clipped[j];
                            //if (Util.Modulus(j, 2) > 0)
                            //    temp.Reverse();

                            //joined.AddRange(temp);
                            joined.AddRange(clipped[j]);
                        }
                        layers.Add(joined, path);
                    }
                    else
                    {
                        for (int j = 0; j < clipped.Count; ++j)
                        {
                            layers.Add(new Path(clipped[j]), path);
                        }
                        //layers.AddRange(clipped.Select(x => new PPolyline(x)), path);
                    }
                }
            }

            layers.AddRange(paths, new GH_Path(path_counter));
            DataTree<GH_tasPath> gh_layers = new DataTree<GH_tasPath>();
            for (int i = 0; i < layers.BranchCount; ++i)
            {
                path = new GH_Path(i);
                for (int j = 0; j < layers.Branches[i].Count; ++j)
                {
                    gh_layers.Add(new GH_tasPath(layers.Branches[i][j]), path);
                }
            }
            DA.SetDataTree(0, gh_layers);
            //DA.SetDataList("Toolpaths", layers.Select(x => new GH_PPolyline(x)).ToList());
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

                planes.Add(Interpolation.InterpolatePlanes2(p1, p2, t));
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