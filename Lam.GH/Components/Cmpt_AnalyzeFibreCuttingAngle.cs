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
    public class Cmpt_AnalyzeFibreCuttingAngle : GH_Component
    {
        public Cmpt_AnalyzeFibreCuttingAngle()
          : base("Analyze Fibre-cutting Angle", "FibreAngle",
              "Displays faces that exceed a specified fibre angle.",
              "tasTools", "Analysis")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam blank to compare component to.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "M", "Mesh to check for fibre cutting angle.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "Fibre-cutting angle (default is 5 degrees).", GH_ParamAccess.item, Rhino.RhinoMath.ToRadians(5));
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Face Indices", "FI", "Indices of faces that exceed the" +
                " specified fibre-cutting angle.", GH_ParamAccess.list);
            pManager.AddGenericParameter("debug", "d", "Debugging.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object m_obj = null;
            Mesh m_mesh = null;
            double m_angle = Rhino.RhinoMath.ToRadians(5);

            DA.GetData("Angle", ref m_angle);
            DA.GetData("Mesh", ref m_mesh);
            DA.GetData("Glulam", ref m_obj);

            while (true)
            {
                GH_Glulam m_ghglulam = m_obj as GH_Glulam;
                if (m_ghglulam != null)
                {
                    m_mesh = m_ghglulam.Value.MapToCurveSpace(m_mesh);
                    break;
                }

                GH_Curve m_ghcurve = m_obj as GH_Curve;
                if (m_ghcurve != null)
                {
                    m_mesh = m_ghcurve.Value.MapToCurveSpace(m_mesh);
                    break;
                }

                Glulam m_glulam = m_obj as Glulam;
                if (m_glulam != null)
                {
                    m_mesh = m_glulam.MapToCurveSpace(m_mesh);
                    break;
                }

                Curve m_curve = m_obj as Curve;
                if (m_curve != null)
                {
                    m_mesh = m_curve.MapToCurveSpace(m_mesh);
                    break;
                }
                throw new Exception("Input must be either Glulam or Curve!");
            }

            var indices = m_mesh.CheckFibreCuttingAngle(m_angle);

            DA.SetDataList("Face Indices", indices);
            DA.SetData("debug", m_mesh);
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
            get { return new Guid("4d26f5c3-d918-429e-8060-08807bc0c3a0"); }
        }
    }
}