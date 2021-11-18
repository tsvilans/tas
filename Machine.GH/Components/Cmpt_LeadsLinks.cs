using System;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH
{
    public class Cmpt_LeadsLinks : GH_Component
    {
        public Cmpt_LeadsLinks()
          : base("Create Leads and Links", "LeadsLinks",
              "Create leads and links for a toolpath.",
              "tasMachine", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Retract plane", "R", "Extra place to retract to inbetween toolpaths.", GH_ParamAccess.item);
            pManager[1].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath with leads and links.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Toolpath tp = null;
            Plane plane = Plane.Unset;

            DA.GetData("Toolpath", ref tp);
            DA.GetData("Retract plane", ref plane);

            if (tp == null) return;

            Toolpath tp2 = tp.Duplicate();

            tp2.CreateLeadsAndLinks(plane);

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