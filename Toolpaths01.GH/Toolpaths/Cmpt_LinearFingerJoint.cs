#if EXTRA
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using tas.Core.Types;
using tas.Core.GH;


namespace tas.Machine.GH.Components
{
    public class Cmpt_LinearFingerJoint : ToolpathBase_Component
    {
        public Cmpt_LinearFingerJoint()
          : base("Special - Finger Joint Linear", "LinFJ",
              "Create linear finger joint from beam member geometry.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddBrepParameter("Brep", "B", "Beam member geometry to clip with.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Male", "M", "Toggle between male and female type of joint.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip", "F", "Flip clipped part of beam geometry.", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Angle", "A", "Angle of v-cutter.", GH_ParamAccess.item, 30.0);
            pManager.AddNumberParameter("Depth", "D", "Depth of grooves.", GH_ParamAccess.item, 6.0);

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Finger Joint", "FJ", "Resultant finger jointed member.", GH_ParamAccess.item);
            base.RegisterOutputParams(pManager);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            Brep brep = null;
            DA.GetData("Brep", ref brep);

            bool male = false;
            DA.GetData("Male", ref male);

            bool flip = false;
            DA.GetData("Flip", ref flip);

            double angle = 0;
            DA.GetData("Angle", ref angle);

            double depth = 0;
            DA.GetData("Depth", ref depth);
            double hdepth = depth / 2;

            double hangle = angle / 360 * Math.PI;
            Curve[] intersections;
            Point3d[] intersection_points;
            Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, Workplane, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out intersections, out intersection_points);
            if (intersections.Length < 1) return;

            Curve[] outline_list = Curve.JoinCurves(intersections);
            if (outline_list.Length < 1) return;
            Curve outline = outline_list[0];

            List<Point3d> toolpath_start_points = new List<Point3d>();
            BoundingBox bb = intersections[0].GetBoundingBox(Workplane);

            double max_x = 0;
            double max_y = 0;
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    for (int k = 0; k < 2; ++k)
                    {
                        max_x = Math.Max(Math.Abs(bb.Corner((i == 1), (j == 1), (k == 1)).X), max_x);
                        max_y = Math.Max(Math.Abs(bb.Corner((i == 1), (j == 1), (k == 1)).Y), max_y);
                    }
                }
            }

            List<Point3d> pts = new List<Point3d>();
            double step = Math.Tan(hangle) * depth;
            Curve[] offsets = outline.Offset(Workplane, step * 2, 0.1, CurveOffsetCornerStyle.Sharp);
            outline = offsets[0];

            int num_grooves = (int)(max_x / step) + 5;

            if (male)
                depth *= -1;

            for (int i = -num_grooves; i <= num_grooves; ++i)
            {
                pts.Add(new Point3d(step * i, -max_y * 2, hdepth * ((Math.Abs(i) % 2) * 2 - 1)));
            }

#region Toolpath
            int flipper = flip ? 1 : 0;

            for (int i = flipper; i < pts.Count; i += 2)
            {
                toolpath_start_points.Add(new Point3d(pts[i].X, pts[i].Y, 0.0));
            }

            flipper = flip ? 1 : -1;

            List<GH_PPolyline> paths = new List<GH_PPolyline>();

            List<Point3d> path_points = new List<Point3d>();
            List<Tuple<Point3d, Point3d>> path_pairs = new List<Tuple<Point3d, Point3d>>();

            for (int i = 0; i < toolpath_start_points.Count; ++i)
            {
                Point3d p1 = toolpath_start_points[i];
                Point3d p2 = toolpath_start_points[i] + Vector3d.YAxis * max_y * 4;
                Polyline temp = new Polyline(new Point3d[] { p1, p2 });
                temp.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));
                Rhino.Geometry.Intersect.CurveIntersections ci = Rhino.Geometry.Intersect.Intersection.CurveCurve(outline, temp.ToNurbsCurve(), 0.1, 0.01);
                if (ci.Count == 2)
                {
                    path_pairs.Add(new Tuple<Point3d, Point3d>(ci[0].PointA + Workplane.ZAxis * hdepth * flipper, ci[1].PointB + Workplane.ZAxis * hdepth * flipper));
                }

            }

            path_points.Add(path_pairs[0].Item1);
            path_points.Add(path_pairs[0].Item2);

            double d1, d2;
            int index = 1;
            for (int i = 1; i < path_pairs.Count; ++i)
            {
                d1 = path_pairs[i].Item1.DistanceTo(path_points[index]);
                d2 = path_pairs[i].Item2.DistanceTo(path_points[index]);
                if (d1 > d2)
                {
                    path_points.Add(path_pairs[i].Item2);
                    path_points.Add(path_pairs[i].Item1);
                }
                else
                {
                    path_points.Add(path_pairs[i].Item1);
                    path_points.Add(path_pairs[i].Item2);
                }
                index += 2;
            }

            paths.Add(new GH_PPolyline(new PPolyline(path_points, Workplane)));

#endregion


            Polyline poly = new Polyline(pts);

            Brep grooves = Extrusion.CreateExtrusion(poly.ToNurbsCurve(), Vector3d.YAxis * max_y * 4).ToBrep();
            grooves.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));

            if (!flip)
            {
                List<Brep> srfs = new List<Brep>();
                for (int i = 0; i < grooves.Surfaces.Count; ++i)
                {
                    srfs.Add(Brep.CreateFromSurface(grooves.Surfaces[i].Reverse(0)));
                }
                grooves = Brep.MergeBreps(srfs, 0.1);
            }

            Brep[] trims = brep.Trim(grooves, 0.1);
            if (trims.Length < 1) throw new Exception("Trim failed!");

            Brep final = trims[0];
            for (int i = 1; i < trims.Length; ++i)
                final.Join(trims[i], 0.1, true);

            trims = grooves.Trim(brep, 0.1);
            for (int i = 0; i < trims.Length; ++i)
                final.Join(trims[i], 0.1, true);

            DA.SetData("Finger Joint", final);
            DA.SetDataList("Paths", paths);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{f65aa7e1-e82a-45e5-9ae9-471d8a753562}"); }
        }
    }
}
#endif