using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;

namespace tas.Machine.GH.Components
{
    public class Cmpt_AddRamp: GH_Component
    {
        public Cmpt_AddRamp()
          : base("Add Ramp", "Ramp",
              "Add a ramp to the beginning of a path.",
              "tasMachine", UiNames.PathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_CreateRamp;
        public override Guid ComponentGuid => new Guid("111DD903-7013-4DE9-96E8-B1E4B4D3C058");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to orient.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height of ramp.", GH_ParamAccess.item, 20);
            pManager.AddNumberParameter("Length", "L", "Planar length of ramp.", GH_ParamAccess.item, 200);
            pManager.AddNumberParameter("MinLength", "M", "Minimum planar length of ramp for very short paths.", GH_ParamAccess.item, 0);

            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path with ramp.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double height = 20, length = 200, minLength = 0;
            object pathObject = null;

            DA.GetData("Path", ref pathObject);
            DA.GetData("Height", ref height);
            DA.GetData("Length", ref length);
            DA.GetData("MinLength", ref minLength);

            var inputPath = pathObject as GH_tasPath;
            if (inputPath == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get path.");
                return;
            }

            if (length < 1e-5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Ramp of zero length specified.");
                DA.SetData("Path", inputPath);
            }

            var path = inputPath.Value;

            length = Math.Max(minLength, Math.Min(length, path.GetLength()));
            var ramp = Path.CreateRamp(path, path[0], height, length);
            
            ramp.RemoveAt(ramp.Count - 1);

            var newPath = new Path();
            newPath.AddRange(ramp);
            newPath.AddRange(path);


            DA.SetData("Path", new GH_tasPath(newPath));
        }
    }
}