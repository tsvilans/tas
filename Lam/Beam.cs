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
using tas.Core.Util;

namespace tas.Lam
{
    public class Beam : BeamBase
    {
        public double Width = 80, Height = 120;
        public Curve Section;

        public Beam(Curve centreline, Curve section = null, Plane[] planes = null)
        {
            if (planes == null || planes.Length < 1)
            {
                if (centreline.IsPlanar())
                {
                    Plane cPlane;
                    centreline.TryGetPlane(out cPlane);
                    Orientation = new VectorOrientation(cPlane.ZAxis);
                }
                else
                {
                    Orientation = new KCurveOrientation();
                }
            }

            Section = section;

            Centreline = centreline;
        }

        public Beam(Curve centreline, GlulamOrientation orientation, double width, double height)
        {
            Centreline = centreline;
            Orientation = orientation;

            Width = width;
            Height = height;
        }

        public Beam(Curve centreline, Curve section, GlulamOrientation orientation)
        {
            Centreline = centreline;
            Orientation = orientation;

            Section = section;
            BoundingBox bb = section.GetBoundingBox(true);
            Width = bb.Max[0] - bb.Min[0];
            Height = bb.Max[1] - bb.Min[1];
        }

        public Curve CreateOffsetCurve(double x, double y, int samples = 100, bool rebuild = false, int rebuild_pts = 20)
        {
            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(samples, true);


            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                pts.Add(p.Origin + p.XAxis * x + p.YAxis * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }

        public Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, int samples = 100, bool rebuild = false, int rebuild_pts = 20)
        {
            if (!offset_start && !offset_end) return Centreline.DuplicateCurve();
            if (offset_start && offset_end) return CreateOffsetCurve(x, y, samples, rebuild, rebuild_pts);

            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(samples, true);

            double tmin = offset_start ? t.First() : t.Last();
            double tmax = offset_end ? t.Last() : t.First();

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                double l = Ease.QuadOut(Interpolation.Unlerp(tmin, tmax, t[i]));
                pts.Add(p.Origin + p.XAxis * l * x + p.YAxis * l * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);


            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }


    }
}
