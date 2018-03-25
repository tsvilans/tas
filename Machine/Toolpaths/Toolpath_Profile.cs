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

using Rhino.Geometry;

using tas.Core;
using tas.Core.Types;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_Profile : ToolpathStrategy
    {
        // reverse curve
        public bool StartEnd = false;

        // 0 - tool centered on path, -1 and 1 to left or right
        public int ToolOffset = 0;

        public List<Polyline> DriveCurves;
        List<PPolyline> Paths;

        public Toolpath_Profile(IEnumerable<Curve> c, double tolerance)
        {
            Workplane = Plane.WorldXY;
            Tolerance = tolerance;
            StartEnd = false;
            DriveCurves = Util.CurvesToPolylines(c.ToList(), Tolerance);
        }

        public Toolpath_Profile(Curve c, double tolerance) : this(new List<Curve> { c }, tolerance)
        {
        }

        public Toolpath_Profile(IEnumerable<Polyline> c)
        {
            Workplane = Plane.WorldXY;
            StartEnd = false;
            DriveCurves = c.ToList();
        }

        public Toolpath_Profile(Polyline c) : this(new List<Polyline> { c })
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
                Vector3d tan, x, t1, t2;
                Plane p;
                double angle;

                if (!P.IsClosed)
                {

                    #region FirstPlane
                    tan = Util.ProjectToPlane(new Vector3d(P[1] - P[0]), Workplane);
                    tan.Unitize();
                    x = Vector3d.CrossProduct(tan, Workplane.ZAxis);
                    p = new Plane(P[0], x, tan);
                    p.Origin = p.Origin + Tool.ToolDiameter / 2 * p.XAxis * ToolOffset;
                    OP.Add(p);
                    #endregion

                    #region MiddlePlanes
                    for (int i = 1; i < P.Count - 1; ++i)
                    {
                        t1 = Util.ProjectToPlane(new Vector3d(P[i] - P[i + 1]), Workplane);
                        t2 = Util.ProjectToPlane(new Vector3d(P[i] - P[i - 1]), Workplane);

                        t1.Unitize();
                        t2.Unitize();

                        angle = Vector3d.VectorAngle(t1, t2);

                        tan = t2 - t1;
                        tan.Unitize();
                        x = Vector3d.CrossProduct(tan, Workplane.ZAxis);
                        p = new Plane(P[i], x, tan);
                        p.Origin = p.Origin + (Tool.ToolDiameter / 2) / Math.Sin(angle / 2) * p.XAxis * ToolOffset;
                        OP.Add(p);
                    }
                    #endregion

                    #region LastPlane
                    tan = Util.ProjectToPlane(new Vector3d(P[P.Count - 1] - P[P.Count - 2]), Workplane);
                    tan.Unitize();
                    x = Vector3d.CrossProduct(tan, Workplane.ZAxis);
                    p = new Plane(P[P.Count - 1], x, tan);
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
                        next = Util.Modulus(i + 1, N);
                        prev = Util.Modulus(i - 1, N);

                        t1 = Util.ProjectToPlane(new Vector3d(P[i] - P[next]), Workplane);
                        t2 = Util.ProjectToPlane(new Vector3d(P[i] - P[prev]), Workplane);

                        t1.Unitize();
                        t2.Unitize();

                        angle = Vector3d.VectorAngle(t1, t2);

                        tan = t2 - t1;
                        tan.Unitize();
                        x = Vector3d.CrossProduct(tan, Workplane.ZAxis);
                        p = new Plane(P[i], x, tan);
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
            return Paths;
        }
    }
}
