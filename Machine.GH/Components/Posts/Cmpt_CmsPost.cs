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
    public class Cmpt_CmsPost : GH_Component
    {

        public Cmpt_CmsPost()
          : base("Post to CMS", "Post2CMS",
              "Post toolpaths to CMS 5-axis machining centre.",
              "tasMachine", "Machining")
        {
        }

        ToolpathSettings settings = new ToolpathSettings();

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

            // Program initialization
            CMSPost cms = new CMSPost();
            cms.Author = "Tom Svilans";
            cms.Name = "TestPost";
            cms.StockModel = null;

            for (int i = 0; i < TP.Count; ++i)
            {
                cms.AddTool(TP[i].Tool);
                cms.AddPath(TP[i]);
            }

            //cms.WorkOffset = new Point3d(0, 0, 0);

            // Post-process toolpaths

            var code = (cms.Compute() as List<string>).Select(x => new GH_String(x));

            // ****** Fun stuff ends here. ******


            List<Point3d> points = new List<Point3d>();
            List<int> types = new List<int>();
            List<Vector3d> vectors = new List<Vector3d>();

            for (int i = 0; i < cms.Paths.Count; ++i)
                for (int j = 0; j < cms.Paths[i].Paths.Count; ++j)
                    for (int k = 0; k < cms.Paths[i].Paths[j].Count; ++k)
                    {
                        points.Add(cms.Paths[i].Paths[j][k].Plane.Origin);
                        vectors.Add(cms.Paths[i].Paths[j][k].Plane.ZAxis);

                        if (cms.Paths[i].Paths[j][k].IsRapid())
                            types.Add(0);
                        else if (cms.Paths[i].Paths[j][k].IsFeed())
                            types.Add(1);
                        else if (cms.Paths[i].Paths[j][k].IsPlunge())
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
                return Properties.Resources.tas_icons_PostCMS_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("68dc4a1b-a8a7-403b-8cab-8869e8d468c6"); }
        }
    }
}