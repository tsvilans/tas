using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core.Util;

namespace tas.Core.GH
{
    public class Cmpt_Cut : GH_Component
    {
        public Cmpt_Cut()
          : base("Cut", "Cut",
              "Boolean Split and remove small parts.",
              "tasCore", "Utility")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Brep that will be cut.", GH_ParamAccess.item);
            pManager.AddBrepParameter("Cutters", "C", "Brep objects that do the cutting.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Output Brep.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double m_tolerance = 0.01;
            Brep m_brep = null;
            if (!DA.GetData("Brep", ref m_brep))
                return;

            List<Brep> m_cutters = new List<Brep>();
            DA.GetDataList("Cutters", m_cutters);

            DA.SetData("Brep", Misc.CutBrep(m_brep, m_cutters, m_tolerance));
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
            get { return new Guid("{6a607434-ad69-4c8f-bed1-5890736e9a7a}"); }
        }
    }
}
