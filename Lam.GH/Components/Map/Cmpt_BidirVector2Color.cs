using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;

namespace tas.Lam.GH.Components
{
    public class Cmpt_BidirVector2Color : GH_Component
    {
        public Cmpt_BidirVector2Color()
          : base("Bidir Vector 2 Color", "BV2C",
              "Creates a color mapping for a bidirectional vector (same color for vector and its reverse).",
              "tasLam", "Map")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Vector", "V", "Bidirectional vector.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "Colour vector.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Vector3d m_vec = Vector3d.Unset;
            float x = 0, y = 0, z = 0;

            DA.GetData("Vector", ref m_vec);
            m_vec.Unitize();

            z = (float)m_vec.Z;

            x = z < 0 ? (float)(-m_vec.X / 2 + 0.5) : (float)(m_vec.X / 2 + 0.5); 
            y = z < 0 ? (float)(-m_vec.Y / 2 + 0.5) : (float)(m_vec.Y / 2 + 0.5);

            Color4f m_col = new Color4f(x, y, Math.Abs(z), 1.0f);

            DA.SetData("Color", m_col.AsSystemColor());
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
            get { return new Guid("6bb25ee8-e6c0-4439-95e4-1ad35b9d32c6"); }
        }
    }
}