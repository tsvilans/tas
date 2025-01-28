using System;
using System.Threading;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;
using System.Linq;

namespace tas.Machine.GH.Components
{
    public class Cmpt_RetractToolpath: GH_Component
    {
        public Cmpt_RetractToolpath()
          : base("Retract Toolpath", "Retract",
              "Add rapid targets on a defined retract plane at the beginning and end of a toolpath.",
              "tasMachine", UiNames.ToolpathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_RetractToolpath;
        public override Guid ComponentGuid => new Guid("0956D817-0851-4FBC-8989-1D01EDABF3BA");
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "TP", "Toolpath to retract.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane to retract to.", GH_ParamAccess.item, Plane.WorldXY);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "TP", "New toolpath with added retracts.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var safety = Plane.WorldYZ;
            object toolpathObject = null;


            DA.GetData("Toolpath", ref toolpathObject);
            DA.GetData("Plane", ref safety);

            var inputToolpath = toolpathObject as GH_Toolpath;
            if (inputToolpath == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to get toolpath ({toolpathObject} : {toolpathObject.GetType()}).");
                return;
            }

            if (!safety.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to get valid retract plane ({safety}).");
                return;
            }

            var toolpath = inputToolpath.Value.Duplicate();

            var first = toolpath.Paths.First().First();
            var last = toolpath.Paths.Last().Last();

            var firstPlane = first.Plane;
            var firstPoint = firstPlane.Origin.ProjectToPlane(safety);

            first.Plane = new Plane(firstPoint, firstPlane.XAxis, firstPlane.YAxis);

            var lastPlane = last.Plane;
            var lastPoint = lastPlane.Origin.ProjectToPlane(safety);

            last.Plane = new Plane(lastPoint, lastPlane.XAxis, lastPlane.YAxis);

            toolpath.Paths.First().Insert(0, first);
            toolpath.Paths.Last().Add(last);

            DA.SetData("Toolpath", new GH_Toolpath(toolpath));
        }
    }
}