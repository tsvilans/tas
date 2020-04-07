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
            pManager.AddGenericParameter("Glulam A", "gA", "First Glulam.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Glulam B", "gB", "Second Glulam.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Bisector surface.", GH_ParamAccess.item);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object iGA = null, iGB = null;
            double extension = 10.0;
            bool normalized = false;

            DA.GetData("Glulam A", ref iGA);
            DA.GetData("Glulam B", ref iGB);

            Glulam gA = iGA as Glulam;
            if (gA == null) return;

            Glulam gB = iGB as Glulam;
            if (gB == null) return;

            Brep bi = Glulam.GetGlulamBisector(gA, gB, extension, normalized);

            DA.SetData("Brep", bi);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("b12d0916-a49e-4c0b-96ef-dab85ff5033b"); }
        }
    }
}