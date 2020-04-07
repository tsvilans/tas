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
    public class Cmpt_MeshGrainDeviation : GH_Component
    {
        public Cmpt_MeshGrainDeviation()
          : base("Mesh Grain Deviation", "GrDev",
              "Calculates the normal deviation from the longitudinal direction of a glulam blank per vertex or mesh face. Can use acos to get the actual angle.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam blank.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Mesh to check for grain deviation.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Faces", "F", "Use faces instead of vertices.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Deviations", "D", "Deviation between 0-1 for each mesh vertex or mesh face.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object m_obj = null;
            Mesh m_mesh = null;
            bool m_faces = false;

            DA.GetData("Mesh", ref m_mesh);
            DA.GetData("Glulam", ref m_obj);
            DA.GetData("Faces", ref m_faces);

            Curve m_curve = null;

            while (true)
            {
                GH_Glulam m_ghglulam = m_obj as GH_Glulam;
                if (m_ghglulam != null)
                {
                    m_curve = m_ghglulam.Value.Centreline;
                    break;
                }

                GH_Curve m_ghcurve = m_obj as GH_Curve;
                if (m_ghcurve != null)
                {
                    m_curve = m_ghcurve.Value;
                    break;
                }

                Glulam m_glulam = m_obj as Glulam;
                if (m_glulam != null)
                {
                    m_curve = m_glulam.Centreline;
                    break;
                }

                m_curve = m_obj as Curve;
                if (m_curve != null)
                {
                    break;
                }
                throw new Exception("Input must be either Glulam or Curve!");
            }

            List<double> deviations = m_mesh.CalculateTangentDeviation(m_curve, m_faces);

            DA.SetDataList("Deviations", deviations);
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
            get { return new Guid("6e2cf01d-962b-4fde-8c9e-aa67d3a239ac"); }
        }
    }
}