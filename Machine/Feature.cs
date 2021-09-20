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
}
