#if EXTRA
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH.Toolpaths
{
    public class CircularFingerJoint_Component : ToolpathBase_Component
    {
        public CircularFingerJoint_Component()
          : base("Special - Finger Joint Circular", "CircFJ",
              "Create radial finger joint from beam member geometry.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddBrepParameter("Brep", "B", "Beam member geometry to clip with.", GH_ParamAccess.item);
            //pManager.AddCurveParameter("Cross section", "X", "Cross section of beam member at plane.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Male", "M", "Toggle between male and female type of joint.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Flip", "F", "Flip clipped part of beam geometry.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Finger Joint", "FJ", "Resultant finger jointed member.", GH_ParamAccess.item);
            base.RegisterOutputParams(pManager);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep brep = null;
            DA.GetData("Brep", ref brep);

            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            bool male = false;
            DA.GetData("Male", ref male);

            bool flip = false;
            DA.GetData("Flip", ref flip);

            double angle = 60;
            double hangle = angle / 360 * Math.PI;
            double depth = 3;

            Curve[] intersections;
            Point3d[] intersection_points;
            Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, Workplane, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out intersections, out intersection_points);
            if (intersections.Length < 1) return;

            BoundingBox bb = intersections[0].GetBoundingBox(Workplane);

            double r = double.MaxValue;
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    for (int k = 0; k < 2; ++k)
                    {
                        r = Math.Min(bb.Corner((i == 1), (j == 1), (k == 1)).DistanceTo(Point3d.Origin), r);
                    }
                }
            }

            List<Point3d> pts = new List<Point3d>();
            double step = Math.Tan(hangle) * depth;
            int num_grooves = (int)(r / step) + 5;

            if (male)
                depth *= -1;

            for (int i = 0; i <= num_grooves; ++i)
            {
                pts.Add(new Point3d(step * i, 0.0, depth * ((Math.Abs(i) % 2) * 2 - 1)));
            }

            Polyline poly = new Polyline(pts);
            Rhino.Geometry.RevSurface rev = RevSurface.Create(poly, new Line(Point3d.Origin, Vector3d.ZAxis));
            rev.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));

            Brep grooves = rev.ToBrep();


            if (flip)
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
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //return Properties.Resources.icon_radial_finger_joint_component_24x24;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{46cf325b-b5fa-4cb6-ae8b-173c38301ca3}"); }
        }
    }
}

#endif