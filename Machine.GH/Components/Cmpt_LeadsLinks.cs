using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core.GH;
using tas.Core.Types;

namespace tas.Machine.GH
{
    public class Cmpt_LeadsLinks : GH_Component
    {
        public Cmpt_LeadsLinks()
          : base("Create Leads and Links", "LeadsLinks",
              "Create leads and links for a toolpath.",
              "tasTools", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath.", GH_ParamAccess.item);

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath with leads and links.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Toolpath tp = null;

            DA.GetData("Toolpath", ref tp);

            if (tp == null) return;

            Toolpath tp2 = tp.Duplicate();

            tp2.CreateLeadsAndLinks();

            DA.SetData("Toolpath", new GH_Toolpath(tp2));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_LeadsLinks_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("eef6b3c1-fba6-4cd5-bb14-df106a5aaba6"); }
        }
    }
}