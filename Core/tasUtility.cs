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
using System.Xml.Linq;

using Rhino.Geometry;
using StudioAvw.Geometry;

using CarveSharp;

using tas.Core.Types;
using System.Drawing;

namespace tas.Core
{
    public static class Constants
    {
        public const double Tau = Math.PI * 2.0;

    }

    public enum Side
    {
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32
    }

    public static class Util
    {
        public enum Axis
        {
            XPos,
            XNeg,
            YPos,
            YNeg,
            ZPos,
            ZNeg,
            Undefined
        }

        public static class Ease
        {
            public static double QuadOut(double t)
            {
                return -1.0 * t * (t - 2);
            }

            public static double QuadIn(double t)
            {
                return t * t * t;
            }

            public static double CubicIn(double t)
            {
                return t * t * t;
            }

            public static double CubicOut(double t)
            {
                t--;
                return t * t * t + 1;
            }
        }

        public static class Interpolation
        {
            /// <summary>
            /// from http://paulbourke.net/miscellaneous/interpolation/
            /// Tension: 1 is high, 0 normal, -1 is low
            /// Bias: 0 is even,
            /// positive is towards first segment,
            /// negative towards the other
            /// </summary>
            /// <param name="y0"></param>
            /// <param name="y1"></param>
            /// <param name="y2"></param>
            /// <param name="y3"></param>
            /// <param name="mu"></param>
            /// <param name="tension"></param>
            /// <param name="bias"></param>
            /// <returns></returns>
            public static double HermiteInterpolate(double y0, double y1, double y2, double y3, double mu, double tension, double bias)
            {
                double m0, m1, mu2, mu3;
                double a0, a1, a2, a3;

                mu2 = mu * mu;
                mu3 = mu2 * mu;
                m0 = (y1 - y0) * (1 + bias) * (1 - tension) / 2;
                m0 += (y2 - y1) * (1 - bias) * (1 - tension) / 2;
                m1 = (y2 - y1) * (1 + bias) * (1 - tension) / 2;
                m1 += (y3 - y2) * (1 - bias) * (1 - tension) / 2;
                a0 = 2 * mu3 - 3 * mu2 + 1;
                a1 = mu3 - 2 * mu2 + mu;
                a2 = mu3 - mu2;
                a3 = -2 * mu3 + 3 * mu2;

                return (a0 * y1 + a1 * m0 + a2 * m1 + a3 * y2);
            }

            /// <summary>
            /// from http://paulbourke.net/miscellaneous/interpolation/
            /// </summary>
            /// <param name="y0"></param>
            /// <param name="y1"></param>
            /// <param name="y2"></param>
            /// <param name="y3"></param>
            /// <param name="mu"></param>
            /// <returns></returns>
            public static double CubicInterpolate(double y0, double y1, double y2, double y3, double mu)
            {
                double a0, a1, a2, a3, mu2;

                mu2 = mu * mu;
                a0 = y3 - y2 - y0 + y1;
                a1 = y0 - y1 - a0;
                a2 = y2 - y0;
                a3 = y1;

                return (a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3);
            }

            /// <summary>
            /// from http://paulbourke.net/miscellaneous/interpolation/
            /// </summary>
            /// <param name="y1"></param>
            /// <param name="y2"></param>
            /// <param name="mu"></param>
            /// <returns></returns>
            public static double LinearInterpolate(double y1, double y2, double mu)
            {
                return (y1 * (1 - mu) + y2 * mu);
            }

            /// <summary>
            /// from http://paulbourke.net/miscellaneous/interpolation/
            /// </summary>
            /// <param name="y1"></param>
            /// <param name="y2"></param>
            /// <param name="mu"></param>
            /// <returns></returns>
            public static double CosineInterpolate(double y1, double y2, double mu)
            {
                double mu2;
                mu2 = (1 - Math.Cos(mu * Math.PI)) / 2;
                return (y1 * (1 - mu2) + y2 * mu2);
            }

            /// <summary>
            /// Spherical interpolation using quaternions.
            /// </summary>
            /// <param name="qA">Quaternion A.</param>
            /// <param name="qB">Quaternion B.</param>
            /// <param name="t">t-value.</param>
            /// <returns></returns>
            public static Quaternion Slerp(Quaternion qA, Quaternion qB, double t)
            {
                if (t == 0) return qA;
                if (t == 1.0) return qB;

                Quaternion qC = new Quaternion();
                double cosHT = qA.A * qB.A + qA.B * qB.B + qA.C * qB.C + qA.D * qB.D;

                if (cosHT < 0.0)
                {
                    qC.A = -qB.A;
                    qC.B = -qB.B;
                    qC.C = -qB.C;
                    qC.D = -qB.D;
                    cosHT = -cosHT;
                }
                else
                    qC = qB;

                if (cosHT >= 1.0)
                {
                    qC.A = qA.A;
                    qC.B = qA.B;
                    qC.C = qA.C;
                    qC.D = qA.D;
                    return qC;
                }
                double HT = Math.Acos(cosHT);
                double sinHT = Math.Sqrt(1.0 - cosHT * cosHT);

                if (Math.Abs(sinHT) < 0.001)
                {
                    qC.A = 0.5 * (qA.A + qC.A);
                    qC.B = 0.5 * (qA.B + qC.B);
                    qC.C = 0.5 * (qA.C + qC.C);
                    qC.D = 0.5 * (qA.D + qC.D);
                    return qC;
                }

                double ratioA = Math.Sin((1 - t) * HT) / sinHT;
                double ratioB = Math.Sin(t * HT) / sinHT;

                qC.A = qA.A * ratioA + qC.A * ratioB;
                qC.B = qA.B * ratioA + qC.B * ratioB;
                qC.C = qA.C * ratioA + qC.C * ratioB;
                qC.D = qA.D * ratioA + qC.D * ratioB;
                return qC;
            }

            public static Vector3d Slerp(Vector3d v1, Vector3d v2, double t)
            {
                double dot = v1 * v2;
                double theta = Math.Acos(dot) * t;
                Vector3d rel = v2 - v1 * dot;
                rel.Unitize();

                return ((v1 * Math.Cos(theta)) + rel * Math.Sin(theta));
            }

            /// <summary>
            /// Simple plane interpolation using interpolated vectors. Not ideal. 
            /// Fails spectacularly in extreme cases.
            /// </summary>
            /// <param name="A">Plane A.</param>
            /// <param name="B">Plane B.</param>
            /// <param name="t">t-value.</param>
            /// <returns></returns>
            public static Plane InterpolatePlanes(Plane A, Plane B, double t)
            {
                return new Plane(Lerp(A.Origin, B.Origin, t),
                                         Lerp(A.XAxis, B.XAxis, t),
                                         Lerp(A.YAxis, B.YAxis, t));
            }

            /// <summary>
            /// Better plane interpolation using quaternions.
            /// </summary>
            /// <param name="A">Plane A.</param>
            /// <param name="B">Plane B.</param>
            /// <param name="t">t-value.</param>
            /// <returns></returns>
            public static Plane InterpolatePlanes2(Plane A, Plane B, double t)
            {
                Quaternion qA = Quaternion.Rotation(Plane.WorldXY, A);
                Quaternion qB = Quaternion.Rotation(Plane.WorldXY, B);

                Quaternion qC = Slerp(qA, qB, t);
                Point3d p = Lerp(A.Origin, B.Origin, t);

                Plane plane;
                qC.GetRotation(out plane);
                plane.Origin = p;

                return plane;
            }

            /// <summary>
            /// Simple lerp between two colors.
            /// </summary>
            /// <param name="colorA">Color A.</param>
            /// <param name="colorB">Color B.</param>
            /// <param name="t">t-value.</param>
            /// <returns>Interpolated color.</returns>
            public static Color Lerp(Color colorA, Color colorB, double t)
            {
                int r = (int)(colorB.R * t + (colorA.R * (1.0 - t)));
                int g = (int)(colorB.G * t + (colorA.G * (1.0 - t)));
                int b = (int)(colorB.B * t + (colorA.B * (1.0 - t)));

                return Color.FromArgb(r, g, b);
            }

            /// <summary>
            /// Simple linear interpolation between two points.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="t"></param>
            /// <returns></returns>
            public static Point3d Lerp(Point3d a, Point3d b, double t)
            {
                return a + t * (b - a);
                //return new Point3d(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t), Lerp(a.Z, b.Z, t));
            }

            /// <summary>
            /// Simple linear interpolation between two vectors.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <param name="t"></param>
            /// <returns></returns>
            public static Vector3d Lerp(Vector3d a, Vector3d b, double t)
            {
                return a + t * (b - a);
            }

            public static double Lerp(double a, double b, double t)
            {
                return a + (b - a) * t;
            }

            public static double Unlerp(double a, double b, double c)
            {
                if (a > b)
                    return 1.0 - (c - b) / (a - b);
                return (c - a) / (b - a);
            }
        }

        static Random random = new Random();



        /// <summary>
        /// Project point onto plane.
        /// </summary>
        /// <param name="p">Point to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected point.</returns>
        public static Point3d ProjectToPlane(Point3d p, Plane pl)
        {
            Vector3d op = new Vector3d(p - pl.Origin);
            double dot = Vector3d.Multiply(pl.ZAxis, op);
            Vector3d v = pl.ZAxis * dot;
            return new Point3d(p - v);
        }

        /// <summary>
        /// Project vector onto plane.
        /// </summary>
        /// <param name="v">Vector to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected vector.</returns>
        public static Vector3d ProjectToPlane(Vector3d v, Plane pl)
        {
            double dot = Vector3d.Multiply(pl.ZAxis, v);
            Vector3d v2 = pl.ZAxis * dot;
            return new Vector3d(v - v2);
        }

        /// <summary>
        /// Offset until Offset() cries for help.
        /// </summary>
        /// <param name="crvs"></param>
        /// <param name="off"></param>
        /// <param name="p"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static List<Curve> OffsetUntilNone(Curve[] crvs, double off, Plane p, int limit)
        {
            List<Curve> cl = new List<Curve>();
            if (crvs == null || limit < 1) return cl;
            Curve[] jcrvs = Curve.JoinCurves(crvs);
            for (int i = 0; i < jcrvs.Length; ++i)
            {
                if (jcrvs[i].IsClosed)
                {
                    Curve[] c = jcrvs[i].Offset(p, off, 0.1, CurveOffsetCornerStyle.Sharp);
                    cl.Add(jcrvs[i]);
                    cl.AddRange(OffsetUntilNone(c, off, p, limit - 1));
                }
            }

            for (int i = 0; i < cl.Count; ++i)
            {
                cl[i] = cl[i].ToPolyline(0, 6, 0.1, 0.1, 0.1, 0.1, 0.2, 50.0, true);
            }
            return cl;
        }

        /// <summary>
        /// Inset a closed polyline until it can't no more.
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="d"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static List<Polyline> InsetUntilNone(Polyline pl, double d, Plane p)
        {
            List<Polyline> pls = new List<Polyline>();
            if (!pl.IsClosed) return pls;
            List<Polyline> offC, offH;
            List<Polyline> tpl = new List<Polyline>();
            tpl.Add(pl);

            while (tpl.Count > 0)
            {
                Polyline3D.Offset(tpl, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, d, p,
                  0.01, out offC, out offH);
                pls.AddRange(offH);
                tpl = offH;
            }
            return pls;
        }

        public static List<Polyline> InsetUntilNone(List<Polyline> pl, double d, Plane p)
        {
            List<Polyline> pls = new List<Polyline>();
            List<Polyline> offC, offH;
            List<Polyline> tpl = new List<Polyline>();

            for (int i = 0; i < pl.Count; ++i)
            {
                if (pl[i].IsClosed)
                    tpl.Add(pl[i]);
            }

            while (tpl.Count > 0)
            {
                Polyline3D.Offset(tpl, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, d, p,
                  0.01, out offC, out offH);
                pls.AddRange(offH);
                tpl = offH;
            }
            return pls;
        }

        public static void InsetUntilNoneClustersRecursive(Polyline Poly, double Distance, Plane P, ref List<List<Polyline>> Clusters, int Index)
        {
            List<Polyline> OffsetPolylines = new List<Polyline>();
            List<Polyline> Contours, Holes;

            if (!Poly.IsClosed) return;
            OffsetPolylines.Add(Poly);
            Clusters[Index].Add(Poly);

            while (OffsetPolylines.Count > 0)
            {
                Polyline3D.Offset(OffsetPolylines, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter,
                    Distance, P,
                    Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                    out Contours, out Holes);

                if (Holes.Count > 1)
                {
                    for (int i = 1; i < Holes.Count; ++i)
                    {
                        if (Holes[i].Count < 2) continue;
                        Clusters.Add(new List<Polyline>());
                        InsetUntilNoneClustersRecursive(Holes[i], Distance, P, ref Clusters, Clusters.Count - 1);
                    }
                }

                if (Holes.Count > 0)
                {
                    Clusters[Index].Add(Holes[0]);
                    OffsetPolylines = new List<Polyline> { Holes[0] };
                }
                else break;
            }
        }

        public static List<List<Polyline>> InsetUntilNoneClusters(Polyline Poly, double Distance, Plane P)
        {
            
            List<List<Polyline>> Clusters = new List<List<Polyline>>();
            Clusters.Add(new List<Polyline>());

            InsetUntilNoneClustersRecursive(Poly, Distance, P, ref Clusters, 0);

            return Clusters;
            /*
            List<Polyline> OffsetPolylines = new List<Polyline>();
            List<Polyline> Contours, Holes;

            if (Poly.IsClosed)
            {
                Clusters.Add(new List<Polyline>());
                OffsetPolylines.Add(Poly);
            }

            while (OffsetPolylines.Count > 0)
            {
                Polyline3D.Offset(OffsetPolylines, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, 
                    Distance, P, 
                    Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                    out Contours, out Holes);

                while (Holes.Count > Clusters.Count)
                    Clusters.Add(new List<Polyline>());

                for (int i = 0; i < Holes.Count; ++i)
                    Clusters[i].Add(Holes[i]);

                OffsetPolylines = Holes;
            }*/
            //return Clusters;
        }



        public static bool IsCollinear(Point3d p0, Point3d p1, Point3d p2, double tol = 1e-12)
        {
            Vector3d v1 = p0 - p1;
            Vector3d v2 = p2 - p1;
            v1.Unitize();
            v2.Unitize();
            Vector3d n = Vector3d.CrossProduct(v1, v2);
            //if ((1 - Math.Abs(Vector3d.Multiply(p0 - p1, p2 - p1)) < tol)) return true;
            if (n.Length < tol) return true;
            return false;
        }


        public static double Distance(Point3d a, Point3d b)
        {
            double d = (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
            return Math.Sqrt(d);
        }

        /// <summary>
        /// Get squared distance between two points. Avoids calculating the square root when not needed.
        /// </summary>
        /// <param name="a">Point A.</param>
        /// <param name="b">Point B.</param>
        /// <returns>Squared distance between points A and B.</returns>
        public static double DistanceSq(Point3d a, Point3d b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z); 
        }

        public static List<Polyline> CurvesToPolylines(IEnumerable<Curve> crvs, double tol, bool reduce = false)
        {
            List<Polyline> polys = new List<Polyline>();

            foreach (Curve c in crvs)
            {
                if (c == null) continue;

                Polyline pl;
                if (c.IsPolyline() && c.TryGetPolyline(out pl))
                {
                    polys.Add(pl);
                }
                else
                {
                    polys.Add(CurveToPolyline(c, tol));
                }
            }
            return polys;
        }

        public static Polyline CurveToPolyline(Curve crv, double tol, bool reduce = false)
        {
            PolylineCurve pc = crv.ToPolyline(0, 0, 0, 0, 0, tol, 0, 0, true);
            Polyline pl;
            pc.TryGetPolyline(out pl);
            if (reduce) pl.ReduceSegments(tol);
            if (pl.IsClosedWithinTolerance(tol))
                pl.Last = pl.First;
            return pl;

            /*
            double len = crv.GetLength();
            int N = (int)Math.Ceiling(len / tol);

            Point3d[] points;
            crv.DivideByCount(N, true, out points);

            Polyline poly = new Polyline(points);
            if (crv.IsClosed && !poly.IsClosed)
                poly.Add(poly.First);

            return poly;
            */

            /*
            double tmin = crv.Domain[0];
            double tmax = crv.Domain[1];

            double step = (tmax - tmin) / N;

            for (int i = 0; i < N; ++i)
            {
                pts[i] = crv.PointAt(tmin + step * i);
            }

            Polyline pl = new Polyline(pts);

            return pl;
            */
        }

        public static BrepFace BrepFaceFromPoint(Brep brep, Point3d testPoint)
        {
            BrepFace closest_face = null;
            if (null != brep && testPoint.IsValid)
            {
                double closest_dist = Rhino.RhinoMath.UnsetValue;
                foreach (BrepFace face in brep.Faces)
                {
                    double u, v;
                    if (face.ClosestPoint(testPoint, out u, out v))
                    {
                        Point3d face_point = face.PointAt(u, v);
                        double face_dist = face_point.DistanceTo(testPoint);
                        if (!Rhino.RhinoMath.IsValidDouble(closest_dist) || face_dist < closest_dist)
                        {
                            closest_dist = face_dist;
                            closest_face = face;
                        }
                    }
                }
            }
            return closest_face;
        }

        public static Vector3d ClosestNormalBrepFromPoint(Brep brep, Point3d testPoint)
        {
            double u, v;
            BrepFace bf = BrepFaceFromPoint(brep, testPoint);
            bf.ClosestPoint(testPoint, out u, out v);
            return bf.NormalAt(u, v);
        }

        public static void SliceBrep(Brep brep, Plane sliceplane, ref List<Curve> curves)
        {
            //brep.Faces.StandardizeFaceSurfaces();
            //BrepSolidOrientation bso = brep.SolidOrientation;
            
            Curve[] xCrvs;
            Point3d[] xPts;
            bool[] inside;
            Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, sliceplane, 0.001, out xCrvs, out xPts);
            if (xCrvs == null)
                return;

            xCrvs = Rhino.Geometry.Curve.JoinCurves(xCrvs);
            curves = new List<Curve>();
            inside = new bool[xCrvs.Length];
            for (int i = 0; i < xCrvs.Length; ++i)
            {
                //int sign = 1;
                Point3d tpt = xCrvs[i].PointAtNormalizedLength(0.5);
                Vector3d n = ClosestNormalBrepFromPoint(brep, tpt);
                n = ProjectToPlane(n, sliceplane);
                tpt.Transform(Transform.Translation(n * 0.001));
                PointContainment pcon = xCrvs[i].Contains(tpt, sliceplane, 0.0001);
                if (pcon == PointContainment.Inside)
                { 
                    inside[i] = true;
                    //sign = -1;
                }
                else
                {
                    inside[i] = false;
                    //sign = 1;
                }

                curves.Add(xCrvs[i]);
                Curve[] offsets = xCrvs[i].Offset(tpt, sliceplane.ZAxis, 3.0, 0.0001, CurveOffsetCornerStyle.Sharp);
                //Curve[] offsets = xCrvs[i].Offset(sliceplane, 3.0 * sign, 0.001, CurveOffsetCornerStyle.Sharp);
                curves.AddRange(offsets);
            }

            return;
            
  
        }

        public static PPolyline CreateRamp(PPolyline poly, Plane pl, double height, double length)//, ref string debug)
        {
            poly.Reverse();
            int N = poly.Count;
            if (poly.IsClosed)
                N--;
            double th = 0.0;
            double td = 0.0;
            int i = 0;
            int next = 1;
            List<Plane> rpts = new List<Plane>();

            while (th < length)
            {
                // distance between i and next
                td = poly[next].Origin.DistanceTo(poly[i].Origin);

                if (th + td >= length)
                {
                    double t = ((th + td) - length) / td; // get t value for lerp
                    rpts.Add(poly[i]);
                    rpts.Add(Interpolation.InterpolatePlanes(poly[i], poly[next], 1.0 - t));

                    break;
                }
                th += td;
                rpts.Add(poly[i]);
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

            return new PPolyline(rpts);
        }

        public static Polyline CreateRamp(Polyline poly, Plane pl, double height, double length)//, ref string debug)
        {
            poly.Reverse();
            int N = poly.Count;
            if (poly.IsClosed)
                N--;
            double th = 0.0;
            double td = 0.0;
            int i = 0;
            int next = 1;
            Vector3d dv;
            List<Point3d> rpts = new List<Point3d>();

            while (th < length)
            {
                // distance between i and next
                dv = poly[next] - poly[i];
                td = dv.Length;
                if (th + td >= length)
                {
                    double t = ((th + td) - length) / td; // get t value for lerp
                    rpts.Add(poly[i]);
                    rpts.Add(Interpolation.Lerp(poly[i], poly[next], 1.0 - t));

                    break;
                }
                th += td;
                rpts.Add(poly[i]);
                i = (i + 1) % N;
                next = (i + 1) % N;
            }

            List<double> el = new List<double>();
            for (int j = 0; j < rpts.Count; ++j)
            {
                int ind = (j + 1) % rpts.Count;
                double d = Distance(rpts[j], rpts[ind]);
                el.Add(d);
            }
            double L = el.Sum();

            double LL = 0.0;
            for (int j = 0; j < rpts.Count - 1; ++j)
            {
                LL = el.Take(j + 1).Sum();
                double z = (LL / L) * height;
                rpts[j + 1] = new Point3d(rpts[j + 1] + pl.ZAxis * z);
            }
            rpts.Reverse();

            return new Polyline(rpts);
        }

        public static Polyline SimplifyPolyline(Polyline poly, double tolerance = 1e-12)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(poly[0]);
            for (int i = 1; i < poly.Count - 1; ++i)
            {
                if (!IsCollinear(poly[i-1], poly[i], poly[i+1], tolerance))
                {
                    pts.Add(poly[i]);
                }
            }
            pts.Add(poly[poly.Count - 1]);
            return new Polyline(pts);
        }

        /// <summary>
        /// Gets closest plane axis to vector. For example, if the input vector is closest to the plane's -X-axis, it will return the -X-axis.
        /// </summary>
        /// <param name="p">Plane to test.</param>
        /// <param name="v">Vector to compare.</param>
        /// <returns>Plane axis closest to vector.</returns>
        public static Vector3d GetClosestAxis(Plane p, Vector3d v)
        {
            double x, y;
            x = Vector3d.Multiply(v, p.XAxis);
            y = Vector3d.Multiply(v, p.YAxis);
            if (Math.Abs(x) > Math.Abs(y))
            {
                if (x > 0.0) return p.XAxis;
                else return -p.XAxis;
            }
            else
            {
                if (y > 0.0) return p.YAxis;
                else return -p.YAxis;
            }
        }

        /// <summary>
        /// Gets closest plane axis to vector. For example, if the input vector is closest to the plane's -X-axis, it will return the -X-axis.
        /// </summary>
        /// <param name="p">Plane to test.</param>
        /// <param name="v">Vector to compare.</param>
        /// <returns>Plane axis closest to vector.</returns>
        public static Axis GetClosestXYZAxis(Plane p, Vector3d v, out Vector3d axis)
        {
            List<double> axes = new List<double>(3);
            axes.Add(Vector3d.Multiply(v, p.XAxis));
            axes.Add(Vector3d.Multiply(v, p.YAxis));
            axes.Add(Vector3d.Multiply(v, p.ZAxis));

            int i = -1;
            i = Math.Abs(axes[1]) > Math.Abs(axes[0]) ? 1 : 0;
            i = Math.Abs(axes[2]) > Math.Abs(axes[i]) ? 2 : i;

            int sign = Math.Sign(axes[i]);

            switch (i)
            {
                case (0):
                    axis = p.XAxis * sign;
                    if (sign > 0)
                        return Axis.XPos;
                    return Axis.XNeg;
                case (1):
                    axis = p.YAxis * sign;
                    if (sign > 0)
                        return Axis.YPos;
                    return Axis.YNeg;
                case (2):
                    axis = p.ZAxis * sign;
                    if (sign > 0)
                        return Axis.ZPos;
                    return Axis.ZNeg;
                default:
                    axis = Vector3d.Unset;
                    return Axis.Undefined;
            }
        }

        public static void GetOverlappingDomain(Curve c1, Curve c2, double maxDist, out Interval int1, out Interval int2, int N = 100, double extension = 0.0)
        {
            double[] tt1 = c1.DivideByCount(N - 1, true);
            double[] tt2 = c2.DivideByCount(N - 1, true);

            double t;
            double d;
            Point3d p1, p2;

            double t1Min = tt1[0];
            double t2Min = tt2[0];
            double t1Max = tt1[0];
            double t2Max = tt2[0];

            bool isIn = false;
            for (int i = 0; i < N; ++i)
            {
                p1 = c1.PointAt(tt1[i]);
                c2.ClosestPoint(p1, out t);
                p2 = c2.PointAt(t);

                d = p1.DistanceTo(p2);
                if (d < maxDist && !isIn)
                {
                    t1Min = tt1[i];
                    isIn = true;
                }
                else if (d > maxDist && isIn)
                {
                    t1Max = tt1[i];
                    isIn = false;
                    break;
                }
                else if (isIn)
                {
                    t1Max = tt1[i];
                }
            }
            isIn = false;

            int1 = new Interval(t1Min, t1Max);

            for (int i = 0; i < N; ++i)
            {
                p1 = c2.PointAt(tt2[i]);
                c1.ClosestPoint(p1, out t);
                p2 = c1.PointAt(t);

                d = p1.DistanceTo(p2);
                if (d < maxDist && !isIn)
                {
                    t2Min = tt2[i];
                    isIn = true;
                }
                else if (d > maxDist && isIn)
                {
                    t2Max = tt2[i];
                    isIn = false;
                    break;
                }
                else if (isIn)
                {
                    t2Max = tt2[i];
                }
            }

            int2 = new Interval(t2Min - extension, t2Max + extension);
        }


        public static double ScaleFromMeter(double MeterUnit = 1.0)
        {
            switch (Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem)
            {
                case (Rhino.UnitSystem.Meters):
                    return MeterUnit * 1.0;
                case (Rhino.UnitSystem.Centimeters):
                    return MeterUnit * 100.0;
                case (Rhino.UnitSystem.Millimeters):
                    return MeterUnit * 1000.0;
                case (Rhino.UnitSystem.Inches):
                    return MeterUnit * 39.3701;
                case (Rhino.UnitSystem.Feet):
                    return MeterUnit * 3.28084;
                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Attempts to align two sets of keypoints in a RANSAC-like fashion.
        /// </summary>
        /// <param name="source">Source points (from).</param>
        /// <param name="target">Target points (to).</param>
        /// <param name="NumIterations">Number of iterations to run.</param>
        /// <param name="MaxDist">Maximum distance between correspondences.</param>
        /// <returns>Transform from source to target.</returns>
        public static Transform DirtyRANSAC(List<Point3d> source, List<Point3d> target, int NumIterations, double MaxDist = double.MaxValue)
        {
            if (source.Count < 3 || target.Count < 3) return Transform.Identity;

            int N = NumIterations;
            double MaxDistSq = MaxDist * MaxDist;

            Transform best = Transform.Identity;

            double global_fitness = double.MaxValue;
            System.Random rand = new System.Random();

            for (int i = 0; i < N; ++i)
            {
                double round_fitness = 0;

                var si = Enumerable.Range(0, source.Count).OrderBy(g => rand.NextDouble()).Take(3).ToArray();
                var ti = Enumerable.Range(0, target.Count).OrderBy(g => rand.NextDouble()).Take(3).ToArray();

                if (IsCollinear(source[si[0]], source[si[1]], source[si[2]]))
                {
                    i--;
                    continue;
                }
                if (IsCollinear(target[ti[0]], target[ti[1]], target[ti[2]]))
                {
                    i--;
                    continue;
                }

                Plane src = new Plane(source[si[0]], source[si[1]], source[si[2]]);
                Plane tar = new Plane(target[ti[0]], target[ti[1]], target[ti[2]]);
                Transform p2p = Transform.PlaneToPlane(src, tar);

                for (int j = 0; j < source.Count; ++j)
                {
                    Point3d temp = source[j];
                    temp.Transform(p2p);

                    double closest = double.MaxValue;
                    for (int k = 0; k < target.Count; ++k)
                    {
                        closest = Math.Min(closest, Distance(temp, target[k]));
                    }
                    round_fitness += (closest > MaxDist) ? closest * 2.0 : closest;
                }

                if (round_fitness < global_fitness)
                {
                    best = p2p;
                    global_fitness = round_fitness;
                }
            }
            return best;
        }

        public static bool GetXMLTargets(string Path, ref List<Point3d> Targets, ref List<Point3d> Other)
        {
            using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(Path))
            {
                XDocument doc = XDocument.Load(reader);

                XElement metascan = doc.Element("metascan");
                XElement targets_element = metascan.Element("targets");
                if (targets_element == null) return false;

                List<XElement> targets = targets_element.Elements("target").ToList();
                string[] tok;
                double x, y, z;
                foreach (XElement elem in targets)
                {
                    tok = elem.Value.Split(' ');

                    x = Convert.ToDouble(tok[0]);
                    y = Convert.ToDouble(tok[1]);
                    z = Convert.ToDouble(tok[2]);

                    Targets.Add(new Point3d(x, y, z));
                }

                targets = targets_element.Elements("other").ToList();

                foreach (XElement elem in targets)
                {
                    tok = elem.Value.Split(' ');

                    x = Convert.ToDouble(tok[0]);
                    y = Convert.ToDouble(tok[1]);
                    z = Convert.ToDouble(tok[2]);

                    Other.Add(new Point3d(x, y, z));
                }
            }
            return true;
        }

        /// <summary>
        /// Modulus which works with negative numbers.
        /// </summary>
        /// <param name="x">Input value.</param>
        /// <param name="m">Domain value.</param>
        /// <returns></returns>
        public static int Modulus(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Extrudes naked edges of mesh downwards onto the input plane and creates a filled polygon on the open face. 
        /// Crude way of closing open meshes (i.e. surfaces).
        /// </summary>
        /// <param name="M">Open mesh to seal.</param>
        /// <param name="P">Plane to project naked edges onto.</param>
        /// <returns>Sealed mesh.</returns>
        public static Mesh SealMeshWithExtrusion(Mesh M, Plane P)
        {

            Polyline[] NakedEdges = M.GetNakedEdges();
            if (NakedEdges == null)
                return M;

            foreach (Polyline p in NakedEdges)
            {
                Polyline np = new Polyline();
                foreach (Point3d pt in p)
                {
                    np.Add(ProjectToPlane(pt, P));
                }

                Mesh NewMesh = new Mesh();
                int N = p.Count;

                NewMesh.Vertices.AddVertices(np);
                NewMesh.Vertices.AddVertices(p);

                for (int i = 0; i < N - 1; ++i)
                {
                    NewMesh.Faces.AddFace(i, i + 1, N + i + 1, N + i);
                }
                NewMesh.Faces.AddFace(N - 1, N, N + N, N + N - 1);

                MeshFace[] mf = np.TriangulateClosedPolyline();
                NewMesh.Faces.AddFaces(mf);

                M.Append(NewMesh);

                M.Weld(Math.PI);
                M.UnifyNormals();
                M.Normals.ComputeNormals();

                return M;
            }

            return M;
        }

        /// <summary>
        /// Sorts vectors around a point. Uses the cross product of the first 2 vectors as a normal axis to sort around.
        /// </summary>
        /// <param name="V">Input vectors.</param>
        /// <param name="p">Point to sort around.</param>
        /// <param name="I">(out) List of vector indices in order.</param>
        /// <returns>Sorted angles of vectors.</returns>
        public static List<double> SortVectorsAroundPoint(List<Vector3d> V, Point3d p, out List<int> I)
        {
            Vector3d n = Vector3d.CrossProduct(V[0], V[1]);

            Plane plane = new Plane(p, n);

            List<Tuple<double, int>> angles = new List<Tuple<double, int>>();

            I = new List<int>();
            List<double> D = new List<double>();

            for (int i = 0; i < V.Count; ++i)
            {
                double dx = Vector3d.Multiply(V[i], plane.XAxis);
                double dy = Vector3d.Multiply(V[i], plane.YAxis);

                angles.Add(new Tuple<double, int>(Math.Atan2(dy, dx), i));
            }

            angles.Sort();

            foreach (Tuple<double, int> t in angles)
            {
                I.Add(t.Item2);
                D.Add(t.Item1);
            }

            return D;
        }

        /// <summary>
        /// Transforms 1st-rank tensor from coordinate system CS1 to CS2.
        /// </summary>
        /// <param name="v">Input vector in coordinate system CS1.</param>
        /// <param name="CS1">Original coordinate system (3 vectors).</param>
        /// <param name="CS2">Transformed coordinate system (3 vectors).</param>
        /// <returns></returns>
        public static Vector3d TensorTransformation(Vector3d v, Vector3d[] CS1, Vector3d[] CS2)
        {
            if (CS1.Length != 3 || CS2.Length != 3) return v;

            Matrix angles = new Matrix(3, 3);

            angles[0, 0] = CS1[0] * CS2[0];
            angles[0, 1] = CS1[1] * CS2[0];
            angles[0, 2] = CS1[2] * CS2[0];

            angles[1, 0] = CS1[0] * CS2[1];
            angles[1, 1] = CS1[1] * CS2[1];
            angles[1, 2] = CS1[2] * CS2[1];

            angles[2, 0] = CS1[0] * CS2[2];
            angles[2, 1] = CS1[1] * CS2[2];
            angles[2, 2] = CS1[2] * CS2[2];

            Vector3d vv = new Vector3d();

            vv.X = v.X * angles[0, 0] + v.Y * angles[0, 1] + v.Z * angles[0, 2];
            vv.Y = v.X * angles[1, 0] + v.Y * angles[1, 1] + v.Z * angles[1, 2];
            vv.Z = v.X * angles[2, 0] + v.Y * angles[2, 1] + v.Z * angles[2, 2];

            return vv;
        }

        public static Point3d PlaneLineIntersection(Line l, Plane p)
        {
            double u = (p.Normal * (p.Origin - l.From)) / (p.Normal * (l.To - l.From));
            return (l.From + l.Direction * u);
        }

        /// <summary>
        /// Calculate the area of a triangle defined by three points.
        /// </summary>
        /// <param name="a">Point A</param>
        /// <param name="b">Point B</param>
        /// <param name="c">Point C</param>
        /// <returns>Area of triangle.</returns>
        public static double TriangleArea(Point3d a, Point3d b, Point3d c)
        {
            double ba = b.DistanceTo(a);
            double bc = b.DistanceTo(c);

            double angle = Vector3d.VectorAngle(a - b, c - b);

            return 0.5 * ba * bc * Math.Sin(angle);
        }

        /// <summary>
        /// Calculate unit normal of triangle defined by three points.
        /// </summary>
        /// <param name="a">Point A</param>
        /// <param name="b">Point B</param>
        /// <param name="c">Point C</param>
        /// <returns>Unit normal of triangle.</returns>
        public static Vector3d TriangleNormal(Point3d a, Point3d b, Point3d c)
        {
            Vector3d n = Vector3d.CrossProduct(b - a, c - a);
            n.Unitize();
            return n;
        }

        /// <summary>
        /// Create point on triangle defined by three points, at coordinates a and b.
        /// </summary>
        /// <param name="A">Point A</param>
        /// <param name="B">Point B</param>
        /// <param name="C">Point C</param>
        /// <param name="a">Parameter a</param>
        /// <param name="b">Parameter b</param>
        /// <returns>Point on triangle at (a , b).</returns>
        public static Point3d PointOnTriangle(Point3d A, Point3d B, Point3d C, double a, double b)
        {
            double c = 0;

            if (a + b > 1)
            {
                a = 1 - a;
                b = 1 - b;
            }
            c = 1 - a - b;

            return new Point3d(
                (a * A.X) + (b * B.X) + (c * C.X),
                (a * A.Y) + (b * B.Y) + (c * C.Y),
                (a * A.Z) + (b * B.Z) + (c * C.Z));
        }

        /// <summary>
        /// Generate N random integers. Courtsey of http://codereview.stackexchange.com/a/61372
        /// </summary>
        /// <param name="count">Number of random integers to generate.</param>
        /// <param name="lower">Lower bound of random range.</param>
        /// <param name="upper">Upper bound of random range.</param>
        /// <returns>List of random integers.</returns>
        public static List<int> GenerateRandom(int count, int lower = 0, int upper = int.MaxValue)
        {
            // generate count random values.
            HashSet<int> candidates = new HashSet<int>();
            while (candidates.Count < count)
            {
                // May strike a duplicate.
                candidates.Add(random.Next(lower, upper));
            }

            // load them in to a list.
            List<int> result = new List<int>();
            result.AddRange(candidates);

            // shuffle the results:
            int i = result.Count;
            while (i > 1)
            {
                i--;
                int k = random.Next(i + 1);
                int value = result[k];
                result[k] = result[i];
                result[i] = value;
            }
            return result;
        }

        /// <summary>
        /// Create frames that are aligned with a Brep. The input curve does not
        /// necessarily have to lie on the Brep.
        /// </summary>
        /// <param name="curve">Input centreline of the glulam.</param>
        /// <param name="brep">Brep to align the glulam orientation to.</param>
        /// <param name="num_samples">Number of orientation frames to use for alignment.</param>
        /// <returns>New Glulam oriented to the brep.</returns>
        public static Plane[] FramesNormalToSurface(Curve curve, Brep brep, int num_samples = 20)
        {
            num_samples = Math.Max(num_samples, 2);
            double[] t = curve.DivideByCount(num_samples - 1, true);
            Plane[] planes = new Plane[num_samples];
            Vector3d xaxis, yaxis, zaxis;
            Point3d pt;
            ComponentIndex ci;

            for (int i = 0; i < t.Length; ++i)
            {
                brep.ClosestPoint(curve.PointAt(t[i]), out pt, out ci, out double u, out double v, 0, out yaxis);

                // ripped from: https://discourse.mcneel.com/t/brep-closestpoint-normal-is-not-normal/15147/8
                // if the closest point is found on an edge, average the face normals
                if (ci.ComponentIndexType == ComponentIndexType.BrepEdge)
                {
                    BrepEdge edge = brep.Edges[ci.Index];
                    int[] faces = edge.AdjacentFaces();
                    yaxis = Vector3d.Zero;
                    for (int j = 0; j < faces.Length; ++j)
                    {
                        BrepFace bf = edge.Brep.Faces[j];
                        if (bf.ClosestPoint(pt, out u, out v))
                        {
                            Vector3d faceNormal = bf.NormalAt(u, v);
                            yaxis += faceNormal;
                        }
                    }
                    yaxis.Unitize();
                }

                zaxis = curve.TangentAt(t[i]);
                xaxis = Vector3d.CrossProduct(zaxis, yaxis);
                planes[i] = new Plane(pt, xaxis, yaxis);
            }

            return planes;
        }

        #region Carve booleans

        /// <summary>
        /// Make boolean difference between two meshes using the CarveSharp library.
        /// </summary>
        /// <param name="MeshA">Mesh to subtract from.</param>
        /// <param name="MeshB">Mesh to subtract.</param>
        /// <returns>Mesh difference between MeshA and MeshB.</returns>
        public static Mesh Carve(Mesh MeshA, Mesh MeshB, CarveSharp.CarveSharp.CSGOperations Operation)
        {
            if (MeshA == null || MeshB == null) return null;
            MeshA.Weld(3.14);
            MeshB.Weld(3.14);

            //MeshA.Faces.ConvertQuadsToTriangles();
            //MeshB.Faces.ConvertQuadsToTriangles();

            var cmA = MeshA.MeshToCarve();
            var cmB = MeshB.MeshToCarve();

            var tmp = CarveSharp.CarveSharp.PerformCSG(cmA, cmB, Operation);

            return tmp.CarveToMesh();
        }

        /// <summary>
        /// Make boolean difference between a mesh and a 
        /// collection of meshes using the CarveSharp library.
        /// </summary>
        /// <param name="MeshA">Mesh to subtract from.</param>
        /// <param name="Meshes">Meshes to subtract.</param>
        /// <returns>Mesh difference.</returns>
        public static Mesh Carve(Mesh MeshA, IEnumerable<Mesh> Meshes, CarveSharp.CarveSharp.CSGOperations Operation)
        {
            MeshA.Weld(3.14);
            MeshA.Faces.ConvertQuadsToTriangles();
            var tmp = MeshA.MeshToCarve();

            foreach (Mesh m in Meshes)
            {
                if (m == null) continue;
                m.Weld(3.14);
                m.Faces.ConvertQuadsToTriangles();
                var mm = m.MeshToCarve();
                tmp = CarveSharp.CarveSharp.PerformCSG(tmp, mm, Operation);
            }

            return tmp.CarveToMesh();
        }

        /// <summary>
        /// Make boolean difference between two collections 
        /// of meshes using the CarveSharp library.
        /// </summary>
        /// <param name="MeshesA">Meshes A.</param>
        /// <param name="MeshesB">Meshes B.</param>
        /// <param name="Operation">Carve operation to perform.</param>
        /// <returns>Mesh difference.</returns>
        public static List<Mesh> Carve(IEnumerable<Mesh> MeshesA, IEnumerable<Mesh> MeshesB, CarveSharp.CarveSharp.CSGOperations Operation)
        {
            List<Mesh> OutMeshes = new List<Mesh>();
            foreach (Mesh mA in MeshesA)
            {
                if (mA == null) continue;
                mA.Weld(3.14);
                //mA.Faces.ConvertQuadsToTriangles();
                CarveMesh temp = mA.MeshToCarve();

                foreach (Mesh m in MeshesB)
                {
                    if (m == null) continue;
                    m.Weld(3.14);
                    //m.Faces.ConvertQuadsToTriangles();
                    var cm = m.MeshToCarve();
                    temp = CarveSharp.CarveSharp.PerformCSG(temp, cm, Operation);
                }

                if (temp != null)
                    OutMeshes.Add(temp.CarveToMesh());
            }
            return OutMeshes;
        }

        #endregion
    }

    public class Gradient
    {
        List<double> Stops;
        List<Color> Colors;

        public Gradient(List<double> stops, List<Color> colors)
        {
            Stops = stops;
            Colors = colors;
        }

        public Color GetColor(double t)
        {
            int index = Stops.BinarySearch(t);
            if (index >= 0) return Colors[index];

            index = ~index;

            double tt = (t - Stops[index - 1]) / (Stops[index] - Stops[index - 1]);
            return Util.Interpolation.Lerp(Colors[index - 1], Colors[index], tt);
        }
    }

    /// <summary>
    /// Courtesy of David Rutten
    /// https://discourse.mcneel.com/t/converting-curves-into-multiple-arcs/20418/4?u=tom_svilans
    /// </summary>
    public static class BiArc
    {
        /// <summary>
        /// Converts a factor (0.0 ~ 1.0) into a bi-arc ratio value (0.01 ~ 100.0)
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        private static double ToRatio(double factor)
        {
            factor = Math.Max(factor, 0.0);
            factor = Math.Min(factor, 1.0);

            //remap to a {-3.0 ~ +3.0} domain. (0.001 ~ 1000.0)
            factor = -3.0 + factor * 6.0;

            return Math.Pow(10.0, factor);
        }

        /// <summary>
        /// Joins A and B into a polycurve.
        /// Segment B will be reversed.
        /// </summary>
        private static PolyCurve JoinArcs(Arc A, Arc B)
        {
            B.Reverse();

            PolyCurve crv = new PolyCurve();
            crv.Append(A);
            crv.Append(B);

            return crv;
        }

        /// <summary>
        /// Solves a BiArc for a specific set of end points and tangents.
        /// If a solution is found that is simpler than a BiArc (i.e. A single Arc or a straight line segment)
        /// then the solution out parameter will contain that solution and the segments will be null.
        /// </summary>
        /// <param name="pA">Start point of biarc</param>
        /// <param name="tA">Start tangent of biarc.</param>
        /// <param name="pB">End point of biarc</param>
        /// <param name="tB">End tangent of biarc.</param>
        /// <param name="segment_A">First biarc segment</param>
        /// <param name="segment_B">Second biarc segment</param>
        /// <param name="solution">Curve representing entire solution.</param>
        /// <returns>true on success, false on failure</returns>
        public static bool Solve(
          Point3d pA, Vector3d tA,
          Point3d pB, Vector3d tB,
          out Arc segment_A,
          out Arc segment_B,
          out Curve solution)
        {
            return Solve(pA, tA, pB, tB, 0.5, out segment_A, out segment_B, out solution);
        }

        /// <summary>
        /// Solves a BiArc for a specific set of end points and tangents.
        /// If a solution is found that is simpler than a BiArc (i.e. A single Arc or a straight line segment)
        /// then the solution out parameter will contain that solution and the segments will be null.
        /// </summary>
        /// <param name="pA">Start point of biarc</param>
        /// <param name="tA">Start tangent of biarc.</param>
        /// <param name="pB">End point of biarc</param>
        /// <param name="tB">End tangent of biarc.</param>
        /// <param name="factor">Factor (between zero and one) that defines the relative weight of the start and end points.</param>
        /// <param name="segment_A">First biarc segment</param>
        /// <param name="segment_B">Second biarc segment</param>
        /// <param name="solution">Curve representing entire solution.</param>
        /// <returns>true on success, false on failure</returns>
        public static bool Solve(
          Point3d pA, Vector3d tA,
          Point3d pB, Vector3d tB,
          double factor,
          out Arc segment_A,
          out Arc segment_B,
          out Curve solution)
        {
            segment_A = Arc.Unset;
            segment_B = Arc.Unset;
            solution = null;

            if ((!pA.IsValid) || (!tA.IsValid) || (!pB.IsValid) || (!tB.IsValid)) { return false; }
            if (tA.IsTiny(1e-12) || tB.IsTiny(1e-12)) { return false; }

            tA.Unitize();
            tB.Unitize();

            Vector3d span = (pA - pB);
            if (span.IsTiny(1e-12)) { return false; }

            //================================================================
            //From here on out it should be possible to always get a solution.
            //================================================================

            double ratio = ToRatio(factor);

            // If the start and end tangent are parallel, we have a simple solution.
            if (tA.IsParallelTo(tB, 1e-12) == +1)
            {
                if (tA.IsParallelTo(span, 1e-12) == -1)
                {
                    // If the span is also parallel to tA, we have a straight line segment
                    solution = new LineCurve(pA, pB);
                    return true;
                }
                else
                {
                    // We have two symmetrical arcs. The regular algorithm cannot handle (anti)parallel tangents,
                    // so we need to handle these separately.
                    segment_A = new Arc(pA, tA, pA + span * 0.5);
                    segment_B = new Arc(pB, -tB, pB + span * 0.5);
                    solution = JoinArcs(segment_A, segment_B);
                    return true;
                }
            }
            // Handle anti-parallel tangents here.


            // Attempt single arc solution.
            Arc simple_solution = new Arc(pA, tA, pB);
            if (!simple_solution.IsValid)
            {
                if (simple_solution.TangentAt(simple_solution.AngleDomain.Max).IsParallelTo(tB, 1e-12) == +1)
                {
                    segment_A = simple_solution;
                    segment_B = simple_solution;

                    segment_A.Trim(simple_solution.AngleDomain.ParameterIntervalAt(new Interval(0.0, factor)));
                    segment_B.Trim(simple_solution.AngleDomain.ParameterIntervalAt(new Interval(factor, 1.0)));

                    solution = new ArcCurve(simple_solution);
                    return true;
                }

                // If the end tangent of the simple solution doesn't match the provided end tangent,
                // we have to fit a real bi-arc.
            }


            // Hard core bi-arc solver. Code based on cmdMikko.cpp in Rhino source.
            {
                double a = ratio * 2.0 * (tA * tB - 1.0);
                if (Math.Abs(a) < Rhino.RhinoMath.ZeroTolerance) { return false; }

                double b = span * 2.0 * (tA * ratio + tB);
                double c = span * span;
                double d = (b * b) - (4.0 * a * c);

                if (d < 0.0) { return false; }

                int lmax = 0;
                if (d > 0.0)
                {
                    d = Math.Sqrt(d);
                    lmax = 2;
                }

                double denom = 1.0 / (2.0 * a);

                for (int l = -1; l < lmax; l += 2)
                {
                    double sol = (-b + l * d) * denom;
                    if (sol > 0.0)
                    {
                        Line ln = new Line(pA + tA * sol * ratio, pB - tB * sol);
                        Point3d pt = ln.PointAt((ratio * sol) / (sol + ratio * sol));

                        if ((pt.DistanceTo(pA) > Rhino.RhinoMath.ZeroTolerance) &&
                          (pt.DistanceTo(pB) > Rhino.RhinoMath.ZeroTolerance))
                        {
                            segment_A = new Arc(pA, tA, pt);
                            if (!segment_A.IsValid) { return false; }

                            segment_B = new Arc(pB, -tB, pt);
                            if (!segment_B.IsValid) { return false; }

                            solution = JoinArcs(segment_A, segment_B);
                            return true;
                        }
                    }
                }
            }

            return (solution != null);
        }
    }




}