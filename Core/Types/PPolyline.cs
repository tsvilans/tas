/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System.Collections.Generic;
using System.Linq;

using Rhino.Geometry;

namespace tas.Core.Types
{
    // PlanePolyine AKA GlorifiedListOfPlanes (previously OrientedPolyline)
    public class PPolyline: Rhino.Collections.RhinoList<Plane>
    {        
        public PPolyline()
        {
        }

        public PPolyline(ICollection<Plane> planes)
        {
            this.AddRange(planes);
        }

        public PPolyline(ICollection<Point3d> points)
        {
            foreach (Point3d p in points)
            {
                this.Add(new Plane(p, Vector3d.ZAxis));
            }
        }

        public PPolyline(IEnumerable<Point3d> points, Plane plane)
        {
            foreach (Point3d p in points)
            {
                plane.Origin = p;
                this.Add(plane);
            }
        }

        public PPolyline(Polyline poly)
        {
            for (int i = 0; i < poly.Count; ++i)
            {
                this.Add(new Plane(poly[i], Vector3d.ZAxis));
            }
        }

        public static explicit operator Polyline(PPolyline a)
        {
            return new Polyline(a.Select(x => x.Origin));
        }

        public static explicit operator PPolyline(Polyline a)
        {
            PPolyline b = new PPolyline(a);
            return b;
        }

        public static implicit operator List<Plane>(PPolyline op)
        {
            return op.ToList();
        }

        public static implicit operator PPolyline(List<Plane> pp)
        {
            return new PPolyline(pp);
        }

        public PolylineCurve PolylineCurve()
        {
            return new Rhino.Geometry.PolylineCurve(this.Select(x => x.Origin));
        }

        public bool IsClosed
        {
            get {
                if (Count < 3) return false;
                else if (First.Origin.DistanceTo(Last.Origin) < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) return true;
                return false;
            }
        }

        public new PPolyline Duplicate()
        {
            List<Plane> p = new List<Plane>();
            for (int i = 0; i < this.Count; ++i)
            {
                p.Add(this[i]);
            }
            return new PPolyline(p);
        }

        public void OverrideOrientation(Plane p)
        {
            for (int i = 0; i < base.Count; ++i)
            {
                p.Origin = this[i].Origin;
                this[i] = p;
            }
        }

        public PPolyline ShallowClone() => MemberwiseClone() as PPolyline;

        public void Join(PPolyline poly)
        {
            this.AddRange(poly);
        }

        public void Join(IEnumerable<PPolyline> poly)
        {
            foreach (PPolyline op in poly)
            {
                this.AddRange(op);
            }
        }

        public void Transform(Transform x)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                this[i].Transform(x);
            }
        }

    }
}