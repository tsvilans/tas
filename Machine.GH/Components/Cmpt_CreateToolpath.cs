using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH
{
    public class Cmpt_CreateToolpath : GH_Component
    {
        public Cmpt_CreateToolpath()
          : base("Create Toolpath", "Toolpath",
              "Create a machining toolpath with tool data.",
              "tasMachine", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of toolpath.", GH_ParamAccess.item, "Toolpath");
            pManager.AddGenericParameter("Toolpaths", "T", "Toolpaths as Paths (from machine strategy components).", GH_ParamAccess.list);
            pManager.AddGenericParameter("Machine Tool", "MT", "Machine tool from library.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Safety", "S", "Safety plane for rapid movements.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("RapidZ", "Rz", "Z-height for rapids.", GH_ParamAccess.item, 20.0);
            pManager.AddNumberParameter("SafeZ", "Sz", "Z-height for safe movements.", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("VertRetract", "VRet", "Retract vertically (true) or along tool (false).", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("FlipWrist", "FW", "Optionally specify to flip the wrist of a multi-axis machine.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath with tool and machining data.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> iToolpaths = new List<object>();
            object iTool = null;
            Plane iSafe = Plane.WorldXY;
            double iRapidZ = 20, iSafeZ = 10;
            bool iVR = true;
            string name = "Toolpath";
            bool flipWrist = false;

            DA.GetData("Name", ref name);
            DA.GetDataList("Toolpaths", iToolpaths);
            DA.GetData("Machine Tool", ref iTool);
            DA.GetData("Safety", ref iSafe);
            DA.GetData("RapidZ", ref iRapidZ);
            DA.GetData("SafeZ", ref iSafeZ);
            DA.GetData("VertRetract", ref iVR);
            DA.GetData("FlipWrist", ref flipWrist);

            this.Message = flipWrist.ToString();

            Toolpath tp = new Toolpath();
            tp.Name = name;
            tp.FlipWrist = flipWrist;

            // Cast tool
            MachineTool mt;
            if (iTool is GH_MachineTool)
                mt = (iTool as GH_MachineTool).Value;
            else
                mt = iTool as MachineTool;
            if (mt == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Machine tool could not be cast.");
                return;
            }

            tp.Tool = mt;

            for (int i = 0; i < iToolpaths.Count; ++i)
            {
                Path poly;
                if (iToolpaths[i] is Path)
                    poly = iToolpaths[i] as Path;
                else if (iToolpaths[i] is GH_tasPath)
                    poly = (iToolpaths[i] as GH_tasPath).Value;
                else
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error in Path wrangling.");
                    continue;
                }

                tp.Paths.Add(poly.Select(x => new Waypoint(x, (int)WaypointType.FEED)).ToList());
            }

            tp.PlaneRetractVertical = iVR;
            tp.RapidZ = iRapidZ;
            tp.SafeZ = iSafeZ;
            tp.Safety = iSafe;

            DA.SetData("Toolpath", new GH_Toolpath(tp));

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CreateToolpath_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0f44ae1d-5506-4655-92cb-72ee7dbf7c5b"); }
        }
    }
}