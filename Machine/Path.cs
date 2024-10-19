using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using Rhino.Collections;

namespace tas.Machine
{
    public class Path : RhinoList<Plane>, ICloneable
    {
        public Path()
        {

        }

        public Path(IEnumerable<Plane> collection)
        {
            this.AddRange(collection);
        }

        public Path(Polyline polyline, Plane orientation)
        {
            foreach (Point3d p in polyline)
            {
                orientation.Origin = p;
                this.Add(orientation);
            }
        }

        public Path(Polyline polyline)
        {
            Plane plane = Plane.WorldXY;
            foreach (Point3d p in polyline)
            {
                plane.Origin = p;
                this.Add(plane);
            }
        }

        public static Path JoinPaths(IEnumerable<Path> paths)
        {
            var path = new Path();
            foreach (var  p in paths)
            {
                path.AddRange(p);
            }

            return path;
        }

        public static Path CreateRamp(Path poly, Plane pl, double height, double length)//, ref string debug)
        {
            if (poly.Count < 1) return null;

            var path = new Path(poly);

            path.Reverse();
            int N = path.Count;
            if (path.IsClosed)
                N--;
            double th = 0.0;
            double td = 0.0;
            int i = 0;
            int next = 1;
            List<Plane> rpts = new List<Plane>();

            while (th < length)
            {
                // distance between i and next
                td = path[next].Origin.DistanceTo(path[i].Origin);

                if (th + td >= length)
                {
                    double t = ((th + td) - length) / td; // get t value for lerp
                    rpts.Add(path[i]);
                    rpts.Add(tas.Core.Util.Interpolation.InterpolatePlanes(path[i], path[next], 1.0 - t));

                    break;
                }
                th += td;
                rpts.Add(path[i]);
                i = (i + 1) % N;
                next = (i + 1) % N;
            }

            double L = 0.0;
            List<double> el = new List<double>();
            for (int j = 0; j < rpts.Count - 1; ++j)
            {
                double d = rpts[j].Origin.DistanceTo(rpts[j + 1].Origin);
                L += d;
                el.Add(d);
            }

            double LL = 0.0;
            for (int j = 1; j < rpts.Count; ++j)
            {
                LL += el[j - 1];

                double z = (LL / L) * height;
                Plane p = new Plane(rpts[j]);
                //p.Origin = p.Origin + pl.ZAxis * z;
                p.Origin = p.Origin + p.ZAxis * z;
                rpts[j] = p;
            }
            rpts.Reverse();

            return new Path(rpts);
        }

        public BoundingBox BoundingBox { get 
            {
                if (this.Count < 1) return BoundingBox.Empty;
                return new BoundingBox(this.Select(x => x.Origin));
            }
        }
        public void Transform(Transform xform)
        {
            for (int i = 0; i < this.Count; ++i)
                this[i].Transform(xform);
        }

        public PolylineCurve PolylineCurve()
        {
            return new Rhino.Geometry.PolylineCurve(this.Select(x => x.Origin));
        }

        public bool IsClosed
        {
            get
            {
                if (Count < 3) return false;
                else if (First.Origin.DistanceTo(Last.Origin) < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) return true;
                return false;
            }
        }

        public new Path Duplicate()
        {
            return new Path(this);

            List<Plane> p = new List<Plane>();
            for (int i = 0; i < this.Count; ++i)
            {
                p.Add(this[i]);
            }
            return new Path(p);
        }

        public void OverrideOrientation(Plane p)
        {
            for (int i = 0; i < base.Count; ++i)
            {
                p.Origin = this[i].Origin;
                this[i] = p;
            }
        }

        public Path ShallowClone() => MemberwiseClone() as Path;

        public void Join(Path poly)
        {
            this.AddRange(poly);
        }

        public void Join(IEnumerable<Path> poly)
        {
            foreach (Path op in poly)
            {
                this.AddRange(op);
            }
        }

        public List<Plane> ToPlanes()
        {
            return this.ToList<Plane>();
        }

    }
}
