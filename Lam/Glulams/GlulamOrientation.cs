/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017-2018 Tom Svilans
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using tas.Core;
using tas.Core.Util;

using Rhino.Collections;

namespace tas.Lam
{
    public abstract class GlulamOrientation
    {
        public abstract Vector3d GetOrientation(Curve crv, double t);

        public Vector3d GetOrientation(Curve crv, Point3d pt)
        {
            double t;
            crv.ClosestPoint(pt, out t);
            return GetOrientation(crv, t);
        }

        public abstract List<Vector3d> GetOrientations(Curve crv, List<double> t);

        public abstract void Remap(Curve old_curve, Curve new_curve);

        public abstract List<GlulamOrientation> Split(ICollection<double> t);

        public abstract GlulamOrientation Join(GlulamOrientation orientation);

        public abstract GlulamOrientation Duplicate();

        public abstract void Transform(Transform x);

        public abstract object GetDriver();

        protected Vector3d NormalizeVector(Curve crv, double t, Vector3d v)
        {
            v.Unitize();
            Vector3d tan = crv.TangentAt(t);
            Vector3d xprod = Vector3d.CrossProduct(tan, v);

            return Vector3d.CrossProduct(xprod, tan);
        }
    }

    /// <summary>
    /// An orientation consisting of a single direction vector. The 
    /// vector should not be parallel to the curve at any point.
    /// </summary>
    public class VectorOrientation : GlulamOrientation
    {
        private Vector3d m_vector;

        public VectorOrientation(Vector3d v)
        {
            m_vector = v;
        }

        public override Vector3d GetOrientation(Curve crv, double t)
        {
            return NormalizeVector(crv, t, m_vector);
        }

        public override List<Vector3d> GetOrientations(Curve crv, List<double> t)
        {
            return t.Select(x => GetOrientation(crv, x)).ToList();
        }

        public override void Remap(Curve old_curve, Curve new_curve)
        {
            return;
        }

        public override GlulamOrientation Duplicate()
        {
            return new VectorOrientation(m_vector);
        }

        public override List<GlulamOrientation> Split(ICollection<double> t)
        {
            List<GlulamOrientation> new_orientations = new List<GlulamOrientation>();
            new_orientations.Add(this.Duplicate());
            foreach (double param in t)
            {
                new_orientations.Add(this.Duplicate());
            }
            return new_orientations;
        }

        public override object GetDriver()
        {
            return m_vector;
        }

        public override GlulamOrientation Join(GlulamOrientation orientation)
        {
            throw new NotImplementedException();
        }

        public override void Transform(Transform x)
        {
            m_vector.Transform(x);
        }
    }

    /// <summary>
    /// An orientation consisting of a list of vectors arranged at specific parameters
    /// along a curve. Vectors should be perpendicular to the curve upon input, 
    /// otherwise it might give funny results.
    /// </summary>
    public class VectorListOrientation : GlulamOrientation
    {
        protected List<Vector3d> m_vectors;
        protected List<double> m_parameters;
        protected Curve m_curve;

        public VectorListOrientation(Curve curve, List<double> parameters, List<Vector3d> vectors)
        {
            m_curve = curve;
            m_vectors = new List<Vector3d>();
            m_parameters = new List<double>();

            int N = Math.Min(parameters.Count, vectors.Count);

            for (int i = 0; i < N; ++i)
            {
                if (vectors[i].IsValid)
                {    
                    m_vectors.Add(vectors[i]);
                    m_parameters.Add(parameters[i]);
                }
            }

            if (m_vectors.Count != m_parameters.Count || m_vectors.Count == 0 || m_parameters.Count == 0)
            {
                throw new Exception("No valid vectors or parameters.");
            }

            Sort();
            RecalculateVectors();
        }

        public void Sort()
        {
            // Zip and sort using LINQ
            var ordered_zip = m_parameters.Zip(m_vectors, (x, y) => new { x, y })
                      .OrderBy(pair => pair.x)
                      .ToList();

            m_parameters = ordered_zip.Select(pair => pair.x).ToList();
            m_vectors = ordered_zip.Select(pair => pair.y).ToList();
        }

        public override Vector3d GetOrientation(Curve crv, double t)
        {

            Vector3d vec;
            if (t < m_parameters.First())
            {
                vec = m_vectors.First();
            }
            else if (t > m_parameters.Last())
            {
                vec = m_vectors.Last();
            }

            int res = m_parameters.BinarySearch(t);
            int max = m_parameters.Count - 1;
            double mu;

            if (res < 0)
            {
                res = ~res;
                res--;
            }

            if (res >= 0 && res < max)
            {
                mu = (t - m_parameters[res]) / (m_parameters[res + 1] - m_parameters[res]);
                vec = Interpolation.Slerp(m_vectors[res], m_vectors[res + 1], mu);
            }
            else if (res < 0)
                vec = m_vectors.First();
            else if (res >= max)
                vec = m_vectors.Last();
            else
                vec = Vector3d.Unset;

            return NormalizeVector(crv, t, vec);
        }

        /// <summary>
        /// This particular one can be optimized as in the older FreeformGlulam code
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public override List<Vector3d> GetOrientations(Curve crv, List<double> t)
        {
            return t.Select(x => GetOrientation(crv, x)).ToList();
        }

        public override void Remap(Curve old_curve, Curve new_curve)
        {
            Point3d pt;
            double t;
            for (int i = 0; i < m_parameters.Count; ++i)
            {
                pt = old_curve.PointAt(m_parameters[i]);
                new_curve.ClosestPoint(pt, out t);
                m_parameters[i] = t;
            }

            m_parameters.Reverse();
            m_vectors.Reverse();
        }

        public override GlulamOrientation Duplicate()
        {
            return new VectorListOrientation(m_curve, m_parameters, m_vectors);
        }

        public override List<GlulamOrientation> Split(ICollection<double> t)
        {
            List<GlulamOrientation> new_orientations = new List<GlulamOrientation>();

            if (t.Count < 1)
            {
                new_orientations.Add(this.Duplicate());
                return new_orientations;
            }

            int prev = 0;
            double prev_param = 0.0;

            List<double> new_parameters;
            List<Vector3d> new_vectors;

            foreach (double param in t)
            {
                int res = m_parameters.BinarySearch(param);

                if (res < 0)
                {
                    res = ~res;

                    new_parameters = m_parameters.GetRange(prev, res - prev);
                    new_vectors = m_vectors.GetRange(prev, res - prev);

                    new_parameters.Add(param);
                    new_vectors.Add(GetOrientation(m_curve, prev_param));

                    if (prev > 0)
                    {
                        new_parameters.Insert(0, prev_param);
                        new_vectors.Insert(0, GetOrientation(m_curve, prev_param));
                    }

                    new_orientations.Add(new VectorListOrientation(
                        m_curve,
                        new_parameters,
                        new_vectors));

                    prev_param = param;
                    prev = res;
                }
                else
                {
                    new_parameters = m_parameters.GetRange(prev, res - prev + 1);
                    new_vectors = m_vectors.GetRange(prev, res - prev + 1);

                    new_orientations.Add(new VectorListOrientation(
                        m_curve,
                        new_parameters,
                        new_vectors));

                    prev = res;
                }
            }

            new_parameters = m_parameters.GetRange(prev, m_parameters.Count - prev);
            new_vectors = m_vectors.GetRange(prev, m_vectors.Count - prev);

            new_parameters.Insert(0, t.Last());
            new_vectors.Insert(0, Vector3d.Zero);

            new_orientations.Add(new VectorListOrientation(
                m_curve,
                new_parameters,
                new_vectors));

            return new_orientations;
        }

        public override object GetDriver()
        {
            return m_parameters.Zip(m_vectors, (x, y) => new { x, y })
                      .OrderBy(pair => pair.x)
                      .ToList();
        }

        public void RecalculateVectors()
        {
            for (int i = 0; i < m_parameters.Count; ++i)
                m_vectors[i] = NormalizeVector(m_curve, m_parameters[i], m_vectors[i]);
        }

        public override GlulamOrientation Join(GlulamOrientation orientation)
        {
            if (orientation is VectorListOrientation)
            {
                VectorListOrientation vlo = orientation as VectorListOrientation;
                m_parameters.AddRange(vlo.m_parameters);
                m_vectors.AddRange(vlo.m_vectors);

                Sort();
            }
            return this.Duplicate();
        }

        public override void Transform(Transform x)
        {
            for (int i = 0; i < m_vectors.Count; ++i)
            {
                m_vectors[i].Transform(x);
            }
        }
    }

    public class SurfaceOrientation : GlulamOrientation
    {
        private Brep m_surface;

        public SurfaceOrientation(Brep srf)
        {
            m_surface = srf;
        }

        public override Vector3d GetOrientation(Curve crv, double t)
        {
            Point3d pt = crv.PointAt(t);
            double u, v;
            ComponentIndex ci;
            Point3d cp;
            Vector3d normal;

            m_surface.ClosestPoint(pt, out cp, out ci, out u, out v, 0, out normal);

            return NormalizeVector(crv, t, normal);
        }


        public override List<Vector3d> GetOrientations(Curve crv, List<double> t)
        {
            return t.Select(x => GetOrientation(crv, x)).ToList();
        }

        public override void Remap(Curve old_curve, Curve new_curve)
        {
            return;
        }

        public override GlulamOrientation Duplicate()
        {
            return new SurfaceOrientation(m_surface);
        }


        public override List<GlulamOrientation> Split(ICollection<double> t)
        {
            List<GlulamOrientation> new_orientations = new List<GlulamOrientation>();
            new_orientations.Add(this.Duplicate());
            foreach (double param in t)
            {
                new_orientations.Add(this.Duplicate());
            }
            return new_orientations;
        }
   
        public override object GetDriver()
        {
            return m_surface;
        }

        public override GlulamOrientation Join(GlulamOrientation orientation)
        {
            return this.Duplicate();
        }

        public override void Transform(Transform x)
        {
            return;
        }
    }

    public class RailCurveOrientation : GlulamOrientation
    {
        private Curve m_curve;
        private bool m_closest;

        /// <summary>
        /// Use the closest curve point. Otherwise, use the same parameter
        /// or curve length.
        /// </summary>
        public bool ClosestPoint
        {
            get { return m_closest; }
            set { m_closest = value; }
        }

        public RailCurveOrientation(Curve curve)
        {
            m_curve = curve.DuplicateCurve();
            m_closest = true;
        }

        public override Vector3d GetOrientation(Curve crv, double t)
        {
            Vector3d v;
            Point3d pt = crv.PointAt(t);
            if (m_closest)
            {
                double t2;
                m_curve.ClosestPoint(pt, out t2);
                v = m_curve.PointAt(t2) - pt;
            }
            else
            {
                v = m_curve.PointAt(t) - pt;
            }

            return NormalizeVector(crv, t, v);
        }

        public override List<Vector3d> GetOrientations(Curve crv, List<double> t)
        {
            return t.Select(x => GetOrientation(crv, x)).ToList();
        }

        public override void Remap(Curve old_curve, Curve new_curve)
        {
            //m_curve.Reverse();
        }

        public override GlulamOrientation Duplicate()
        {
            return new RailCurveOrientation(m_curve);
        }

        public override List<GlulamOrientation> Split(ICollection<double> t)
        {
            List<GlulamOrientation> new_orientations = new List<GlulamOrientation>();
            new_orientations.Add(this.Duplicate());
            foreach (double param in t)
            {
                new_orientations.Add(this.Duplicate());
            }
            return new_orientations;
        }

        public override object GetDriver()
        {
            return m_curve;
        }

        public override GlulamOrientation Join(GlulamOrientation orientation)
        {
            return this.Duplicate();
        }

        public override void Transform(Transform x)
        {
            return;
        }
    }

    /// <summary>
    /// Orientation that does nothing. Direction vector is the 
    /// normalized curvature vector of the queried curve.
    /// </summary>
    public class KCurveOrientation : GlulamOrientation
    {

        public KCurveOrientation()
        {
        }

        public override Vector3d GetOrientation(Curve crv, double t)
        {
            Vector3d k = crv.CurvatureAt(t);
            k.Unitize();
            return k;
        }
        public override List<Vector3d> GetOrientations(Curve crv, List<double> t)
        {
            return t.Select(x => GetOrientation(crv, x)).ToList();
        }

        public override void Remap(Curve old_curve, Curve new_curve)
        {
            return;
        }

        public override GlulamOrientation Duplicate()
        {
            return new KCurveOrientation();
        }

        public override List<GlulamOrientation> Split(ICollection<double> t)
        {
            List<GlulamOrientation> new_orientations = new List<GlulamOrientation>();
            new_orientations.Add(this.Duplicate());
            foreach (double param in t)
            {
                new_orientations.Add(this.Duplicate());
            }
            return new_orientations;
        }

        public override object GetDriver()
        {
            return null;
        }

        public override GlulamOrientation Join(GlulamOrientation orientation)
        {
            return this.Duplicate();
        }

        public override void Transform(Transform x)
        {
            return;
        }
    }
}
