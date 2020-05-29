using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using tas.Core;
using tas.Lam;

namespace tas.Lam.GH.Components
{
    public class Cmpt_MeshCurvatureXY : GH_Component
    {
        public Cmpt_MeshCurvatureXY()
          : base("Glulam Curvature", "GK",
              "Calculates the curvature values at each vertex of a Glulam mesh.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam blank.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Glulam mesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Curvature X", "Kx", "Curvature value for each mesh vertex in the X-direction.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Curvature Y", "Ky", "Curvature value for each mesh vertex in the Y-direction.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {


            Glulam m_glulam = null;

            if (!DA.GetData("Glulam", ref m_glulam))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No glulam input.");
                return;
            }

            Mesh m = m_glulam.GetBoundingMesh();

            List<double> kx_values = new List<double>();
            List<double> ky_values = new List<double>();
            double t;

            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m_glulam.Centreline.ClosestPoint(m.Vertices[i], out t);
                Plane frame = m_glulam.GetPlane(t);

                Vector3d offset = m.Vertices[i] - frame.Origin;

                double offset_x = offset * frame.XAxis;
                double offset_y = offset * frame.YAxis;

                Vector3d kv = m_glulam.Centreline.CurvatureAt(t);
                double k = kv.Length;

                kv.Unitize();

                double r = (1 / k) - offset * kv;

                k = 1 / r;

                double kx = k * (kv * frame.XAxis);
                double ky = k * (kv * frame.YAxis);

                kx_values.Add(Math.Abs(kx));
                ky_values.Add(Math.Abs(ky));
            }

            DA.SetData("Mesh", m);
            DA.SetDataList("Curvature X", kx_values);
            DA.SetDataList("Curvature Y", ky_values);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CurvatureAnalysis_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("b2b055d7-e2fd-4d32-8e72-599e996d2110"); }
        }
    }
}