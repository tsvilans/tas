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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using tas.Core;

namespace tas.Lam
{
    public class Beam
    {
        /*protected*/
        public List<Tuple<double, Plane>> Frames;
        public Curve Centreline { get; protected set; }
        public int Samples = 50;
        public double Width = 80, Height = 120;

        public Beam(Curve centreline, Plane[] planes = null)
        {
            if (planes == null || planes.Length < 1)
            {
                Plane p;
                centreline.PerpendicularFrameAt(centreline.Domain.Min, out p);
                planes = new Plane[] { p };
            }

            Centreline = centreline;
            Frames = new List<Tuple<double, Plane>>();
            double t;

            for (int i = 0; i < planes.Length; ++i)
            {
                Centreline.ClosestPoint(planes[i].Origin, out t);
                Frames.Add(new Tuple<double, Plane>(t, planes[i]));
            }

            SortFrames();
            RecalculateFrames();
        }

        public Beam(Curve centreline, Plane[] planes, double width, double height)
        {
            if (planes == null)
            {
                Plane p;
                centreline.PerpendicularFrameAt(centreline.Domain.Min, out p);
                planes = new Plane[] { p };
            }

            Centreline = centreline;
            Frames = new List<Tuple<double, Plane>>();
            double t;

            for (int i = 0; i < planes.Length; ++i)
            {
                Centreline.ClosestPoint(planes[i].Origin, out t);
                Frames.Add(new Tuple<double, Plane>(t, planes[i]));
            }

            SortFrames();
            RecalculateFrames();
            Width = width;
            Height = height;
        }

        public Plane GetPlane(double t)
        {
            Tuple<Plane, Plane, double> faround = FramesAround(t);
            Plane plane;

            plane = Util.InterpolatePlanes(faround.Item1, faround.Item2, faround.Item3);
            plane.Origin = Centreline.PointAt(t);
            plane.Transform(Rhino.Geometry.Transform.Rotation(plane.ZAxis, Centreline.TangentAt(t), plane.Origin));

            return plane;
        }

        public  Tuple<Plane, Plane, double> FramesAround(double t)
        {
            if (Frames.Count < 1) return null;

            double tt = 0;
            int index = 1;
            int last = Frames.Count - 1;

            if (t <= Frames[0].Item1)
                return new Tuple<Plane, Plane, double>(Frames[0].Item2, Frames[0].Item2, 0.0);
            else if (t > Frames[last].Item1)
            {
                return new Tuple<Plane, Plane, double>(Frames[last].Item2, Frames[last].Item2, 1.0);
            }

            for (int i = 1; i < Frames.Count; ++i)
            {
                if (t > Frames[i].Item1) continue;
                else
                {
                    index = i;
                    break;
                }
            }
            tt = (t - Frames[index - 1].Item1) / (Frames[index].Item1 - Frames[index - 1].Item1);
            return new Tuple<Plane, Plane, double>(Frames[index - 1].Item2, Frames[index].Item2, tt);
        }


        public void SortFrames()
        {
            Frames.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }

        public void RecalculateFrames()
        {
            Point3d pt_on_crv;
            Vector3d tan, xaxis, yaxis;

            for (int i = 0; i < Frames.Count; ++i)
            {
                pt_on_crv = Centreline.PointAt(Frames[i].Item1);
                tan = Centreline.TangentAt(Frames[i].Item1);
                xaxis = Vector3d.CrossProduct(Frames[i].Item2.YAxis, tan);
                yaxis = Vector3d.CrossProduct(tan, xaxis);
                Frames[i] = new Tuple<double, Plane>(Frames[i].Item1, new Plane(pt_on_crv, xaxis, yaxis));
            }
        }

        public Curve CreateOffsetCurve(double x, double y, bool rebuild = false, int rebuild_pts = 20)
        {
            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(Samples, true);


            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                pts.Add(p.Origin + p.XAxis * x + p.YAxis * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);

            if (new_curve == null)
                throw new Exception("FreeformGlulam::CreateOffsetCurve:: Failed to create interpolated curve!");

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }

        public Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, bool rebuild = false, int rebuild_pts = 20)
        {
            if (!offset_start && !offset_end) return Centreline.DuplicateCurve();
            if (offset_start && offset_end) return CreateOffsetCurve(x, y, rebuild, rebuild_pts);

            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(Samples, true);

            double tmin = offset_start ? t.First() : t.Last();
            double tmax = offset_end ? t.Last() : t.First();

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                double l = Util.Ease.QuadOut(Util.Unlerp(tmin, tmax, t[i]));
                pts.Add(p.Origin + p.XAxis * l * x + p.YAxis * l * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);

            if (new_curve == null)
                throw new Exception(this.ToString() + "::CreateOffsetCurve:: Failed to create interpolated curve!");

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }

        public List<Ray3d> FibreDeviation(Glulam blank, out List<double> angles, int divX = 8, int divY = 8, int divZ = 50)
        {
            double stepX = Width / (divX + 1);
            double stepY = Height / (divY + 1);

            List<Ray3d> rays = new List<Ray3d>();
            angles = new List<double>();

            double[] tt = this.Centreline.DivideByCount(divZ, true);
            double t;

            for (int z = 0; z < tt.Length; ++z)
            {
                for (int y = -divY / 2; y <= divY / 2; ++y)
                {
                    for (int x = -divX / 2; x <= divX / 2; ++x)
                    {
                        Plane BePlane = this.GetPlane(tt[z]);
                        Point3d pt = BePlane.Origin + BePlane.YAxis * stepY * y + BePlane.XAxis * stepX * x;

                        blank.Centreline.ClosestPoint(pt, out t);

                        Vector3d tanBl = blank.Centreline.TangentAt(t);
                        Vector3d tanBe = this.Centreline.TangentAt(tt[z]);

                        double angle = Math.Acos(Math.Abs(tanBl * tanBe));

                        rays.Add(new Ray3d(pt, tanBl));
                        angles.Add(angle);
                    }
                }
            }

            return rays;
        }

        public List<Ray3d> FibreDeviation(Glulam blank, int divX = 8, int divY = 8, int divZ = 50)
        {
            List<double> angles;
            return FibreDeviation(blank, out angles, divX, divY, divZ);
        }


    }
}
