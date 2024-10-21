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

using tas.Core.Types;
using System.Drawing;
using Rhino.Display;

namespace tas.Core.Util
{
    public static class IO
    {
        public static Mesh ReadBinarySTL(string filePath)
        {
            return ReadBinarySTL(filePath, out List<string> log);
        }

        public static Mesh ReadBinarySTL(string filePath, out List<string> log)
        {
            var mesh = new Mesh();
            log = new List<string>();

            var buffer = System.IO.File.ReadAllBytes(filePath);

            var header = System.Text.Encoding.UTF8.GetString(buffer, 0, 80);
            log.Add($"{header}");

            var nFacets = System.BitConverter.ToInt32(buffer, 80);
            log.Add($"nFacets {nFacets}");

            var index = 84;

            for (int i = 0; i < nFacets; ++i)
            {
                var j = index + (i * 50);

                var nX = System.BitConverter.ToSingle(buffer, j + 0);
                var nY = System.BitConverter.ToSingle(buffer, j + 4);
                var nZ = System.BitConverter.ToSingle(buffer, j + 8);

                var v0X = System.BitConverter.ToSingle(buffer, j + 12 + 0);
                var v0Y = System.BitConverter.ToSingle(buffer, j + 12 + 4);
                var v0Z = System.BitConverter.ToSingle(buffer, j + 12 + 8);

                var v1X = System.BitConverter.ToSingle(buffer, j + 12 + 12);
                var v1Y = System.BitConverter.ToSingle(buffer, j + 12 + 16);
                var v1Z = System.BitConverter.ToSingle(buffer, j + 12 + 20);

                var v2X = System.BitConverter.ToSingle(buffer, j + 12 + 24);
                var v2Y = System.BitConverter.ToSingle(buffer, j + 12 + 28);
                var v2Z = System.BitConverter.ToSingle(buffer, j + 12 + 32);

                var a = mesh.Vertices.Add(v0X, v0Y, v0Z);
                var b = mesh.Vertices.Add(v1X, v1Y, v1Z);
                var c = mesh.Vertices.Add(v2X, v2Y, v2Z);

                mesh.Faces.AddFace(a, b, c);
                mesh.FaceNormals.AddFaceNormal(nX, nY, nZ);
            }

            log.Add($"Pre-weld:  {mesh.Vertices.Count} vertices, {mesh.Faces.Count} faces");

            mesh.Weld(Math.PI);

            log.Add($"Post-weld: {mesh.Vertices.Count} vertices, {mesh.Faces.Count} faces");

            return mesh;
        }
    }

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
        public static double QuadOut(double t) =>
             -1.0 * t * (t - 2);

        public static double QuadIn(double t) =>
            t * t * t;

        public static double CubicIn(double t) =>
            t * t * t;

        public static double CubicOut(double t)
        {
            t--;
            return t * t * t + 1;
        }
    }

    public static class Intersection
    {
        public static Point3d PlaneLineIntersection(Line l, Plane p)
        {
            double u = (p.Normal * (p.Origin - l.From)) / (p.Normal * (l.To - l.From));
            return (l.From + l.Direction * u);
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
        public static double HermiteInterpolate(double y0, double y1, double y2, double y3, double mu, double tension = 0.0, double bias = 0.0)
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
        public static double Lerp(double y1, double y2, double mu) =>
            y1 + (y2 - y1) * mu;
        //return y1 * (1 - mu) + y2 * mu;

        public static int Lerp(int y1, int y2, double mu) =>
            (int)(y1 + (y2 - y1) * mu);
        /*
        /// <summary>
        /// Simple lerp between two colors.
        /// </summary>
        /// <param name="c1">Color A.</param>
        /// <param name="c2">Color B.</param>
        /// <param name="t">t-value.</param>
        /// <returns>Interpolated color.</returns>
        public static Color Lerp(Color c1, Color c2, double t)
        {
            return Color.FromArgb(
                Lerp(c1.R, c2.R, t),
                Lerp(c1.G, c2.G, t),
                Lerp(c1.B, c2.B, t)
                );
        }
        */
        /// <summary>
        /// Simple linear interpolation between two points.
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Point3d Lerp(Point3d pA, Point3d pB, double t)
        {
            return pA + (pB - pA) * t;
        }

        /// <summary>
        /// Simple linear interpolation between two vectors.
        /// </summary>
        /// <param name="vA"></param>
        /// <param name="vB"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Vector3d Lerp(Vector3d vA, Vector3d vB, double t) =>
            vA + t * (vB - vA);

        public static double Unlerp(double a, double b, double c)
        {
            if (a > b)
                return 1.0 - (c - b) / (a - b);
            return (c - a) / (b - a);
        }

        /// <summary>
        /// from http://paulbourke.net/miscellaneous/interpolation/
        /// </summary>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <param name="mu"></param>
        /// <returns></returns>
        public static double CosineInterpolate(double y1, double y2, double mu) =>
            Lerp(y1, y2, (1 - Math.Cos(mu * Math.PI)) / 2);


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
            if (dot >= 1.0) return v1;

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

            qC.GetRotation(out Plane plane);
            plane.Origin = p;

            return plane;
        }

        public static Plane CalculateFrenetFrame(Curve c, double t)
        {
            Vector3d z = c.TangentAt(t);
            Vector3d y = c.CurvatureAt(t);
            return new Plane(c.PointAt(t), Vector3d.CrossProduct(z, y), z);
        }

        /// <summary>
        /// Calculate RMF for curve. Adapted from https://math.stackexchange.com/a/2847887 and based on
        /// the paper https://dl.acm.org/doi/10.1145/1330511.1330513
        /// </summary>
        /// <param name="c"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        public static List<Plane> CalculateRMF(Curve c, int steps)
        {
            List<Plane> frames = new List<Plane>();
            double c1, c2, step = 1.0 / steps, t0, t1;
            Vector3d v1, v2, riL, tiL, riN, siN;
            Plane x0, x1;

            // n = YAxis
            // r = XAxis
            // t = ZAxis

            // Start off with the standard tangent/axis/normal frame
            // associated with the curve just prior the Bezier interval.
            t0 = -step;
            frames.Add(CalculateFrenetFrame(c, t0));

            // start constructing RM frames
            for (; t0 < 1.0; t0 += step)
            {
                // start with the previous, known frame
                x0 = frames[frames.Count - 1];

                // get the next frame: we're going to throw away its axis and normal
                t1 = t0 + step;
                x1 = CalculateFrenetFrame(c, t1);

                // First we reflect x0's tangent and axis onto x1, through
                // the plane of reflection at the point midway x0--x1
                v1 = x1.Origin - x0.Origin;
                c1 = v1 * v1;
                riL = x0.XAxis - v1 * (2 / c1 * (v1 * x0.XAxis));
                tiL = x0.ZAxis - v1 * (2 / c1 * (v1 * x0.ZAxis));

                // Then we reflection a second time, over a plane at x1
                // so that the frame tangent is aligned with the curve tangent:
                v2 = x1.ZAxis - tiL;
                c2 = v2 * v2;
                riN = riL - v2 * (2 / c2 * (v2 * riL));
                siN = Vector3d.CrossProduct(x1.ZAxis, riN);
                x1.YAxis = siN;
                x1.XAxis = riN;

                // we record that frame, and move on
                frames.Add(x1);
            }

            // and before we return, we throw away the very first frame,
            // because it lies outside the Bezier interval.
            frames.RemoveAt(0);

            return frames;
        }


    }

    public static class Triangle
    {
        /// <summary>
        /// Calculate the area of a triangle defined by three points.
        /// </summary>
        /// <param name="a">Point A</param>
        /// <param name="b">Point B</param>
        /// <param name="c">Point C</param>
        /// <returns>Area of triangle.</returns>
        public static double Area(Point3d a, Point3d b, Point3d c)
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
        public static Vector3d Normal(Point3d a, Point3d b, Point3d c)
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
            if (a + b > 1)
            {
                a = 1 - a;
                b = 1 - b;
            }
            double c = 1 - a - b;

            return new Point3d(
                (a * A.X) + (b * B.X) + (c * C.X),
                (a * A.Y) + (b * B.Y) + (c * C.Y),
                (a * A.Z) + (b * B.Z) + (c * C.Z));
        }

    }

    public static class Mapping
    {
        public static Color4f VectorToColor1(Vector3d v)
        {
            return new Color4f((float)(v.X / 2 + 0.5), (float)(v.Y / 2 + 0.5), (float)(v.Z / 2 + 0.5), 1.0f);
        }

        public static Color4f Contrast(Color4f color, float contrast)
        {
            var red = ((color.R - 0.5f) * contrast) + 0.5f;
            var green = ((color.G - 0.5f) * contrast) + 0.5f;
            var blue = ((color.B - 0.5f) * contrast) + 0.5f;

            red = Math.Min(1.0f, Math.Max(0.0f, red));
            green = Math.Min(1.0f, Math.Max(0.0f, green));
            blue = Math.Min(1.0f, Math.Max(0.0f, blue));

            return Color4f.FromArgb(color.A, red, green, blue);
        }

        public static Color Contrast(Color color, double contrast)
        {
            var red = ((((color.R / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
            var green = ((((color.G / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
            var blue = ((((color.B / 255.0) - 0.5) * contrast) + 0.5) * 255.0;
            if (red > 255) red = 255;
            if (red < 0) red = 0;
            if (green > 255) green = 255;
            if (green < 0) green = 0;
            if (blue > 255) blue = 255;
            if (blue < 0) blue = 0;

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        public static Color4f VectorToColor2(Vector3d v)
        {
            if (v.Z >= 0)
                return new Color4f((float)(v.X / 2 + 0.5), (float)(v.Y / 2 + 0.5), (float)v.Z, 1.0f);
            return new Color4f((float)(-v.X / 2 + 0.5), (float)(-v.Y / 2 + 0.5), (float)-v.Z, 1.0f);
        }

        public static Point3d FromBarycentricCoordinates(Point3d pt, Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            double x, y, z;

            x = (p1.X - p4.X) * pt.X + (p2.X - p4.X) * pt.Y + (p3.X - p4.X) * pt.Z + p4.X;
            y = (p1.Y - p4.Y) * pt.X + (p2.Y - p4.Y) * pt.Y + (p3.Y - p4.Y) * pt.Z + p4.Y;
            z = (p1.Z - p4.Z) * pt.Z + (p2.Z - p4.Z) * pt.Y + (p3.Z - p4.Z) * pt.Z + p4.Z;

            return new Point3d(x, y, z);
        }

        public static Point3d[] FromBarycentricCoordinates(ICollection<Point3d> pt, Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            return pt.Select(x => FromBarycentricCoordinates(x, p1, p2, p3, p4)).ToArray();
        }

        public static Point3d ToBarycentricCoordinates(Point3d pt, Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            return ToBarycentricCoordinates(new Point3d[] { pt }, p1, p2, p3, p4)[0];
        }

        public static Point3d[] ToBarycentricCoordinates(ICollection<Point3d> pt, Point3d p1, Point3d p2, Point3d p3, Point3d p4)
        {
            //Vector3d r1 = p2 - p1;
            //Vector3d r2 = p3 - p1;
            //Vector3d r3 = p4 - p1;

            //double J = Vector3d.CrossProduct(r1, r2) * r3;

            Transform xform = Transform.Identity;
            xform[0, 0] = p1.X - p4.X;
            xform[0, 1] = p2.X - p4.X;
            xform[0, 2] = p3.X - p4.X;

            xform[1, 0] = p1.Y - p4.Y;
            xform[1, 1] = p2.Y - p4.Y;
            xform[1, 2] = p3.Y - p4.Y;

            xform[2, 0] = p1.Z - p4.Z;
            xform[2, 1] = p2.Z - p4.Z;
            xform[2, 2] = p3.Z - p4.Z;


            xform.TryGetInverse(out Transform inverse);

            var output = new Point3d[pt.Count];

            int i = 0;
            foreach (Point3d p in pt)
            {
                output[i] = inverse * new Point3d(p - p4);
                ++i;
            }

            return output;
        }


    }

    public static class Misc
    {

        static readonly Random random = new Random();

        public static Plane PlaneFromNormalAndYAxis(Point3d origin, Vector3d normal, Vector3d yaxis)
        {
            return new Plane(origin, Vector3d.CrossProduct(yaxis, normal), yaxis);
        }

        #region KELOWNA
        public static Point3d[] CreateArcWithArcLength(Point3d pt, Vector3d v1, Vector3d v2, double arc_length, out double offset, out double radius)
        {
            var dot = v1 * v2;

            if (Math.Abs(dot) > 1 - 1e-10)
            {
                offset = arc_length / 2;
                radius = double.PositiveInfinity;
                return new Point3d[] { pt + v1 * (arc_length / 2), pt, pt + v2 * (arc_length / 2) };
            }

            var v3 = v1 + v2; v3.Unitize();

            var theta = Math.Acos(dot);

            radius = arc_length / (Math.PI - theta);

            var h = radius / Math.Sin(theta / 2);
            offset = Math.Sqrt(h * h - radius * radius);

            var interior = pt + v3 * (h - radius);

            var pt1 = pt + v1 * offset;
            var pt2 = pt + v2 * offset;

            return new Point3d[] { pt1, interior, pt2 };
        }

        public static Point3d[] CreateArcWithOffset(Point3d pt, Vector3d v1, Vector3d v2, double offset, out double arc_length, out double radius)
        {
            var dot = v1 * v2;

            if (Math.Abs(dot) > 1 - 1e-10)
            {
                arc_length = offset * 2;
                radius = double.PositiveInfinity;
                return new Point3d[] { pt + v1 * offset, pt, pt + v2 * offset };
            }

            var v3 = v1 + v2; v3.Unitize();

            var theta = Math.Acos(dot);

            var h = offset / Math.Cos(theta / 2);

            radius = Math.Sqrt(h * h - offset * offset);

            var interior = pt + v3 * (h - radius);

            var pt1 = pt + v1 * offset;
            var pt2 = pt + v2 * offset;

            arc_length = radius * (Math.PI - theta);

            return new Point3d[] { pt1, interior, pt2 };
        }

        public static Point3d[] CreateArcWithRadius(Point3d pt, Vector3d v1, Vector3d v2, double radius, out double arc_length, out double offset)
        {
            var dot = v1 * v2;

            if (Math.Abs(dot) > 1 - 1e-10)
            {
                arc_length = 0;
                offset = 0;
                radius = double.PositiveInfinity;
                return new Point3d[] { pt + v1, pt, pt + v2 };
            }

            var v3 = v1 + v2; v3.Unitize();

            var theta = Math.Acos(dot);

            var h = radius / Math.Sin(theta / 2);

            offset = Math.Sqrt(h * h - radius * radius);

            var interior = pt + v3 * (h - radius);

            var pt1 = pt + v1 * offset;
            var pt2 = pt + v2 * offset;

            arc_length = radius * (Math.PI - theta);

            return new Point3d[] { pt1, interior, pt2 };
        }

        public static double CurvatureFrom3Points(Point3d p0, Point3d p1, Point3d p2)
        {
            double a = p0.DistanceTo(p1);
            double b = p1.DistanceTo(p2);
            double c = p2.DistanceTo(p0);

            double s = (a + b + c) / 2;
            double area = Math.Sqrt(s * (s - a) * (s - b) * (s - c));

            return 4 * area / (a * b * c);
        }

        public static bool TryGet<T>(Dictionary<string, object> dict, string key, out T val)
        {
            //if (typeof(T).IsValueType)
            //{
            if (dict.ContainsKey(key))
            {
                val = (T)dict[key];
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
            //}
        }

        /// <summary>
        /// Offset polyline sideways and out-of-plane, guided by a normal vector.
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="normal"></param>
        /// <param name="d"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Polyline OffsetPolyline(Polyline pl, Vector3d normal, double d, double h = 0.0, int open_style = 0)
        {
            if (normal.IsZero)
            {
                Plane.FitPlaneToPoints(pl, out Plane fit);
                normal = fit.ZAxis;

                var poly_direction = Vector3d.CrossProduct(pl[2] - pl[1], pl[0] - pl[1]);

                if (normal * poly_direction < 0) normal.Reverse();
            }

            var newPoly = new Polyline();

            if (pl.Count < 2)
                throw new Exception("Polyline doesn't contain enough points.");

            Vector3d v1, v2, v3;
            double alpha, beta;
            int sign = 1, side = 1;
            Point3d pt;
            int iPrev, iNext;

            int N = pl.Count;

            normal.Unitize();

            bool is_clockwise = pl.IsClockwise(normal);

            if (is_clockwise)
                side = -1;

            if (pl.IsClosed)
            {
                open_style = 0;
                N -= 1;
            }

            // Offset open endpoints trim-like
            switch (open_style)
            {
                case (0):
                    // Handle closed polyline

                    for (int i = 0; i < N; ++i)
                    {
                        iPrev = (i - 1).Modulus(N);
                        iNext = (i + 1).Modulus(N);

                        v1 = pl[iPrev] - pl[i];
                        v2 = pl[iNext] - pl[i];
                        v1.Unitize();
                        v2.Unitize();

                        if (-v1 * v2 > 0.9999999)
                        {
                            v3 = Vector3d.CrossProduct(normal, v1);
                            v3.Unitize();
                            pt = pl[i] + v3 * d * side + normal * h;

                        }
                        else
                        {
                            v3 = Vector3d.CrossProduct(normal, v1);

                            alpha = Math.Acos(v1 * v2) / 2;
                            sign = v2 * v3 > 0 ? 1 : -1;

                            v3 = v1 + v2; v3.Unitize();

                            pt = pl[i] + v3 * (d / Math.Sin(alpha)) * sign * side + normal * h;
                        }

                        if (pt.IsValid)
                            newPoly.Add(pt);
                        else
                            throw new Exception("Bad point.");
                    }

                    if (pl.IsClosed)
                        newPoly.Add(newPoly[0]);
                    break;

                // Offset open endpoints perpendicularly
                case (1):
                    // Handle first point
                    v1 = pl[1] - pl[0]; v1.Unitize();
                    v3 = Vector3d.CrossProduct(normal, v1);

                    newPoly.Add(pl[0] - v3 * side * d + normal * h);

                    // Handle intermediate points
                    if (N > 2)
                    {
                        for (int i = 1; i < N - 1; ++i)
                        {
                            v1 = pl[i - 1] - pl[i]; v1.Unitize();
                            v2 = pl[i + 1] - pl[i]; v2.Unitize();

                            // Co-linear points
                            if (-v1 * v2 > 0.9999999)
                            {
                                v3 = Vector3d.CrossProduct(normal, v1); v3.Unitize();
                                pt = pl[i] + v3 * d * side + normal * h;
                            }
                            else
                            {
                                v3 = Vector3d.CrossProduct(normal, v1);

                                alpha = Math.Acos(v1 * v2) / 2;
                                sign = v2 * v3 > 0 ? 1 : -1;

                                v3 = v1 + v2; v3.Unitize();

                                pt = pl[i] + v3 * (d / Math.Sin(alpha)) * sign * side + normal * h;
                            }

                            if (pt.IsValid)
                                newPoly.Add(pt);
                            else
                                throw new Exception("Bad point.");
                        }
                    }

                    // Handle last point


                    v1 = pl[N - 2] - pl[N - 1]; v1.Unitize();
                    v3 = Vector3d.CrossProduct(normal, v1);

                    newPoly.Add(pl[N - 1] + v3 * side * d + normal * h);
                    break;

                case (2):
                    // Handle first point
                    v1 = pl[1] - pl[0]; v1.Unitize();
                    v2 = pl[N - 1] - pl[0]; v2.Unitize();

                    alpha = Math.Acos(v1 * v2);
                    beta = alpha - Math.PI / 2;

                    newPoly.Add(pl[0] + v2 * (d / Math.Cos(beta)) + normal * h);

                    // Handle intermediate points
                    if (N > 2)
                    {
                        for (int i = 1; i < N - 1; ++i)
                        {
                            v1 = pl[i - 1] - pl[i]; v1.Unitize();
                            v2 = pl[i + 1] - pl[i]; v2.Unitize();

                            // Co-linear points
                            if (-v1 * v2 > 0.9999999)
                            {
                                v3 = Vector3d.CrossProduct(normal, v1); v3.Unitize();
                                pt = pl[i] + v3 * d * side + normal * h;
                            }
                            else
                            {
                                v3 = Vector3d.CrossProduct(normal, v1);

                                alpha = Math.Acos(v1 * v2) / 2;
                                sign = v2 * v3 > 0 ? 1 : -1;

                                v3 = v1 + v2; v3.Unitize();

                                pt = pl[i] + v3 * (d / Math.Sin(alpha)) * sign * side + normal * h;
                            }

                            if (pt.IsValid)
                                newPoly.Add(pt);
                            else
                                throw new Exception("Bad point.");
                        }
                    }

                    // Handle last point
                    v1 = pl[N - 2] - pl[N - 1]; v1.Unitize();
                    v2 = pl[0] - pl[N - 1]; v2.Unitize();

                    alpha = Math.Acos(v1 * v2);
                    beta = alpha - Math.PI / 2;

                    newPoly.Add(pl[N - 1] + v2 * (d / Math.Cos(beta)) + normal * h);
                    break;

                default:
                    break;
            }

            return newPoly;
        }

        /// <summary>
        /// Create a perpendicular plane on curve at parameter t, such that its Y-axis aligns with guide vector v.
        /// </summary>
        /// <param name="c">Curve to evaluate.</param>
        /// <param name="t">Curve parameter to place plane at.</param>
        /// <param name="v">Guide vector for aligning the Y-axis of the plane.</param>
        /// <returns>Plane perpendicular to the curve.</returns>
        public static Plane PlaneAtWithGuide(Curve c, double t, Vector3d v)
        {
            Point3d origin = c.PointAt(t);
            var tan = c.TangentAt(t);
            var xaxis = Vector3d.CrossProduct(tan, v);
            var yaxis = Vector3d.CrossProduct(xaxis, tan);

            return new Plane(origin, xaxis, yaxis);
        }
        #endregion

        /// <summary>
        /// Project point onto plane.
        /// </summary>
        /// <param name="p">Point to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected point.</returns>
        public static Point3d ProjectToPlane(Point3d p, Plane pl)
        {
            return p.ProjectToPlane(pl);

            /*
            Vector3d op = new Vector3d(p - pl.Origin);
            double dot = Vector3d.Multiply(pl.ZAxis, op);
            Vector3d v = pl.ZAxis * dot;
            return new Point3d(p - v);
            */
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
                    if (c.Length > 0)
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
        /// Insets Polyline based on local corners. Height is derived from the cross
        /// product of the two corner vectors. Open Polyline endpoints are linked, so
        /// this is mostly good for insetting polygons / N-gons with the option of 
        /// having them open (Kelowna project).
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="d"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static Polyline InsetPolyline(Polyline pl, Vector3d normal, double d, double h = 0.0)
        {
            var newPoly = new Polyline();

            if (pl.Count < 2)
                throw new Exception("Polyline doesn't contain enough points.");

            Vector3d v1, v2, v3, nor;
            double alpha, beta;
            Point3d pt;

            Plane.FitPlaneToPoints(pl, out Plane fitPlane);
            normal = fitPlane.ZAxis;
            if (normal * Vector3d.ZAxis < 0)
                normal.Reverse();
            normal.Unitize();

            if (pl.IsClosed)
            {
                // Handle closed polyline
                int iPrev, iNext;

                for (int i = 0; i < pl.Count - 1; ++i)
                {
                    iPrev = (i - 1).Modulus(pl.Count - 1);
                    iNext = (i + 1).Modulus(pl.Count - 1);

                    v1 = pl[iPrev] - pl[i];
                    v2 = pl[iNext] - pl[i];
                    v1.Unitize();
                    v2.Unitize();

                    if (Math.Abs(v1 * v2) > 0.9999999)
                    {
                        v3 = Vector3d.CrossProduct(normal, v1);
                        pt = pl[i] + v3 * d + normal * h;

                    }
                    else
                    {
                        nor = Vector3d.CrossProduct(v1, v2);
                        alpha = Math.Acos(v1 * v2) / 2;

                        v3 = v1 + v2;
                        v3.Unitize();
                        pt = pl[i] + v3 * (d / Math.Sin(alpha)) + nor * h;
                    }

                    newPoly.Add(pt);
                }

                newPoly.Add(newPoly[0]);
            }
            else
            {
                // Handle first point
                v1 = pl[1] - pl[0];
                v2 = pl[pl.Count - 1] - pl[0];

                v1.Unitize();
                v2.Unitize();
                nor = Vector3d.CrossProduct(v2, v1);

                alpha = Math.Acos(v1 * v2) / 2;
                beta = alpha - Math.PI / 2;

                newPoly.Add(pl[0] + v2 * (d / Math.Cos(beta)) + nor * h);

                // Handle intermediate points
                if (pl.Count > 2)
                {
                    for (int i = 1; i < pl.Count - 1; ++i)
                    {
                        v1 = pl[i - 1] - pl[i];
                        v2 = pl[i + 1] - pl[i];
                        v1.Unitize();
                        v2.Unitize();

                        if (Math.Abs(v1 * v2) > 0.9999999)
                        {
                            v3 = Vector3d.CrossProduct(normal, v1);
                            newPoly.Add(pl[i] + v3 * d + normal * h);
                        }
                        else
                        {
                            nor = Vector3d.CrossProduct(v1, v2);
                            alpha = Math.Acos(v1 * v2) / 2;

                            v3 = v1 + v2;
                            v3.Unitize();

                            newPoly.Add(pl[i] + v3 * (d / Math.Sin(alpha)) + nor * h);
                        }
                    }
                }

                // Handle last point
                v1 = pl[pl.Count - 2] - pl[pl.Count - 1];
                v2 = pl[0] - pl[pl.Count - 1];
                v1.Unitize();
                v2.Unitize();
                nor = Vector3d.CrossProduct(v1, v2);

                alpha = Math.Acos(v1 * v2) / 2;
                beta = alpha - Math.PI / 2;

                newPoly.Add(pl[pl.Count - 1] + v2 * (d / Math.Cos(beta)) + nor * h);
            }

            return newPoly;
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
            List<Polyline> tpl = new List<Polyline>
            {
                pl
            };

            while (tpl.Count > 0)
            {
                Polyline3D.Offset(tpl, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, d, p,
                  0.01, out List<Polyline> offC, out List<Polyline> offH);
                pls.AddRange(offH);
                tpl = offH;
            }
            return pls;
        }

        public static List<Polyline> InsetUntilNone(List<Polyline> pl, double d, Plane p)
        {
            List<Polyline> pls = new List<Polyline>();
            List<Polyline> tpl = new List<Polyline>();

            for (int i = 0; i < pl.Count; ++i)
            {
                if (pl[i].IsClosed)
                    tpl.Add(pl[i]);
            }

            while (tpl.Count > 0)
            {
                Polyline3D.Offset(tpl, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, d, p,
                  0.01, out _, out List<Polyline> offH);
                pls.AddRange(offH);
                tpl = offH;
            }
            return pls;
        }

        public static void InsetUntilNoneClustersRecursive(Polyline Poly, double Distance, Plane P, ref List<List<Polyline>> Clusters, int Index)
        {
            List<Polyline> OffsetPolylines = new List<Polyline>();

            if (!Poly.IsClosed) return;
            OffsetPolylines.Add(Poly);
            Clusters[Index].Add(Poly);

            while (OffsetPolylines.Count > 0)
            {
                Polyline3D.Offset(OffsetPolylines, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter,
                    Distance, P,
                    Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                    out _, out List<Polyline> Holes);

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

        public static List<Polyline> CurvesToPolylines(IEnumerable<Curve> crvs, double tol, bool reduce = false)
        {
            List<Polyline> polys = new List<Polyline>();

            foreach (Curve c in crvs)
            {
                if (c == null) continue;

                if (c.IsPolyline() && c.TryGetPolyline(out Polyline pl))
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
            pc.TryGetPolyline(out Polyline pl);
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

        public static bool ArePointsCollinear(Point3d p0, Point3d p1, Point3d p2, double tol = 1e-12)
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
                double d = rpts[j].DistanceTo(rpts[ind]); // Distance(rpts[j], rpts[ind]);
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

                if (ArePointsCollinear(source[si[0]], source[si[1]], source[si[2]]))
                {
                    i--;
                    continue;
                }
                if (ArePointsCollinear(target[ti[0]], target[ti[1]], target[ti[2]]))
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
                        closest = Math.Min(closest, temp.DistanceTo(target[k]));
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
                    np.Add(pt.ProjectToPlane(P));
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



        /// <summary>
        /// Generate N random integers. Courtsey of http://codereview.stackexchange.com/a/61372
        /// </summary>
        /// <param name="count">Number of random integers to generate.</param>
        /// <param name="lower">Lower bound of random range.</param>
        /// <param name="upper">Upper bound of random range.</param>
        /// <returns>List of random integers.</returns>
        public static List<int> GenerateRandom(int count, int lower = 0, int upper = int.MaxValue)
        {
            count = Math.Min(upper - lower, count);

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

        public static Brep CutBrep(Brep brep, ICollection<Brep> cutters, double tolerance = 0.0)
        {
            if (tolerance == 0.0)
                tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            //var splits = new Brep[0];
            var splits = Brep.CreateBooleanSplit(new Brep[] { brep }, cutters, tolerance);

            if (splits == null || splits.Length < 1)
                return brep;

            return GetBiggestVolume(splits);
        }

        public static Brep GetBiggestVolume(Brep[] breps)
        {
            if (breps == null || breps.Length < 1) return null;

            int index = 0;
            double volume = 0;
            double temp;

            for (int i = 0; i < breps.Length; ++i)
            {
                if (breps[i] == null) continue;
                temp = breps[i].GetVolume();
                if (temp > volume)
                {
                    index = i;
                    volume = temp;
                }
            }

            return breps[index];
        }
    }

    public class Gradient
    {
        List<double> Stops;
        List<Rhino.Display.Color4f> Colors;

        public Gradient(List<double> stops, List<Rhino.Display.Color4f> colors)
        {
            Stops = stops;
            Colors = colors;
        }

        public Color GetColor(double t)
        {
            int index = Stops.BinarySearch(t);
            if (index >= 0) return Colors[index].AsSystemColor();

            index = ~index;
            if (index == 0) return Colors[0].AsSystemColor();
            if (index > Colors.Count - 1) return Colors.Last().AsSystemColor();

            double tt = (t - Stops[index - 1]) / (Stops[index] - Stops[index - 1]);
            return Colors[index - 1].BlendTo((float)tt, Colors[index]).AsSystemColor();
            /*
            return Color.FromArgb(
                Util.Interpolation.Lerp(Colors[index - 1].R, Colors[index].R, tt),
                Util.Interpolation.Lerp(Colors[index - 1].G, Colors[index].G, tt),
                Util.Interpolation.Lerp(Colors[index - 1].B, Colors[index].B, tt));
            */
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