using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Robots;
using tas.Core;
using tas.Core.GH;
using tas.Core.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace tas.Machine.GH.Extended
{
    public class ToolpathSettings
    {
        public double RapidRate;
        public double FeedRate;
        public double PlungeRate;
        public double RapidZ;
        public double SafeZ;
        public bool UseExternalAxis;

        public ToolpathSettings()
        {
            RapidRate = 100.0;
            FeedRate = 10.0;
            RapidZ = 10.0;
            SafeZ = 10.0;
            PlungeRate = FeedRate / 3;
            UseExternalAxis = false;
        }
    }

    public class Cmpt_ABBProcessor : GH_Component
    {
        ToolpathSettings settings = new ToolpathSettings();
        ABBPostComponent_Form form;

        private object Safety = null;

        public Cmpt_ABBProcessor()
          : base("tasToolpath: Robot Targets", "tasTP: RobTar",
              "Connect multiple toolpaths together, link, and output robot targets.",
              "tasTools", "Machining")
        {
        }

        public Zone ZoneRapid = new Zone(3.0, "ZoneRapid");
        public Zone ZoneCutting = new Zone(1.0, "ZoneCutting");
        public Zone ZonePrecise = new Zone(0.0, "ZonePrecise");


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "TP", "Toolpaths as a list of OrientedPolyline objects.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Tool", "T", "Robots Tool parameter.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Safety", "S", "Safe zone for rapid movements. If it is a Plane, the tool will retract along its axis to the plane. " +
                "If it's a Mesh or Brep, the tool will retract along its axis until it hits the geometry.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Frame", "F", "Optional workframe for all targets.", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "Targets", "Robot targets as list.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Path", "Path", "Output toolpath.", GH_ParamAccess.item);
            pManager.AddTextParameter("debug", "d", "debug info", GH_ParamAccess.item);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            menu.Items.Add("Settings...", null);
            menu.ItemClicked += SettingsClicked;
        }

        private void SettingsClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "Settings..." || e.ClickedItem.Text == "Settings...")
            {
                form = new ABBPostComponent_Form(this, settings);
                if (form != null)
                    form.Show();
                return;
            }
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetDouble("RapidZ", settings.RapidZ);
            writer.SetDouble("SafeZ", settings.SafeZ);
            writer.SetDouble("FeedRate", settings.FeedRate);
            writer.SetDouble("RapidRate", settings.RapidRate);
            writer.SetBoolean("UseExternalAxis", settings.UseExternalAxis);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            settings.RapidRate = reader.GetDouble("RapidRate");
            settings.RapidZ = reader.GetDouble("RapidZ");
            settings.SafeZ = reader.GetDouble("SafeZ");
            settings.FeedRate = reader.GetDouble("FeedRate");
            settings.UseExternalAxis = reader.GetBoolean("UseExternalAxis");

            return base.Read(reader);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> paths = new List<object>();
            if (!DA.GetDataList("Paths", paths)) return;

            // get Frame
            Frame frame = null;
            Plane frame_ref = new Plane();
            if (!DA.GetData("Frame", ref frame_ref)) frame = null;
            else frame = new Frame(frame_ref);

            // get Tool
            Tool tool = new Tool(Plane.WorldXY);
            object tool_ref = null;
            DA.GetData("Tool", ref tool_ref);
            if (tool_ref != null && tool_ref is Robots.Grasshopper.GH_Tool)
                tool = (tool_ref as Robots.Grasshopper.GH_Tool).Value;
            //tool = tool_ref as Tool;

            // polyline conversion to targets

            List<PPolyline> polys = new List<PPolyline>();
            foreach (object obj in paths)
            {
                if (obj is Core.GH.GH_PPolyline)
                    polys.Add((obj as GH_PPolyline).Value);
                else if (obj is GH_Curve)
                {
                    GH_Curve crv_ref = obj as GH_Curve;
                    if (crv_ref.Value.IsPolyline())
                    {
                        Polyline pl;
                        if (crv_ref.Value.TryGetPolyline(out pl)) polys.Add((PPolyline)pl);
                    }
                }
            }

            if (polys.Count < 1) return;

            string debug = "";
            debug += "SafeZ: " + settings.SafeZ.ToString("0.00") + "\n";
            debug += "RapidZ: " + settings.RapidZ.ToString("0.00") + "\n";
            debug += "FeedRate: " + settings.FeedRate.ToString("0.00") + "\n";
            debug += "RapidRate: " + settings.RapidRate.ToString("0.00") + "\n";

            if (!DA.GetData("Safety", ref Safety)) Safety = polys[0][0];
            if (!(Safety is Mesh) && (Safety is Brep))
            {
                Mesh m = new Mesh();

                Mesh[] converted = Mesh.CreateFromBrep(Safety as Brep, MeshingParameters.Smooth);
                foreach (Mesh cm in converted)
                {
                    m.Append(cm);
                }

                Safety = m;
            }

            debug += "Raw: " + Safety.ToString() + "\n";
            ParseSafety();
            debug += "Parsed: " + Safety.ToString() + "\n";

            List<Target> targets = new List<Target>();
            List<Plane> path_planes = new List<Plane>();
            Speed FeedSpeed = new Speed(settings.FeedRate, name: "cutting");
            Speed RapidSpeed = new Speed(settings.RapidRate, name: "rapid");
            Speed PlungeSpeed = new Speed(settings.PlungeRate, name: "plunge");

            Plane LastTarget = new Plane();
            bool last = false;
            
            foreach(PPolyline poly in polys)
            {

                if (poly.Count < 1) continue;

                //Plane temp = poly.Vertices[0];
                //temp.Origin = poly.Vertices[0].Origin + temp.ZAxis * settings.RapidZ;

                // first target retracted
                Plane temp = RetractToSafety(poly[0]);


                // add link if necessary
                if (last)
                {
                    List<Plane> links = LinkOnSafety2(LastTarget, temp, ref debug);
                    foreach (Plane p in links)
                    {
                        targets.Add(new CartesianTarget(p, null, Target.Motions.Linear, tool, RapidSpeed, ZoneRapid, null, frame, null));
                        path_planes.Add(p);
                    }
                }

                targets.Add(new CartesianTarget(temp, null, Target.Motions.Linear, tool, RapidSpeed, ZonePrecise, null, frame, null));
                path_planes.Add(temp);

                temp = poly[0];
                temp.Origin = poly[0].Origin + temp.ZAxis * settings.SafeZ;
                targets.Add(new CartesianTarget(temp, null, Target.Motions.Linear, tool, RapidSpeed, ZonePrecise, null, frame, null));
                path_planes.Add(temp);

                temp = poly[0];
                targets.Add(new CartesianTarget(poly[0], null, Target.Motions.Linear, tool, PlungeSpeed, ZonePrecise, null, frame, null));
                path_planes.Add(poly[0]);

                for (int i = 1; i < poly.Count; ++i)
                {
                    targets.Add(new CartesianTarget(poly[i], null, Target.Motions.Linear, tool, FeedSpeed, ZoneCutting, null, frame, null));
                    path_planes.Add(poly[i]);
                }

                temp = poly[poly.Count - 1];
                temp.Origin = poly[poly.Count - 1].Origin + temp.ZAxis * settings.SafeZ;
                targets.Add(new CartesianTarget(temp, null, Target.Motions.Linear, tool, FeedSpeed, ZonePrecise, null, frame, null));
                path_planes.Add(temp);

                // last target retracts
                temp = RetractToSafety(poly[poly.Count - 1]);

                LastTarget = temp;
                last = true;

                //temp.Origin = poly[poly.Vertices.Count - 1].Origin + temp.ZAxis * settings.RapidZ;
                targets.Add(new CartesianTarget(temp, null, Target.Motions.Linear, tool, RapidSpeed, ZonePrecise, null, frame, null));
                path_planes.Add(temp);
            }

            if (settings.UseExternalAxis)
            {
                for (int i = 0; i < targets.Count; ++i)
                {
                    targets[i].External = new double[] { 0.0, 0.0 };
                }
            }
            
            DA.SetData("debug", debug);
            DA.SetDataList("Targets", targets);
            DA.SetData("Path", new GH_PPolyline(new PPolyline(path_planes)));
        }

        private Plane RetractToSafety(Plane current)
        {
            Ray3d ray = new Ray3d(current.Origin, current.ZAxis);

            if (Safety is Plane)
            {
                double t;
                Line line = new Line(current.Origin, current.ZAxis);
                if (Rhino.Geometry.Intersect.Intersection.LinePlane(line, (Plane)Safety, out t))
                { 
                    Plane p = new Plane(current);
                    p.Origin = line.PointAt(t);
                    return p;
                }
            }
            else if (Safety is Mesh)
            {
                double d = Rhino.Geometry.Intersect.Intersection.MeshRay(Safety as Mesh, ray);
                Plane p = new Plane(current);
                p.Origin = current.Origin + current.ZAxis * d;
                return p;
            }
            else if (Safety is GeometryBase)
            {
                Point3d[] pts = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, new GeometryBase[] { Safety as GeometryBase }, 1);
                if (pts.Length > 0)
                {
                    Plane p = new Plane(current);
                    p.Origin = pts[0];
                    return p;
                }
            }

            current.Origin += current.ZAxis * settings.RapidZ;
            return current;
        }

        private void ParseSafety()
        {
            object temp;
            if (Safety is GH_Plane) temp = (Safety as GH_Plane).Value;
            else if (Safety is GH_Mesh) temp = (Safety as GH_Mesh).Value;
            else if (Safety is GH_Brep) temp = (Safety as GH_Brep).Value;
            else if (Safety is GH_Surface) temp = (Safety as GH_Surface).Value;
            else return;
            Safety = temp;
        }

        private List<Plane> LinkOnSafety(Plane A, Plane B, ref string debug)
        {
            int N = 20;
            double travel_dist = 10.0;
            List<Plane> link_targets = new List<Plane>();
            Point3d nO;
            if (Safety is Mesh)
            {
                Mesh m = Safety as Mesh;
                Plane Last = A;

                for (int i = 1; i < N; ++i)
                {
                    double t = 1.0 / (N - i - 1.0);
                    //double t = (double)i / (N - i - 1);
                    debug += string.Format("Link {0}: {1:0.00}\n", i, t);
                    Plane p = Util.Interpolation.InterpolatePlanes2(Last, B, t);

                    nO = m.ClosestPoint(p.Origin);

                    Vector3d v = new Vector3d(nO - Last.Origin);
                    if (v.Length < travel_dist)
                    {
                        double d = travel_dist - v.Length;
                        v.Unitize();
                        nO = m.ClosestPoint(nO + v * travel_dist);
                    }

                    p.Origin = nO;
                    Last = p;
                    link_targets.Add(p);
                    //link_targets.Add(new Plane(nO, p.XAxis, p.YAxis));
                }
            }
            else if (Safety is Plane)
            {
                //Plane p = A;
                //nO = Util.ProjectToPlane(p.Origin, (Plane)Safety);
                //p.Origin = nO;
                //link_targets.Add(p);
            }
            return link_targets;
        }

        private List<Plane> LinkOnSafety2(Plane A, Plane B, ref string debug)
        {
            double SkipDistance = 20.0;
            double minD = 10.0;
            double step = 10.0;
            int counter = 0;
            int maxIter = 1000;

            if (A.Origin.DistanceTo(B.Origin) < SkipDistance)
                return new List<Plane>();

            List<Plane> link_targets = new List<Plane>();
            List<Point3d> link_points = new List<Point3d>();
            List<Vector3d> normals = new List<Vector3d>();

            if (Safety is Mesh)
            {
                Mesh m = Safety as Mesh;
                Point3d point = A.Origin;
                MeshPoint mp = m.ClosestMeshPoint(point, step * 0.99);
                while (point.DistanceTo(B.Origin) > minD && counter < maxIter)
                {
                    counter++;
                    Vector3d toEnd = new Vector3d(B.Origin - mp.Point);
                    Vector3d n = m.NormalAt(mp);
                    Vector3d v = Util.ProjectToPlane(toEnd, new Plane(mp.Point, n));
                    v.Unitize();
                    point = mp.Point + v * step;
                    mp = m.ClosestMeshPoint(point, step / 2);
                    link_points.Add(mp.Point);
                    normals.Add(n);
                }

                Polyline poly = new Polyline(link_points);
                double length = 0.0;
                double total_length = poly.Length;

                for (int i = 0; i < poly.Count - 1; ++i)
                {
                    Plane p = Util.Interpolation.InterpolatePlanes2(A, B, length / total_length);
                    Plane pnorm = new Plane(p);
                    pnorm.Transform(Transform.Rotation(p.ZAxis, normals[i], p.Origin));

                    double t = Math.Sin(length / total_length * Math.PI);

                    p = Util.Interpolation.InterpolatePlanes2(p, pnorm, t);


                    p.Origin = poly[i];
                    length += poly[i].DistanceTo(poly[i + 1]);
                    link_targets.Add(p);
                }

            }
            return link_targets;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //return Properties.Resources.tasTools_icons_RobotTargets_24x24;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{d9e4a5b0-7bbe-49e2-97b4-f97be5d138b4}"); }
        }
    }
}
