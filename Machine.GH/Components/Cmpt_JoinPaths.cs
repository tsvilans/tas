using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH
{
    public class Cmpt_JoinPaths : GH_Component
    {
        public Cmpt_JoinPaths()
          : base("Join Paths", "Path",
              "Join orientated paths together.",
              "tasMachine", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "P", "Oriented paths to join.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> inputPaths = new List<object>();


            DA.GetDataList(0, inputPaths);

            Path path = new Path();

            foreach (var ipath in inputPaths)
            {
                if (ipath is Path)
                {
                    path.AddRange(ipath as Path);
                }
                else if (ipath is GH_tasPath)
                {
                    path.AddRange((ipath as GH_tasPath).Value);
                }
            }

            DA.SetData(0, new GH_tasPath(path));
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CreateToolpath_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("65e38a47-8b71-4c15-bf1d-89935c870e73"); }
        }
    }
}