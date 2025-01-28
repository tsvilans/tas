
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;


using Grasshopper;
using Grasshopper.Kernel.Data;
using tas.Core.Util;
using tas.Machine.GH.Properties;

namespace tas.Machine.GH.Components
{
    public class Cmpt_ClipPath : GH_Component
    {

        public Cmpt_ClipPath()
          : base("Clip Path", "ClipP",
              "Clip path with bounding mesh.",
              "tasMachine", UiNames.PathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Resources.tasMachine_ClipPath;
        public override Guid ComponentGuid => new Guid("770AF309-6D60-422A-A764-49EAC23AD939");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to offset and clip.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Bounding mesh to clip path to.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Clipped path.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_tasPath ghPath = null;
            Mesh M = null;

            DA.GetData("Path", ref ghPath);

            if (!DA.GetData("Mesh", ref M))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh not found.");
                return;
            }

            if (ghPath == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No paths collected.");
                return;
            }

            var path = ghPath.Value;
            var clipped = ClipPathWithMesh(path, M);

 
            DA.SetDataList("Path", GH_tasPath.MakeGoo(clipped));
            //DA.SetDataList("Toolpaths", layers.Select(x => new GH_PPolyline(x)).ToList());
        }

        List<Path> ClipPathWithMesh(Path path, Mesh m)
        {
            var new_paths = new List<Path>();
            var np = new Path();

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
                        np = new Path();
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
    }
}