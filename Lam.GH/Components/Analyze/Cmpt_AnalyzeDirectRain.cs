using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;
using tas.Lam;

namespace tas.Lam.GH.Components
{
    public class Cmpt_AnalyzeDirectRain : GH_Component
    {


        bool m_display_enabled = false;
        PointCloud m_cloud = null;
        System.Drawing.Color m_color = System.Drawing.Color.DodgerBlue;

        public Cmpt_AnalyzeDirectRain()
          : base("Analyze Direct Rain", "Rain",
              "Displays direct rain exposure on mesh.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh", "M", "Mesh to analyze for rainfall.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Samples", "N", "Number of rain samples to use.", GH_ParamAccess.item, 2000);
            pManager.AddNumberParameter("Angle Variance", "AV", "Deviation in radians from -Z rainfall vector.", GH_ParamAccess.item, Rhino.RhinoMath.ToRadians(3));
            pManager.AddBooleanParameter("Enabled", "On", "Switch to enable or disable simulation.", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddNumberParameter("Max curvature", "MaxK", "Maximum curvature found.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData("Enabled", ref m_display_enabled);

            if (m_display_enabled)
            {
                int N = 0;
                List<Mesh> m_input_meshes = new List<Mesh>();
                DA.GetDataList("Mesh", m_input_meshes);

                Mesh m_mesh = new Mesh();
                for (int i = 0; i < m_input_meshes.Count; ++i)
                {
                    m_mesh.Append(m_input_meshes[i]);
                }

                DA.GetData("Samples", ref N);

                BoundingBox bb = m_mesh.GetBoundingBox(true);
                Plane m_plane = Plane.WorldXY;
                m_plane.Origin = new Point3d(bb.Min.X, bb.Min.Y, bb.Max.Z + 10.0);
                Rectangle3d rec = new Rectangle3d(m_plane, bb.Max.X - bb.Min.X, bb.Max.Y - bb.Min.Y);

                Random rnd = new Random();
                double hrecW = rec.Width / 2;
                double hrecH = rec.Height / 2;
                double hit_dist = 0;

                m_cloud = new PointCloud();

                for (int i = 0; i < N; ++i)
                {
                    Ray3d ray = new Ray3d(new Point3d(
                      rec.Center.X - hrecW + rnd.NextDouble() * rec.Width,
                      rec.Center.Y - hrecH + rnd.NextDouble() * rec.Height,
                      rec.Center.Z),
                      -Vector3d.ZAxis);

                    hit_dist = Rhino.Geometry.Intersect.Intersection.MeshRay(m_mesh, ray);
                    if (hit_dist > 0)
                        m_cloud.Add(ray.Position + ray.Direction * hit_dist);
                }
            }

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("63c09f46-1608-4ce3-b579-0068c6450f8f"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (m_display_enabled && m_cloud != null)
            {
                args.Display.DrawPointCloud(m_cloud, 1, m_color);
            }
        }
    }
}