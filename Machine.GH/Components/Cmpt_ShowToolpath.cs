using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace tas.Machine.GH.Components
{
    public class Cmpt_ShowToolpath : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Cmpt_ShowToolpath class.
        /// </summary>
        public Cmpt_ShowToolpath()
          : base("Show Toolpath", "Show",
              "Display toolpath.",
              "tasTools", "Machining")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath to display.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Paths", "P", "Toolpath as Polylines.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object iToolpath = null;

            DA.GetData("Toolpath", ref iToolpath);

            Toolpath tp = iToolpath as Toolpath;
            if (tp == null) return;

            List<GH_Curve> plines = new List<GH_Curve>();

            for (int i = 0; i < tp.Paths.Count; ++i)
            {
                plines.Add(new GH_Curve(new Polyline(tp.Paths[i].Select(x => x.Plane.Origin)).ToNurbsCurve()));
            }

            DA.SetDataList("Paths", plines);
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
            get { return new Guid("af624288-def3-4a30-b7d5-7bb3636e1624"); }
        }
    }
}