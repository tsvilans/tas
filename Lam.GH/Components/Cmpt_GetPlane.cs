using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;

namespace tas.Lam.GH.Components
{
    public class Cmpt_GetPlane : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Cmpt_GetPlane class.
        /// </summary>
        public Cmpt_GetPlane()
          : base("Get Glulam Plane", "GPlane",
              "Get Glulam cross-section plane closest to point.",
              "tasTools", "Glulam")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Glulam to get plane from.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Point", "P", "Point at which to get plane.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Flip", "F", "Flip plane around Y-axis.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "P", "Output plane.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d iPt = Point3d.Unset;
            object iGlulam = null;
            bool iFlip = false;

            DA.GetData("Glulam", ref iGlulam);
            DA.GetData("Point", ref iPt);
            DA.GetData("Flip", ref iFlip);

            Glulam g = iGlulam as Glulam;
            if (g == null) return;

            Plane plane = g.GetPlane(iPt);

            if (iFlip)
            {
                plane = plane.FlipAroundYAxis();
            }

            DA.SetData("Plane", plane);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("124634ca-ef08-4f05-ba0c-466412322ed7"); }
        }
    }
}