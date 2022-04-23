using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

namespace tas.Machine
{
    public abstract class Feature
    {
        public string Name;
        public abstract Brep ToBrep();
        public abstract Curve ToNurbsCurve();
        public abstract Feature Duplicate();
        public abstract void Transform(Transform xform);
    }

    public class Drilling : Feature
    {
        public Line Path;
        public double Diameter;
        public double Radius { get { return Diameter * 0.5; } set { Diameter = value * 2; } }

        public Drilling(string name, Line line, double diameter = 10.0)
        {
            Name = name;
            Path = line;
            Diameter = diameter;
        }

        public override Brep ToBrep()
        {
            return new Cylinder(
                new Circle(
                    new Plane(Path.From, Path.Direction), Diameter / 2), 
                Path.Length).ToBrep(true, true);
        }

        public override Curve ToNurbsCurve()
        {
            return Path.ToNurbsCurve();
        }

        public override Feature Duplicate()
        {
            return new Drilling(Name, Path, Diameter);
        }

        public override void Transform(Transform xform)
        {
            Path.Transform(xform);
        }

        public override string ToString()
        {
            return $"Drilling ({Name} {Diameter:0.0} x {Path.Length:0.0})";
        }
    }

    public class Surfacing : Feature
    {
        public Brep Surface;

        public Surfacing(string name, Brep surface)
        {
            Name = name;
            Surface = surface;
        }
        public override Brep ToBrep()
        {
            return Surface;
        }

        public override Curve ToNurbsCurve()
        {
            return Surface.DuplicateNakedEdgeCurves(true, false)[0];
        }

        public override Feature Duplicate()
        {
            return new Surfacing(Name, Surface.DuplicateBrep());
        }

        public override void Transform(Transform xform)
        {
            Surface.Transform(xform);
        }
        public override string ToString()
        {
            return $"Surfacing ({Name})";
        }
    }
    public class Tracing : Feature
    {
        public Curve Path;
        /// <summary>
        /// Side of curve to offset tool on (- = left; 0 = centred; + = right).
        /// </summary>
        int Offset;

        public Tracing(string name, Curve path, int offset = 0)
        {
            Name = name;
            Path = path;
            Offset = Math.Sign(offset);
        }
        public override Brep ToBrep()
        {
            return null;
        }

        public override Curve ToNurbsCurve()
        {
            return Path.DuplicateCurve();
        }

        public override Feature Duplicate()
        {
            return new Tracing(Name, Path.DuplicateCurve());
        }

        public override void Transform(Transform xform)
        {
            Path.Transform(xform);
        }
        public override string ToString()
        {
            return $"Tracing ({Name})";
        }
    }

    public class Pocket : Feature
    {
        public Curve Outline;
        public double Depth;
        public Vector3d ToolAxis;

        public Pocket(string name, Curve crv, double depth=0)
        {
            Name = name;
            Outline = crv.DuplicateCurve();
            Depth = depth;

            Plane pocket_plane;
            Outline.TryGetPlane(out pocket_plane);

            ToolAxis = pocket_plane.ZAxis;
        }

        public Pocket(string name, Curve crv, Vector3d tool_axis, double depth = 0)
        {
            Name = name;
            Outline = crv.DuplicateCurve();
            Depth = depth;

            ToolAxis = tool_axis;
        }

        public override Feature Duplicate()
        {
            return new Pocket(Name, Outline, Depth);
        }

        public override Brep ToBrep()
        {
            return Extrusion.Create(Outline, Depth, true).ToBrep();
        }

        public override Curve ToNurbsCurve()
        {
            return Outline.DuplicateCurve();
        }

        public override void Transform(Transform xform)
        {
            Outline.Transform(xform);
        }
    }

    public class EndCut : Feature
    {
        public static Interval CutterInterval = new Interval(-500, 500);
        public Plane CutPlane;

        public EndCut(string name, Plane cut_plane)
        {
            Name = name;
            CutPlane = cut_plane;
        }

        public override Feature Duplicate()
        {
            return new EndCut(Name, CutPlane);
        }

        public override Brep ToBrep()
        {
            return Brep.CreateFromCornerPoints(
                CutPlane.PointAt(CutterInterval.Min, CutterInterval.Min),
                CutPlane.PointAt(CutterInterval.Max, CutterInterval.Min),
                CutPlane.PointAt(CutterInterval.Max, CutterInterval.Max),
                CutPlane.PointAt(CutterInterval.Min, CutterInterval.Max),
                0.01
                );
        }

        public override Curve ToNurbsCurve()
        {
            return new Rectangle3d(CutPlane, CutterInterval, CutterInterval).ToNurbsCurve();
        }

        public override void Transform(Transform xform)
        {
            CutPlane.Transform(xform);
        }
    }
}
