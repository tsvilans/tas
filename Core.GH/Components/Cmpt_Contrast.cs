using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core.Util;

namespace tas.Core.GH
{
    public class Cmpt_Contrast : GH_Component
    {
        public Cmpt_Contrast()
          : base("Contrast", "Con",
              "Adjust the contrast of a colour.",
              "tasCore", "Utility")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Colour", "C", "Colour to adjust.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Factor", "F", "Contrast factor.", GH_ParamAccess.item, 1.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Colour", "C", "Output colour.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Color color = Color.Black;
            DA.GetData("Colour", ref color);

            double f = 1.0;
            DA.GetData("Factor", ref f);

            DA.SetData("Colour", Mapping.Contrast(color, f));
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
            get { return new Guid("{d2b51993-624c-402b-a834-fdfad8a3bd13}"); }
        }
    }
}
