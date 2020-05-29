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
    public abstract partial class Glulam
    {

        public Plane[] MapToGlulamSpace(IList<Plane> planes)
        {
            Plane[] m_output_planes = new Plane[planes.Count];

            Plane m_plane;
            Plane m_temp;
            double t;

            for (int i = 0; i < planes.Count; ++i)
            {
                Centreline.ClosestPoint(planes[i].Origin, out t);
                m_plane = GetPlane(t);
                m_temp = planes[i];
                m_temp.Transform(Rhino.Geometry.Transform.PlaneToPlane(m_plane, Plane.WorldXY));
                m_temp.OriginZ = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));

                m_output_planes[i] = m_temp;
            }

            return m_output_planes;
        }

        public Plane MapToGlulamSpace(Plane plane)
        {
            Centreline.ClosestPoint(plane.Origin, out double t);
            Plane m_plane = GetPlane(t);
            plane.Transform(Rhino.Geometry.Transform.PlaneToPlane(m_plane, Plane.WorldXY));
            plane.OriginZ = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));

            return plane;
        }

        public Point3d MapToGlulamSpace(Point3d pt)
        {
            Centreline.ClosestPoint(pt, out double t);
            Plane m_plane = GetPlane(t);
            pt.Transform(Rhino.Geometry.Transform.PlaneToPlane(m_plane, Plane.WorldXY));
            pt.Z = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));

            return pt;
        }

        public Point3d[] MapToGlulamSpace(IList<Point3d> pts)
        {
            Point3d[] m_output_pts = new Point3d[pts.Count];

            Plane m_plane;
            Point3d m_temp;
            double t;
            for (int i = 0; i < pts.Count; ++i)
            {
                Centreline.ClosestPoint(pts[i], out t);
                m_plane = GetPlane(t);
                m_plane.RemapToPlaneSpace(pts[i], out m_temp);
                m_temp.Z = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));

                m_output_pts[i] = m_temp;
            }

            return m_output_pts;
        }

        public Mesh MapToGlulamSpace(Mesh mesh)
        {
            //Mesh m_mesh = new Mesh();
            Mesh m_mesh = mesh.DuplicateMesh();
            m_mesh.Vertices.Clear();
            m_mesh.Vertices.AddVertices(MapToGlulamSpace(mesh.Vertices.ToPoint3dArray()));

            return m_mesh;
        }

        public List<Point3d> DiscretizeCentreline(bool adaptive = true)
        {
            if (adaptive)
            {
                var pCurve = Centreline.ToPolyline(Glulam.Tolerance, Glulam.AngleTolerance, 0.0, 0.0);
                return pCurve.ToPolyline().ToList();
            }

            var tt = Centreline.DivideByCount(Data.Samples, true);
            return tt.Select(x => Centreline.PointAt(x)).ToList();
        }

        public virtual Mesh GetBoundingMesh(double offset = 0.0, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            return new Mesh();
        }

        public virtual Brep GetBoundingBrep(double offset = 0.0)
        {
            return new Brep();
        }

        public virtual List<Curve> GetLamellaCurves()
        {
            return new List<Curve>();
        }

        public virtual List<Mesh> GetLamellaMeshes()
        {
            return new List<Mesh>();
        }

        public virtual List<Brep> GetLamellaBreps()
        {
            return new List<Brep>();
        }

        public abstract void GenerateCrossSectionPlanes(ref int N, out Plane[] planes, out double[] t, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR);


    }
}