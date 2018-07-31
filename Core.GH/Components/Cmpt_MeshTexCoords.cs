using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;

namespace tas.Lam.GH.Components
{
    public class Cmpt_MeshTexCoords: GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Cmpt_GetPlane class.
        /// </summary>
        public Cmpt_MeshTexCoords()
          : base("Get Mesh Texture Coordaintes", "MTex",
              "Encode Mesh texture coordinates.",
              "tasTools", "Test")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Source mesh.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Base64", "B", "Encode as base64 string.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("TexCoords", "TX", "Texture coordinates as human-readable string or base64 string.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh iMesh = new Mesh();
            bool iB = false;

            if (!DA.GetData("Mesh", ref iMesh))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No input mesh supplied.");
                return;
            }

            if (iMesh.TextureCoordinates.Count != iMesh.Vertices.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh contains no texture coordinates.");
                return;
            }

            DA.GetData("Base64", ref iB);

            int N = iMesh.TextureCoordinates.Count;
            //double[] uvs = new double[N * 2];

            System.Text.StringBuilder uv_string = new System.Text.StringBuilder();

            for (int i = 0; i < N; ++i)
            {
                uv_string.Append(string.Format("{0} {1} ", iMesh.TextureCoordinates[i].X, iMesh.TextureCoordinates[i].Y));
                //uvs[i * 2] = iMesh.TextureCoordinates[i].X;
                //uvs[i * 2 + 1] = iMesh.TextureCoordinates[i].Y;
            }

            string strOutput;

            if (iB)
                strOutput = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(uv_string.ToString()));
            else
                strOutput = uv_string.ToString();

            DA.SetData("TexCoords", strOutput);
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
            get { return new Guid("0655b195-257a-4758-9102-38766625601f"); }
        }
    }
}