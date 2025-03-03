using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH.Components
{
    public class Cmpt_CreateToolpath : GH_Component
    {
        public Cmpt_CreateToolpath()
          : base("Create Toolpath", "Toolpath",
              "Create a machining toolpath with tool data.",
              "tasMachine", UiNames.ToolpathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_CreateToolpath;
        public override Guid ComponentGuid => new Guid("0f44ae1d-5506-4655-92cb-72ee7dbf7c5b");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        Line[][] VisualizationLines = new Line[0][];
        WaypointType[][] VisualizationTypes = new WaypointType[0][];

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of toolpath.", GH_ParamAccess.item, "Toolpath");
            pManager.AddGenericParameter("Toolpaths", "T", "Toolpaths as Paths (from machine strategy components).", GH_ParamAccess.list);
            pManager.AddGenericParameter("Machine Tool", "MT", "Machine tool from library.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Safety", "S", "Safety plane for rapid movements.", GH_ParamAccess.item);
            pManager.AddNumberParameter("RapidZ", "Rz", "Z-height for rapids.", GH_ParamAccess.item, 20.0);
            pManager.AddNumberParameter("SafeZ", "Sz", "Z-height for safe movements.", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("VertRetract", "VRet", "Retract vertically (true) or along tool (false).", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("FlipWrist", "FW", "Optionally specify to flip the wrist of a multi-axis machine.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Link", "L", "Create toolpath links.", GH_ParamAccess.item, false);

            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath with tool and machining data.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Duration", "D", "Duration of toolpath in seconds.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> ToolpathObjects = new List<object>();
            object ToolObject = null;
            Plane? SafetyPlane = null;
            double RapidZ = 20, iSafeZ = 10;
            bool VerticalRetract = true;
            string Name = "Toolpath";
            bool FlipWrist = false;
            bool CreateLeadsAndLinks = false;

            DA.GetData("Name", ref Name);
            DA.GetDataList("Toolpaths", ToolpathObjects);
            DA.GetData("Machine Tool", ref ToolObject);
            DA.GetData("Safety", ref SafetyPlane);
            DA.GetData("RapidZ", ref RapidZ);
            DA.GetData("SafeZ", ref iSafeZ);
            DA.GetData("VertRetract", ref VerticalRetract);
            DA.GetData("FlipWrist", ref FlipWrist);
            DA.GetData("Link", ref CreateLeadsAndLinks);

            //this.Message = flipWrist.ToString();

            Toolpath tp = new Toolpath();
            tp.Name = Name;
            tp.FlipWrist = FlipWrist;

            // Cast tool
            MachineTool mt;
            if (ToolObject is GH_MachineTool)
                mt = (ToolObject as GH_MachineTool).Value;
            else
                mt = ToolObject as MachineTool;
            if (mt == null)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Machine tool could not be cast.");
                return;
            }

            tp.Tool = mt;

            for (int i = 0; i < ToolpathObjects.Count; ++i)
            {
                Path poly;
                if (ToolpathObjects[i] is Path)
                    poly = ToolpathObjects[i] as Path;
                else if (ToolpathObjects[i] is GH_tasPath)
                    poly = (ToolpathObjects[i] as GH_tasPath).Value;
                else
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error in Path wrangling.");
                    continue;
                }

                tp.Paths.Add(poly.Select(x => new Waypoint(x, (int)WaypointType.FEED)).ToList());
            }

            tp.PlaneRetractVertical = VerticalRetract;
            tp.RapidZ = RapidZ;
            tp.SafeZ = iSafeZ;
            tp.Safety = SafetyPlane;

            if (CreateLeadsAndLinks)
            {
                tp.CreateLeadsAndLinks(tp.LinkPlane);

                var joined = new List<Waypoint>();
                foreach (var path in tp.Paths)
                {
                    joined.AddRange(path);
                }

                tp.Paths = new List<List<Waypoint>> { joined };
            }

            DA.SetData("Toolpath", new GH_Toolpath(tp));
            DA.SetData("Duration", tp.GetTotalTime());

            // Create visualization data

            VisualizationLines = new Line[tp.Paths.Count][];
            VisualizationTypes = new WaypointType[tp.Paths.Count][];

            for (int i = 0; i < tp.Paths.Count; ++i)
            {
                var visualisationPath = new Polyline();
                VisualizationTypes[i] = new WaypointType[tp.Paths[i].Count];

                for (int j = 0; j < tp.Paths[i].Count; ++j)
                {
                    visualisationPath.Add(tp.Paths[i][j].Plane.Origin);

                    if (tp.Paths[i][j].IsRapid())
                        VisualizationTypes[i][j] = WaypointType.RAPID;
                    else if (tp.Paths[i][j].IsFeed())
                        VisualizationTypes[i][j] = WaypointType.FEED;
                    else if (tp.Paths[i][j].IsPlunge())
                        VisualizationTypes[i][j] = WaypointType.PLUNGE;
                    else
                        VisualizationTypes[i][j] = WaypointType.UNKNOWN;
                }

                VisualizationLines[i] = visualisationPath.GetSegments();
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            for (int i = 0; i < VisualizationLines.Length; ++i)
            {
                for (int j = 0; j < VisualizationLines[i].Length; ++j)
                {
                    switch (VisualizationTypes[i][j + 1])
                    {
                        case (WaypointType.RAPID):
                            args.Display.DrawLine(VisualizationLines[i][j], System.Drawing.Color.Red);
                            break;
                        case (WaypointType.FEED):
                            args.Display.DrawLine(VisualizationLines[i][j], System.Drawing.Color.LightBlue);
                            break;
                        case (WaypointType.PLUNGE):
                            args.Display.DrawLine(VisualizationLines[i][j], System.Drawing.Color.LimeGreen);
                            break;
                        case (WaypointType.UNKNOWN):
                            args.Display.DrawLine(VisualizationLines[i][j], System.Drawing.Color.Purple);
                            break;
                        default:
                            break;
                    }
                }
            }

        }
    }
}