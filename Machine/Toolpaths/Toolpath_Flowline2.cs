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

using System.Collections.Generic;
using System.Linq;

using Rhino.Geometry;

using tas.Core.Util;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_Flowline2 : ToolpathStrategy
    {
        Brep Driver;
        bool Direction;
        public bool OffsetSides;
        public bool StartEnd;
        List<Path> Paths;
        Curve Boundary;

        public Toolpath_Flowline2(Brep surf, bool direction = false, Curve boundary = null)
        {
            Tolerance = 0.5;
            Driver = surf;
            Direction = direction;
            Boundary = boundary;
        }

        public override void Calculate()
        {
            if (Driver == null) return;

            int U = Direction ? 1 : 0;
            int V = Direction ? 0 : 1;

            Paths = new List<Path>();
            Curve IsoFence;

            if (StartEnd)
                IsoFence = Driver.Faces[0].IsoCurve(U, Driver.Faces[0].Domain(V).Min);
            else
                IsoFence = Driver.Faces[0].IsoCurve(U, Driver.Faces[0].Domain(V).Max);

            double[] Uts;
            double length = IsoFence.GetLength();
            double minOffset = IsoFence.Domain.Min;
            double maxOffset = IsoFence.Domain.Max;

            if (OffsetSides)
            {
                IsoFence.LengthParameter(Tool.StepOver, out minOffset);
                IsoFence.LengthParameter(length - Tool.StepOver, out maxOffset);
                IsoFence = IsoFence.Trim(new Interval(minOffset, maxOffset));
            }

            Uts = IsoFence.DivideByLength(Tool.StepOver, true);
            if (Uts == null) return;

            List<double> UtsList = Uts.ToList();

            UtsList.Add(maxOffset);

            /*
            if (Boundary != null)
            {
                Boundary = Driver.Faces[0].Pullback(Boundary, 0.01);
            }
            */

            List<Curve> Isos = new List<Curve>();

            for (int i = 0; i < UtsList.Count; ++i)
            {
                Isos.AddRange(Driver.Faces[0].TrimAwareIsoCurve(V, UtsList[i]));
            }

            for (int i = 0; i < Isos.Count; ++i)
            {
                //Curve Iso = Driver.Faces[0].IsoCurve(V, Uts[i]);
                Curve Iso = Isos[i];

                /*
                if (Boundary != null)
                {
                    Rhino.Geometry.Intersect.CurveIntersections ci = Rhino.Geometry.Intersect.Intersection.CurveCurve(Boundary, Iso, 0.01, 0.01);
                    if (ci.Count == 2)
                    {
                        Iso = Iso.Trim(new Interval(ci[0].ParameterB, ci[1].ParameterB));
                    }
                }
                */

                Polyline Pl = Misc.CurveToPolyline(Iso, Tolerance);

                Path OPl = new Path();
                Vector3d nor, tan, x;

                foreach (Point3d p in Pl)
                {
                    double t, u, v;
                    Point3d cp;
                    ComponentIndex ci;
                    Driver.ClosestPoint(p, out cp, out ci, out u, out v, 0, out nor);

                    if (Vector3d.Multiply(nor, Workplane.ZAxis) < 0.0)
                        nor.Reverse();
                    Iso.ClosestPoint(p, out t);
                    tan = Iso.TangentAt(t);
                    x = -Vector3d.CrossProduct(nor, tan);

                    //Plane plane = new Plane(p, nor);

                    Plane plane = new Plane(p, x, tan);
                    //if (Vector3d.Multiply(plane.ZAxis, Workplane.ZAxis) < 0.0)
                    //    plane = new Plane(p, Vector3d.CrossProduct(nor, tan), tan);
                    OPl.Add(plane);
                }
                Paths.Add(OPl);
            }

            /*
            if (Boundary != null && Boundary.IsClosed)
            {
                Polyline BPoly = Util.CurveToPolyline(Boundary, 0.1);
                Polyline BFPoly = new Polyline();
                Point3d pt1, pt0;
                double u, v;

                for (int i = 0; i < BPoly.Count; ++i)
                {
                    Driver.Faces[0].ClosestPoint(BPoly[i], out u, out v);
                    BFPoly.Add(new Point3d(u, v, 0));
                }

                Curve BFCrv = BFPoly.ToNurbsCurve();
                PointContainment pc;
                bool inside = false;
                bool onsrf = false;

                List<OrientedPolyline> NewPaths = new List<OrientedPolyline>();

                for (int i = 0; i < Paths.Count; ++i)
                {
                    Polyline poly = new Polyline();
                    OrientedPolyline new_op = new OrientedPolyline();

                    // get initial state
                    Driver.Faces[0].ClosestPoint(Paths[i][0].Origin, out u, out v);
                    pt1 = new Point3d(u, v, 0);

                    pc = BFCrv.Contains(pt1);

                    if (pc == PointContainment.Inside || pc == PointContainment.Coincident)
                    {
                        inside = true;
                        new_op.Add(Paths[i][0]);
                    }

                    for (int j = 1; j < Paths[i].Count; ++j)
                    {
                        Driver.Faces[0].ClosestPoint(Paths[i][j].Origin, out u, out v);
                        pt0 = pt1;
                        pt1 = new Point3d(u, v, 0);
                        pc = BFCrv.Contains(pt1);
                        if (pc == PointContainment.Inside || pc == PointContainment.Coincident)
                        {
                            if (!inside)
                                new_op.AddRange(IntersectSegment(BFCrv, pt0, pt1, Paths[i][j - 1], Paths[i][j], ref inside));
                            new_op.Add(Paths[i][j]);
                            inside = true;
                        }
                        else // the point is NOT within the boundary...
                        {
                            // find intersections between 
                            new_op.AddRange(IntersectSegment(BFCrv, pt0, pt1, Paths[i][j - 1], Paths[i][j], ref inside));
                            inside = false;
                        }
                    }
                    if (new_op.Count > 1)
                        NewPaths.Add(new_op);

                }

                Paths = NewPaths;
            }
            */
        }

        private List<Plane> IntersectSegment(Curve boundary, Point3d pt1, Point3d pt2, Plane p1, Plane p2, ref bool inside)
        {
            Line l = new Line(pt1, pt2);
            Curve lcrv = l.ToNurbsCurve();
            lcrv.Domain = new Interval(0, 1.0);

            Rhino.Geometry.Intersect.CurveIntersections ci = Rhino.Geometry.Intersect.Intersection.CurveCurve(boundary,
                lcrv, 1.0, 1.0);

            List<Plane> planes = new List<Plane>();

            if (ci.Count < 1)
            {
                //inside = false;
                return planes;
            }

            if (ci.Count % 2 == 0)
                inside = false;

            for (int i = 0; i < ci.Count; ++i)
            {
                planes.Add(Interpolation.InterpolatePlanes2(p1, p2, ci[i].ParameterB));
            }

            return planes;
        }

        public override List<Path> GetPaths()
        { 
            return Paths;
        }
    }
}
