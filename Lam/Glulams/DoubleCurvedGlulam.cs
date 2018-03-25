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

namespace tas.Lam
{
    public class DoubleCurvedGlulam : FreeformGlulam
    {
        public DoubleCurvedGlulam(Curve centreline, Plane[] planes) : base()
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
        }

        public override GlulamType Type()
        {
            return GlulamType.DoubleCurved;
        }

        public override string ToString()
        {
            return "DoubleCurvedGlulam";
        }

        public override Mesh MapToCurveSpace(Mesh m)
        {
            Plane cp;
            double t, l;

            Mesh mesh = new Mesh();

            List<Point3d> verts = new List<Point3d>();
            object m_lock = new object();
            Point3d temp1, temp2;

            //Parallel.For(0, m.Vertices.Count, i =>
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                temp1 = m.Vertices[i];
                Centreline.ClosestPoint(temp1, out t);
                l = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));
                cp = GetPlane(t);
                //Centreline.PerpendicularFrameAt(t, out cp);
                //p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cp, Plane.WorldXY));
                cp.RemapToPlaneSpace(temp1, out temp2);
                temp2.Z = l;

                //lock(m_lock)
                //{
                    verts.Add(temp2);
                //}
            }
            //});
            /*
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                Point3d p = new Point3d(m.Vertices[i]);
                Centreline.ClosestPoint(p, out t);
                l = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));
                cp = GetPlane(t);
                //Centreline.PerpendicularFrameAt(t, out cp);
                p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cp, Plane.WorldXY));
                p.Z = l;

            }
            */

            mesh.Vertices.AddVertices(verts);
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
        */
    }

}
