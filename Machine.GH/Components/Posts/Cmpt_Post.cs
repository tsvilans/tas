#if OBSOLETE

using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;

using Rhino.Geometry;

using tas.Machine.Posts;
using GH_IO.Serialization;

namespace tas.Machine.GH.Posts
{
    public class Cmpt_Post : GH_Component
    {

        public Cmpt_Post()
          : base("Post to machine", "Post",
              "Postprocess toolpaths to CNC machine.",
              "tasMachine", UiNames.ToolpathSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tas_icons_PostCMS_24x24;
        public override Guid ComponentGuid => new Guid("54ef7bba-a5c3-424e-8f3c-79ec62ddee07");
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        Guid LastValueList = Guid.Empty;
        IGH_Param PostParameter = null;

        string[] AvailablePosts = new string[] { "Haas", "CMS", "Shopbot", "Raptor", "CNC-STEP" };

        protected override void BeforeSolveInstance()
        {

            if (PostParameter.SourceCount > 0)
            {
                var last = PostParameter.Sources.Last();
                if (last.InstanceGuid != LastValueList && last is GH_ValueList)
                {
                    var valueList = last as GH_ValueList;
                    valueList.ListItems.Clear();

                    for (int i = 0; i < AvailablePosts.Length; ++i)
                    {
                        valueList.ListItems.Add(new GH_ValueListItem(AvailablePosts[i], $"\"{AvailablePosts[i]}\""));
                    }

                    valueList.SelectItem(0);
                    LastValueList = last.InstanceGuid;
                }

                PostParameter.CollectData();
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Machine", "M", "Machine to post to", GH_ParamAccess.item);
            pManager.AddGenericParameter(
                "Toolpaths", "TP", "Toolpaths as a list.", GH_ParamAccess.list);
            pManager.AddGenericParameter(
                "Safety", "S", "Safe zone for rapid movements. If it is a Plane, the tool will retract along its axis to the plane. " +
                "If it's a Mesh or Brep, the tool will retract along its axis until it hits the geometry.", GH_ParamAccess.item);
            pManager.AddPlaneParameter(
                "Frame", "F", "Optional workframe for all targets.", GH_ParamAccess.item);
            pManager.AddBooleanParameter(
                "All codes", "A", "Post all G-codes", GH_ParamAccess.item, false);

            PostParameter = pManager[0];
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Gcode", "NC", "Output NC code for CMS machine.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Axes", "A", "Axis values for each waypoint.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Speeds", "S", "Speeds for each waypoint.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Path", "P", "Output toolpath.", GH_ParamAccess.item);
            pManager.AddTextParameter("debug", "d", "debug info", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Toolpath> tpIn = new List<Toolpath>();
            object safety = null;
            string post_name = "";
            bool post_all = false;
            Plane frame = Plane.WorldXY;


            DA.GetData("Machine", ref post_name);
            this.Message = post_name;

            DA.GetDataList("Toolpaths", tpIn);
            DA.GetData("Safety", ref safety);
            DA.GetData("Frame", ref frame);
            DA.GetData("All codes", ref post_all);

            List<Toolpath> TP = tpIn.Select(x => x.Duplicate()).ToList();

            MachinePost post = null;
            switch (post_name)
            {
                case ("CMS"):
                    post = new CMSPost();
                    break;
                case ("Haas"):
                    post = new HaasPost();
                    break;
                case ("Shopbot"):
                    post = new ShopbotPost();
                    break;
                case ("Raptor"):
                    post = new RaptorBCNPost();
                    break;
                case ("CNC-STEP"):
                    post = new CncStepPost();
                    break;
                default:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Machine {post_name} not found.");
                    return;
            }


            // Program initialization
            var doc = OnPingDocument();
            if (doc != null)
            {
                post.Author = doc.Author.Name;
                post.Name = doc.DisplayName;
            }
            else
            {
                post.Author = "Author";
                post.Name = "Post";
            }

            post.StockModel = null;
            
            post.AlwaysWriteGCode = post_all;

            for (int i = 0; i < TP.Count; ++i)
            {
                var toolpath = TP[i];
                post.AddTool(toolpath.Tool);

                if (frame != Plane.WorldXY)
                {
                    toolpath.Transform(Transform.PlaneToPlane(Plane.WorldXY, frame));
                }

                post.AddPath(toolpath);
            }

            //cms.WorkOffset = new Point3d(0, 0, 0);

            // Post-process toolpaths

            var code = (post.Compute() as List<string>).Select(x => new GH_String(x));

            // ****** Fun stuff ends here. ******


            List<Point3d> points = new List<Point3d>();
            List<int> types = new List<int>();
            List<Vector3d> vectors = new List<Vector3d>();

            for (int i = 0; i < post.Paths.Count; ++i)
                for (int j = 0; j < post.Paths[i].Paths.Count; ++j)
                    for (int k = 0; k < post.Paths[i].Paths[j].Count; ++k)
                    {
                        points.Add(post.Paths[i].Paths[j][k].Plane.Origin);
                        vectors.Add(post.Paths[i].Paths[j][k].Plane.ZAxis);

                        if (post.Paths[i].Paths[j][k].IsRapid())
                            types.Add(0);
                        else if (post.Paths[i].Paths[j][k].IsFeed())
                            types.Add(1);
                        else if (post.Paths[i].Paths[j][k].IsPlunge())
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
            DA.SetDataList("debug", post.Errors);

            if (post.Axes != null)
            {
                var axes = new List<string>();
                foreach (var values in post.Axes)
                {
                    axes.Add($"{values.X:0.000}, {values.Y:0.000}, {values.Z:0.000}, {values.B:0.000}, {values.C:0.000}");
                }
                DA.SetDataList("Axes", axes);
                DA.SetDataList("Speeds", post.Axes.Select(x => x.Speed));
            }

        }

        public override bool Read(GH_IReader reader)
        {
            reader.TryGetGuid("ValueListGuid", ref LastValueList);
            return base.Read(reader);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetGuid("ValueListGuid", LastValueList);
            return base.Write(writer);
        }
    }
}
#endif