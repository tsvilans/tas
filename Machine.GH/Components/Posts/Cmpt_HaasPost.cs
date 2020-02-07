using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core;
using tas.Core.Types;
using tas.Machine.Posts;

namespace tas.Machine.GH.Posts
{
    public class Cmpt_HaasPost : GH_Component
    {

        public Cmpt_HaasPost()
          : base("Post to Haas", "Post2Haas",
              "Post toolpaths to Haas 3-axis machining centre.",
              "tasTools", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Toolpaths", "TP", "Toolpaths as a list.", GH_ParamAccess.list);
            pManager.AddGenericParameter(
                "Safety", "S", "Safe zone for rapid movements. If it is a Plane, the tool will retract along its axis to the plane. " +
                "If it's a Mesh or Brep, the tool will retract along its axis until it hits the geometry.", GH_ParamAccess.item);
            pManager.AddPlaneParameter(
                "Frame", "F", "Optional workframe for all targets.", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Gcode", "NC", "Output NC code for CMS machine.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Targets", "Targets", "Robot targets as list.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Path", "Path", "Output toolpath.", GH_ParamAccess.item);
            pManager.AddTextParameter("debug", "d", "debug info", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Toolpath> tpIn = new List<Toolpath>();
            object safety = null;

            DA.GetDataList("Toolpaths", tpIn);
            DA.GetData("Safety", ref safety);

            List<Toolpath> TP = tpIn.Select(x => x.Duplicate()).ToList();

            /*
            // Create and initialize toolpath
            Toolpath T = new Toolpath();
            T.Safety = safety;
            T.PlaneRetractVertical = false;
            T.SafeZ = 10.0;
            T.Paths = new List<List<Waypoint>>();

            bool zigzag = false;
            bool zz = false;
            bool rev = false;

            // Add individual paths to toolpath, zigzag or reverse if necessary
            for (int i = 0; i < TP.Count; ++i)
            {
                List<Waypoint> waypoints = new List<Waypoint>();
                waypoints.AddRange(TP[i].Select(x => new Waypoint(x, (int)WaypointType.FEED)));

                if (zigzag && zz)
                    waypoints.Reverse();
                else if (!zz && rev)
                    waypoints.Reverse();

                T.Paths.Add(waypoints);

                zigzag = !zigzag;
            }
            */

            //T.CreateRamps(20, 50);

            // Program initialization

            HaasPost haas = new HaasPost();
            haas.Author = "Tom Svilans";
            haas.Name = "TestPost";
            haas.StockModel = null;

            for (int i = 0; i < TP.Count; ++i)
            {
                haas.AddTool(TP[i].Tool);
                //TP[i].CreateLeadsAndLinks();
                haas.AddPath(TP[i]);
            }

            //haas.WorkOffset = new Point3d(0, 0, 0);

            // Post-process toolpaths

            var code = (haas.Compute() as List<string>).Select(x => new GH_String(x));

            // ****** Fun stuff ends here. ******


            List<Point3d> points = new List<Point3d>();
            List<int> types = new List<int>();
            List<Vector3d> vectors = new List<Vector3d>();

            for (int i = 0; i < haas.Paths.Count; ++i)
                for (int j = 0; j < haas.Paths[i].Paths.Count; ++j)
                    for (int k = 0; k < haas.Paths[i].Paths[j].Count; ++k)
                    {
                        points.Add(haas.Paths[i].Paths[j][k].Plane.Origin);
                        vectors.Add(haas.Paths[i].Paths[j][k].Plane.ZAxis);

                        if (haas.Paths[i].Paths[j][k].IsRapid())
                            types.Add(0);
                        else if (haas.Paths[i].Paths[j][k].IsFeed())
                            types.Add(1);
                        else if (haas.Paths[i].Paths[j][k].IsPlunge())
                            types.Add(2);
                        else
                            types.Add(-1);
                    }

            Polyline poly = new Polyline(points);
            List<Line> lines = new List<Line>();

            for (int i = 1; i < poly.Count; ++i)
                lines.Add(new Line(poly[i - 1], poly[i]));

            types.RemoveAt(0);

            DA.SetDataList("Gcode", code);
            DA.SetDataList("Path", lines);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_PostHaas_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("a39bfd74-ad49-454e-8453-32a359f46c4e"); }
        }
    }
}