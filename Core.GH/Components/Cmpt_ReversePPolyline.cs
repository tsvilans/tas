#if OBSOLETE
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core.Types;

namespace tas.Core.GH
{
    public class Cmpt_ReversePPolyline : GH_Component
    {
        public Cmpt_ReversePPolyline()
          : base("Reverse PPolyline", "RPPoly",
              "Reverse a PlanePolyline.",
              "tasCore", "Oriented Polyline")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PP", "PlanePolyline", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PP", " Reversed PPolyline", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            GH_PPolyline pp = null;
            DA.GetData("PPolyline", ref pp);

            if (pp == null) return;

            PPolyline flipped = new PPolyline(pp.Value);
            flipped.Reverse();

            DA.SetData("PPolyline", new GH_PPolyline(flipped));
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                return Properties.Resources.icon_deoriented_polyline_component_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{2bbe2817-e099-45e4-80d5-53e9981f93a1}"); }
        }
    }
}
#endif