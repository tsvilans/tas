#if OBSOLETE
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;

namespace tas.Lam.GH.Components
{
    public class Cmpt_GetFibreCutting : GH_Component
    {
        public Cmpt_GetFibreCutting()
          : base("Cmpt_GetFibreCutting", "Nickname",
              "Description",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input mesh to check against fibre direction.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "Fibre direction curve.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle", "A", "Angle limit for fibre cutting (radians).", GH_ParamAccess.item, Rhino.RhinoMath.ToRadians(5.0));
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Mesh faces which are past the fibre cutting limit.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            Curve crv = null;
            double angle = 0.0;

            DA.GetData("Mesh", ref m);
            DA.GetData("Curve", ref crv);
            DA.GetData("Angle", ref angle);

            if (m.FaceNormals.Count < m.Faces.Count)
                m.FaceNormals.ComputeFaceNormals();

            Mesh mm = crv.MapToCurveSpace(m);
            Mesh out_mesh = new Mesh();

            out_mesh.Vertices.AddVertices(m.Vertices);

            if (mm.FaceNormals.Count < mm.Faces.Count)
                mm.FaceNormals.ComputeFaceNormals();

            double dot;
            for (int i = 0; i < mm.FaceNormals.Count; ++i)
            {
                dot = Math.Abs(mm.FaceNormals[i] * Vector3d.ZAxis);
                if (dot > Math.Sin(angle))
                {
                    out_mesh.Faces.AddFace(mm.Faces[i]);
                    out_mesh.FaceNormals.AddFaceNormal(m.FaceNormals[i]);
                }
            }

            out_mesh.Compact();

            DA.SetData("Mesh", out_mesh);


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
            get { return new Guid("3c41ec63-4155-4ce9-920b-8cb12c42f55a"); }
        }
    }
}

#endif