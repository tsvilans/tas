using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;


namespace tas.Machine.GH.Components
{
    public class Cmpt_JoinToolpaths : GH_Component
    {
        public Cmpt_JoinToolpaths()
          : base("Join Toolpaths", "Join",
              "Join two or more toolpaths into one. Tool and safety data will be taken from the first toolpath in the list.",
              "tasMachine", UiNames.ToolpathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_JoinToolpaths;
        public override Guid ComponentGuid => new Guid("8d6b0bfd-3528-49ab-84b0-1a9173c6e66d");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpaths", "T", "Toolpath.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath with leads and links.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Toolpath> tpIn = new List<Toolpath>();

            DA.GetDataList("Toolpaths", tpIn);

            if (tpIn.Count < 1) return;

            Toolpath tp = new Toolpath(tpIn[0]);

            for (int i = 1; i < tpIn.Count; ++i)
            {
                tp.Paths.AddRange(tpIn[i].Paths);
            }

            DA.SetData("Toolpath", new GH_Toolpath(tp));
        }
    }
}