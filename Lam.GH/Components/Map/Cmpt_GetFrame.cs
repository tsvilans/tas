using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;

namespace tas.Lam.GH.Components
{
    public class Cmpt_GetPlane : GH_Component
    {
        public Cmpt_GetPlane()
          : base("Glulam Frame", "GFrame",
              "Get Glulam cross-section frame closest to point.",
              "tasLam", "Map")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam to get plane from.", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Point at which to get plane.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip", "F", "Flip plane around Y-axis.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Output plane.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d m_point = Point3d.Unset;
            bool m_flip = false;

            DA.GetData("Point", ref m_point);
            DA.GetData("Flip", ref m_flip);

            // Get Glulam
            Glulam m_glulam = null;
            DA.GetData<Glulam>("Glulam", ref m_glulam);
            if (m_glulam == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid glulam input.");
                return;
            }

            Plane plane = m_glulam.GetPlane(m_point);

            if (m_flip)
                plane = plane.FlipAroundYAxis();

            DA.SetData("Plane", plane);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_GlulamFrame_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("124634ca-ef08-4f05-ba0c-466412322ed7"); }
        }
    }
}