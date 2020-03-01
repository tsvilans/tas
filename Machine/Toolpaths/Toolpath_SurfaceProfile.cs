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

using tas.Core;
using tas.Core.Types;

using Rhino.Geometry;
using tas.Core.Util;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_SurfaceProfile : ToolpathStrategy
    {
        // reverse curve
        public bool StartEnd = false;

        // 0 - tool centered on path, -1 and 1 to left or right
        public int ToolOffset = 0;

        public Surface DriveSurface;
        public List<Polyline> DriveCurves;
        List<PPolyline> Paths;

        public Toolpath_SurfaceProfile(IEnumerable<Curve> c, Surface surface, double tolerance)
        {
            Workplane = Plane.WorldXY;
            Tolerance = tolerance;
            StartEnd = false;
            DriveSurface = surface;
            DriveCurves = Misc.CurvesToPolylines(c.ToList(), Tolerance);
        }

        public Toolpath_SurfaceProfile(Curve c, Surface surface, double tolerance) : this(new List<Curve> { c }, surface, tolerance)
        {
        }

        public Toolpath_SurfaceProfile(IEnumerable<Polyline> c, Surface surface)
        {
            Workplane = Plane.WorldXY;
            StartEnd = false;
            DriveSurface = surface;
            DriveCurves = c.ToList();
        }

        public Toolpath_SurfaceProfile(Polyline c, Surface surface) : this(new List<Polyline> { c }, surface)
        {
        }

        public override void Calculate()
        {
            Paths = new List<PPolyline>();

            foreach (Polyline P in DriveCurves)
            {
                if (P.Count < 2)
                    continue;

                if (StartEnd) P.Reverse();

                PPolyline OP = new PPolyline();
                Vector3d tan, x, t1, t2, nor;
                Plane p;
                Point3d OnSrf;
                double u, v;
                double angle;

                if (!P.IsClosed)
                {

                    #region FirstPlane

                    DriveSurface.ClosestPoint(P[0], out u, out v);
                    OnSrf = DriveSurface.PointAt(u, v);
                    nor = DriveSurface.NormalAt(u, v);

                    tan = new Vector3d(P[1] - P[0]).ProjectToPlane(Workplane);
                    tan.Unitize();
                    x = Vector3d.CrossProduct(tan, nor);
                    p = new Plane(P[0], x, Vector3d.CrossProduct(nor, x));

                    p.Origin = p.Origin + Tool.ToolDiameter / 2 * p.XAxis * ToolOffset;
                    OP.Add(p);
                    #endregion

                    #region MiddlePlanes
                    for (int i = 1; i < P.Count - 1; ++i)
                    {
                        DriveSurface.ClosestPoint(P[i], out u, out v);
                        OnSrf = DriveSurface.PointAt(u, v);
                        nor = DriveSurface.NormalAt(u, v);

                        t1 = new Vector3d(P[i] - P[i + 1]).ProjectToPlane(Workplane);
                        t2 = new Vector3d(P[i] - P[i - 1]).ProjectToPlane(Workplane);

                        t1.Unitize();
                        t2.Unitize();

                        angle = Vector3d.VectorAngle(t1, t2);

                        tan = t2 - t1;
                        tan.Unitize();
                        x = Vector3d.CrossProduct(tan, nor);
                        p = new Plane(OnSrf, x, Vector3d.CrossProduct(nor, x));
                        p.Origin = p.Origin + (Tool.ToolDiameter / 2) / Math.Sin(angle / 2) * p.XAxis * ToolOffset;
                        OP.Add(p);
                    }
                    #endregion

                    #region LastPlane
                    DriveSurface.ClosestPoint(P[P.Count - 1], out u, out v);
                    OnSrf = DriveSurface.PointAt(u, v);
                    nor = DriveSurface.NormalAt(u, v);

                    tan = new Vector3d(P[P.Count - 1] - P[P.Count - 2]).ProjectToPlane(Workplane);
                    tan.Unitize();
                    x = Vector3d.CrossProduct(tan, nor);
                    p = new Plane(OnSrf, x, Vector3d.CrossProduct(nor, x));
                    p.Origin = p.Origin + Tool.ToolDiameter / 2 * p.XAxis * ToolOffset;
                    OP.Add(p);
                    #endregion
                }
                else
                {
                    int N = P.Count - 1;
                    int next, prev;
                    for (int i = 0; i < N; ++i)
                    {
                        DriveSurface.ClosestPoint(P[i], out u, out v);
                        OnSrf = DriveSurface.PointAt(u, v);
                        nor = DriveSurface.NormalAt(u, v);

                        next = (i + 1).Modulus(N);
                        prev = (i - 1).Modulus(N);

                        t1 = new Vector3d(P[i] - P[next]).ProjectToPlane(Workplane);
                        t2 = new Vector3d(P[i] - P[prev]).ProjectToPlane(Workplane);

                        t1.Unitize();
                        t2.Unitize();

                        angle = Vector3d.VectorAngle(t1, t2);

                        tan = t2 - t1;
                        tan.Unitize();
                        x = Vector3d.CrossProduct(tan, nor);
                        p = new Plane(OnSrf, x, Vector3d.CrossProduct(nor, x));
                        p.Origin = p.Origin + (Tool.ToolDiameter / 2) / Math.Sin(angle / 2) * p.XAxis * ToolOffset;
                        OP.Add(p);
                    }

                    OP.Add(OP[0]);
                }

                Paths.Add(OP);
            }
        }

        public override List<PPolyline> GetPaths()
        {
            return this.Paths;
        }
    }
}
