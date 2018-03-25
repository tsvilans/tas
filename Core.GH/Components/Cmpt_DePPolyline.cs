using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Core.GH
{
    public class DePPolyline_Component : GH_Component
    {
        public DePPolyline_Component()
          : base("Deconstruct PPolyline", "DePPoly",
              "Extract planes from PlanePolyline.",
              "tasTools", "Test")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PP", "PlanePolyline", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Output planes.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object Input = null;
            DA.GetData(0, ref Input);

            Types.PPolyline poly = new Types.PPolyline();

            if (Input is Types.PPolyline)
            {
                poly = Input as Types.PPolyline;
            }
            else if (Input is GH.GH_PPolyline)
            {
                poly = (Input as GH.GH_PPolyline).Value;
            }

            DA.SetDataList(0, poly.ToArray());
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
            get { return new Guid("{b944cec9-df5f-40aa-ae94-f22cf94dd457}"); }
        }
    }
}