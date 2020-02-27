using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core;
using tas.Core.Types;

namespace tas.Core.GH
{
    public class Cmpt_MergePPolyline : GH_Component
    {
        public Cmpt_MergePPolyline()
          : base("Merge PPolylines", "MPPoly",
              "Merge multiple PPolylines into one..",
              "tasTools", "Test")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PP", "Input PPolyline.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PP", "Merged PPolyline.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_PPolyline> pp = new List<GH_PPolyline>();
            DA.GetDataList("PPolyline", pp);

            PPolyline poly = new PPolyline();

            foreach (GH_PPolyline ppoly in pp)
            {
                poly.AddRange(ppoly.Value);
            }

            DA.SetData(0, new GH_PPolyline(poly));
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
            
            get { return new Guid("{037dca44-cfbe-4351-a938-da4459968cee}"); }
        }
    }
}