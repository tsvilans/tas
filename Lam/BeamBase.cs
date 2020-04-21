using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using tas.Core.Util;

using Rhino.Geometry;

namespace tas.Lam
{
    public abstract class BeamBase
    {
        public Curve Centreline { get; protected set; }
        public GlulamOrientation Orientation;

        public Plane GetPlane(double t) => Misc.PlaneFromNormalAndYAxis(
                                                        Centreline.PointAt(t),
                                                        Centreline.TangentAt(t),
                                                        Orientation.GetOrientation(Centreline, t));
        public Plane GetPlane(Point3d pt)
        {
            double t;
            Centreline.ClosestPoint(pt, out t);
            Vector3d v = Orientation.GetOrientation(Centreline, t);
            return tas.Core.Util.Misc.PlaneFromNormalAndYAxis(Centreline.PointAt(t), Centreline.TangentAt(t), v);
        }
        public void Transform(Transform x)
        {
            Centreline.Transform(x);
            Orientation.Transform(x);
        }

    }
}
