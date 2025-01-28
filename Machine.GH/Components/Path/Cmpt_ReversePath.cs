#if !LITE
using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;

namespace tas.Machine.GH.Components
{
    public class Cmpt_ReversePath: GH_Component
    {
        public Cmpt_ReversePath()
          : base("Reverse Path", "RevP",
              "Reverses the direction of a path.",
              "tasMachine", UiNames.PathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_ReversePath;
        public override Guid ComponentGuid => new Guid("9F05A7C8-6A78-4382-A682-E1B1ABEB4174");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to reverse.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Reversed path.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object pathObject = null;


            DA.GetData("Path", ref pathObject);

            var inputPath = pathObject as GH_tasPath;
            if (inputPath == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get path.");
                return;
            }

            var path = new Path(inputPath.Value);

            path.Reverse();

            DA.SetData("Path", new GH_tasPath(path));
        }
    }
}
#endif