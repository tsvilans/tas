using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;

namespace tas.Machine.GH.Components
{
    public class Cmpt_OrientPath: GH_Component
    {
        public Cmpt_OrientPath()
          : base("Orient Path", "OrientP",
              "Orient path target Y-axes towards a vector, maintaining normal direction.",
              "tasMachine", UiNames.PathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tas_icons_OrientTargets_24x24;

        public override Guid ComponentGuid => new Guid("2F9A10B5-857A-45D5-AEFE-4DF59AE49ED2");

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to orient.", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector", "V", "Vector to orient towards.", GH_ParamAccess.item, -Vector3d.ZAxis);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Oriented path.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var vector = -Vector3d.ZAxis;
            object pathObject = null;


            DA.GetData("Path", ref pathObject);
            DA.GetData("Vector", ref vector);

            var inputPath = pathObject as GH_tasPath;
            if (inputPath == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get path.");
                return;
            }

            if (!vector.IsValid || vector.IsZero)
                return;

            var path = new Path(inputPath.Value);

            for (int i = 0; i < path.Count; ++i)
            {
                var plane = path[i];

                var xaxis = Vector3d.CrossProduct(vector, plane.ZAxis);
                var yaxis = Vector3d.CrossProduct(plane.ZAxis, xaxis);

                path[i] = new Plane(plane.Origin, xaxis, yaxis);
            }

            DA.SetData("Path", new GH_tasPath(path));
        }
    }
}