﻿/*
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

namespace tas.Lam
{
    public class SingleCurvedGlulam : FreeformGlulam
    {
        public SingleCurvedGlulam(Curve centreline, Plane[] planes, bool with_twist = false) : base()
        {
            if (planes == null)
            {
                Plane p;
                if (centreline.TryGetPlane(out p))
                {
                    double midT = centreline.Domain.Mid;
                    planes = new Plane[] {
                        new Plane(centreline.PointAtStart,
                        Vector3d.CrossProduct(centreline.TangentAtStart, p.ZAxis),
                        p.ZAxis),
                        new Plane(centreline.PointAt(midT),
                        Vector3d.CrossProduct(centreline.TangentAt(midT), p.ZAxis),
                        p.ZAxis),
                        new Plane(centreline.PointAtEnd,
                        Vector3d.CrossProduct(centreline.TangentAtEnd, p.ZAxis),
                        p.ZAxis)};
                }
                else
                {
                    planes = new Plane[] { centreline.GetAlignedPlane(20) };
                }
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

        public override GlulamType Type()
        {
            return GlulamType.SingleCurved;
        }

        public override string ToString()
        {
            return "SingleCurvedGlulam";
        }

        public override bool InKLimitsComponent(out bool width, out bool height)
        {
            width = height = false;
            double[] t = Centreline.DivideByCount(CurvatureSamples, false);
            double max_kw = 0.0, max_kh = 0.0;
            Plane temp;
            Vector3d k;
            for (int i = 0; i < t.Length; ++i)
            {
                temp = GetPlane(t[i]);

                k = Centreline.CurvatureAt(t[i]);
                max_kw = Math.Max(max_kw, Math.Abs(k * temp.XAxis));
                max_kh = Math.Max(max_kh, Math.Abs(k * temp.YAxis));
            }

            double rw = (1 / max_kw) / RadiusMultiplier;
            double rh = (1 / max_kh) / RadiusMultiplier;

            if (rw - Data.LamWidth > -RadiusTolerance || double.IsInfinity(1 / max_kw))
                width = true;
            if (rh - Data.LamHeight > -RadiusTolerance || double.IsInfinity(1 / max_kh))
                height = true;

            return width && height;
        }

        public override Mesh MapToCurveSpace(Mesh m)
        {
            Plane cp, cpp = Plane.Unset;
            if (!Centreline.TryGetPlane(out cp, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
                throw new Exception("SingleCurvedGlulam: Centreline is not planar!");
            double t, l;

            Mesh mesh = new Mesh();

            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                Point3d p = new Point3d(m.Vertices[i]);
                Centreline.ClosestPoint(p, out t);
                l = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));
                Vector3d xaxis = Vector3d.CrossProduct(cp.ZAxis, Centreline.TangentAt(t));
                cpp = new Plane(Centreline.PointAt(t), xaxis, cp.ZAxis);
                p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cpp, Plane.WorldXY));
                p.Z = l;

                mesh.Vertices.Add(p);
            }

            mesh.Faces.AddFaces(m.Faces);
            mesh.FaceNormals.ComputeFaceNormals();

            return mesh;
        }
        /*
        public override Curve CreateOffsetCurve(double x, double y, bool rebuild = false, int rebuild_pts = 20)
        {
            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(this.Data.Samples, true);

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                pts.Add(p.Origin + p.XAxis * x + p.YAxis * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);


            if (new_curve == null)
                throw new Exception("SingleCurvedGlulam::CreateOffsetCurve:: Failed to create interpolated curve!");

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }

        public override Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, bool rebuild = false, int rebuild_pts = 20)
        {
            throw new NotImplementedException();
        }
        */
    }
}
