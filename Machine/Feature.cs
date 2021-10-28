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
        public Pocket(string name, Curve crv, double depth=0)
        {
            Name = name;
            Outline = crv.DuplicateCurve();
            Depth = depth;

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
}
