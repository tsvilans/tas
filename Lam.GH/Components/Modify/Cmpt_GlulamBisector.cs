using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Lam.GH.Components
{
    public class Cmpt_GlulamBisector : GH_Component
    {

        public Cmpt_GlulamBisector()
          : base("Glulam Bisector", "GBi",
              "Description",
              "tasLam", "Modify")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam A", "G", "First Glulam.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Glulam B", "G", "Second Glulam.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Bisector surface.", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Glulam gA = null, gB = null;
            double extension = 10.0;
            bool normalized = false;

            if (!DA.GetData<Glulam>("Glulam A", ref gA) ||
            !DA.GetData<Glulam>("Glulam B", ref gB))
                return;

            Brep bi = Glulam.GetGlulamBisector(gA, gB, extension, normalized);

            DA.SetData("Brep", bi);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_Bisector_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("b12d0916-a49e-4c0b-96ef-dab85ff5033b"); }
        }
    }
}