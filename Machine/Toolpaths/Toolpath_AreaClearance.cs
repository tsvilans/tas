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

using ClipperLib;
using StudioAvw.Geometry;

using Rhino.Geometry;

using tas.Core;

using CPath = System.Collections.Generic.List<ClipperLib.IntPoint>;
using CPaths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;


namespace tas.Machine.Toolpaths
{
    public class Toolpath_AreaClearance : ToolpathStrategy
    {
        private int LOOP_LIMIT = 500;

        // stock to leave
        public double RestHorizontal;
        public double RestVertical;
        public bool CheckForUndercuts;
        public bool RemoveSmallPaths;
        public double SmallPathThreshold;

        // reverse curve
        public bool StartEnd = false;

        public List<Mesh> Stock;
        public List<Mesh> DriveGeometry;

        // debug
        public List<Polyline> ShadowPolylines;

        List<Polyline> ResultPaths;

        public Toolpath_AreaClearance(Mesh Geometry) : this(new List<Mesh> { Geometry }, new List<Mesh> { Mesh.CreateFromBox(Geometry.GetBoundingBox(true), 1, 1, 1) })
        {
        }

        public Toolpath_AreaClearance(List<Mesh> Geometry, List<Mesh> StockGeometry) :
            this (Geometry, StockGeometry, new MachineTool())
        {
        }

        public Toolpath_AreaClearance(List<Mesh> geometry, List<Mesh> stock_geometry, MachineTool tool)
        {
            Tool = tool;
            Workplane = Plane.WorldXY;
            StartEnd = false;
            DriveGeometry = geometry;
            Stock = stock_geometry;
            MaxDepth = double.MaxValue;

            SmallPathThreshold = Tool.Diameter;
            Tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        }


        public override void Calculate()
        {
            ResultPaths = new List<Polyline>();

            if (Stock == null || Stock.Count < 1 || DriveGeometry == null || DriveGeometry.Count < 1)
                throw new Exception("Stock or drive geometry not set!");

            BoundingBox bb = Stock[0].GetBoundingBox(Workplane);
            foreach (Mesh m in Stock)
            {
                bb = BoundingBox.Union(bb, m.GetBoundingBox(Workplane));
            }

            if (bb.IsDegenerate(0.1) > 0)
                throw new Exception("Bounding Box is degenerate.");// return;
            Point3d top = bb.Center; top.Z = bb.Corner(false, false, false).Z;
            Point3d bottom = bb.Center; bottom.Z = bb.Corner(true, true, true).Z;

            double TotalDepth = top.DistanceTo(bottom);

            int N = (int)(Math.Ceiling(Math.Min(MaxDepth, TotalDepth) / Tool.StepDown));
            Plane CuttingPlane = new Plane(Workplane);
            Point3d top_xform = new Point3d(top);
            top_xform.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));
            CuttingPlane.Origin = top_xform;

            Tuple<CPaths, CPaths, CPaths> Polygons;
            Polygons = GeneratePolygons(CuttingPlane);

            CPaths Shadow = new CPaths(Polygons.Item1);
            double Area = PathsArea(Shadow);

            for (int i = 0; i <= N; ++i)
            {
                // for each layer
                CuttingPlane.Origin = CuttingPlane.Origin - Workplane.ZAxis * Tool.StepDown;

                Polygons = GeneratePolygons(CuttingPlane, Shadow);
                if (Polygons == null) throw new Exception("Failed to generate polygons!");

                double AreaNew = PathsArea(Polygons.Item1);
                if (AreaNew > Area)
                {
                    Shadow = new CPaths(Polygons.Item1);
                    Area = AreaNew;
                }

                ClipperOffset offset = new ClipperOffset(0.25, 0.25);
                offset.AddPaths(Polygons.Item3, JoinType.jtMiter, EndType.etClosedPolygon);

                PolyTree tree = new PolyTree();
                offset.Execute(ref tree, -(Tool.Diameter / 2 + RestHorizontal) / Tolerance);

                CPaths WorkingPaths = new CPaths();

                List<Polyline> Output = new List<Polyline>();

                foreach (PolyNode pn in tree.Iterate())
                {
                    if (pn.Contour.Count > 0)
                    {
                        Output.Add(pn.Contour.ToPolyline(CuttingPlane, Tolerance, true));
                        WorkingPaths.Add(pn.Contour);
                    }
                }

                int counter = 0;
                do
                {
                    offset.Clear();
                    offset.AddPaths(WorkingPaths, JoinType.jtMiter, EndType.etClosedPolygon);
                    offset.Execute(ref tree, -Tool.StepOver / Tolerance);

                    WorkingPaths = new List<List<IntPoint>>();
                    foreach (PolyNode pn in tree.Iterate())
                    {
                        if (pn.Contour.Count > 0)
                        {
                            Output.Add(pn.Contour.ToPolyline(CuttingPlane, Tolerance, true));
                            WorkingPaths.Add(pn.Contour);
                        }
                    }
                    counter++;
                }
                while (tree.Total > 0 && counter < LOOP_LIMIT);

                ResultPaths.AddRange(Output);
            }

            ShadowPolylines = new List<Polyline>();
            foreach (CPath p in Shadow)
            {
                ShadowPolylines.Add(p.ToPolyline(CuttingPlane, Tolerance, true));
            }

        }

        public override List<Path> GetPaths()
        {
            List<Path> OPs = new List<Path>();
            if (ResultPaths == null || ResultPaths.Count < 1)
                throw new Exception("Calculation did not succeed! No paths to yield.");
                //return OPs;

            if (RestVertical > 0.0001)
                AdjustRest();

            if (CheckForUndercuts)
                CheckUndercuts();

            foreach (Polyline p in ResultPaths)
            {
                if (RemoveSmallPaths)
                {
                    if (p.Length > SmallPathThreshold)
                        OPs.Add(new Path(p, Workplane));
                }
                else
                    OPs.Add(new Path(p, Workplane));
            }
            return OPs;
        }

        private double PathsArea(CPaths P)
        {
            double area = 0.0;
            foreach (CPath path in P)
            {
                area += Clipper.Area(path);
            }
            return area;
        }

        private void AdjustRest()
        {
            Ray3d ray;
            foreach (Polyline poly in ResultPaths)
            {
                for (int i = 0; i < poly.Count; ++i)
                {
                    ray = new Ray3d(poly[i], -Workplane.ZAxis);
                    foreach (Mesh m in DriveGeometry)
                    {
                        double d = Rhino.Geometry.Intersect.Intersection.MeshRay(m, ray);
                        if (d > 0.0 && d < RestVertical)
                            poly[i] = poly[i] + Workplane.ZAxis * (RestVertical - d);
                    }
                }
            }
        }

        private void CheckUndercuts()
        {
            List<Polyline> NewResultPaths = new List<Polyline>();
            Ray3d ray;
            bool Fucked;
            double d;

            foreach (Polyline p in ResultPaths)
            {
                Fucked = false;
                foreach (Point3d pt in p)
                {
                    ray = new Ray3d(pt, Workplane.ZAxis);
                    foreach (Mesh m in DriveGeometry)
                    {
                        d = Rhino.Geometry.Intersect.Intersection.MeshRay(m, ray);
                        if (d > 0.0) Fucked = true;
                    }
                }
                if (!Fucked)
                    NewResultPaths.Add(p);
            }

            ResultPaths = NewResultPaths;
        }

        private Tuple<CPaths, CPaths, CPaths> GeneratePolygons(Plane P, CPaths Shadow = null)
        {
            if (DriveGeometry == null || DriveGeometry.Count < 1) return null;
            if (Stock == null || Stock.Count < 1) return null;

            List<Polyline> Polygon = new List<Polyline>();
            List<List<IntPoint>> SolutionMesh = new List<List<IntPoint>>();
            List<List<IntPoint>> SolutionStock = new List<List<IntPoint>>();
            List<List<IntPoint>> SolutionPolygon = new List<List<IntPoint>>();
            Clipper clipper;

            // DO GEO
            List<Polyline> GeoSlices = new List<Polyline>();
            foreach (Mesh m in DriveGeometry)
            {
                Polyline[] x = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, P);
                if (x == null) continue;
                GeoSlices.AddRange(x);
            }
            if (GeoSlices.Count > 0)
            {
                clipper = new Clipper(0);
                clipper.AddPaths(GeoSlices.Select(x => x.ToPath2D(P, Tolerance)).ToList(), PolyType.ptClip, true);
                if (Shadow != null && Shadow.Count > 0)
                    clipper.AddPaths(Shadow, PolyType.ptClip, true);
                clipper.Execute(ClipType.ctUnion, SolutionMesh, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            }

            // DO STOCK
            List<Polyline> StockSlices = new List<Polyline>();
            foreach (Mesh m in Stock)
            {
                Polyline[] x = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, P);
                if (x == null) continue;
                StockSlices.AddRange(x);
            }

            if (StockSlices.Count > 0)
            {
                clipper = new Clipper(0);
                clipper.AddPaths(StockSlices.Select(x => x.ToPath2D(P, Tolerance)).ToList(), PolyType.ptClip, true);
                clipper.Execute(ClipType.ctUnion, SolutionStock, PolyFillType.pftNonZero, PolyFillType.pftNonZero);


                ClipperOffset stock_offset = new ClipperOffset(0.25, 0.25);
                stock_offset.AddPaths(SolutionStock, JoinType.jtMiter, EndType.etClosedPolygon);
                stock_offset.Execute(ref SolutionStock, (Tool.Diameter / 2 + RestHorizontal) / Tolerance);
            }

            if (SolutionStock.Count > 0 && SolutionMesh.Count > 0)
            {
                clipper = new Clipper(0);
                clipper.AddPaths(SolutionStock, PolyType.ptSubject, true);
                clipper.AddPaths(SolutionMesh, PolyType.ptClip, true);
                clipper.Execute(ClipType.ctDifference, SolutionPolygon, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            }
            else
            {
                SolutionPolygon = SolutionStock;
            }

            return new Tuple<CPaths, CPaths, CPaths>(SolutionMesh, SolutionStock, SolutionPolygon);
        }

        private class ACLayer
        {
            public Plane Workplane;
            public List<ACIsland> Islands;

            public void Calculate(double d)
            {
                foreach (ACIsland pi in Islands)
                {
                    pi.Calculate(Workplane, d);
                }
            }

            public ACLayer(IEnumerable<Polyline> drive_curves, Plane plane)
            {
                Islands = new List<ACIsland>();
                Workplane = plane;
                foreach (Polyline poly in drive_curves)
                {
                    // project curve onto workplane
                    List<Point3d> points = new List<Point3d>();
                    foreach (Point3d p in poly)
                    {
                        points.Add(p.ProjectToPlane(Workplane));
                    }
                    Islands.Add(new ACIsland(new Polyline(points)));
                }
            }
            public List<Polyline> GetPaths()
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ACIsland island in Islands)
                {
                    polylines.AddRange(island.GetPaths());
                }
                return polylines;
            }
        }

        private class ACIsland
        {
            public List<ACIsland> Children;
            public Polyline DriveCurve;
            public List<Polyline> Paths;

            public ACIsland(Polyline poly)
            {
                DriveCurve = poly;
                Children = new List<ACIsland>();
                Paths = new List<Polyline>();
            }

            public void Calculate(Plane p, double d)
            {
                List<Polyline> offC, offH;
                offH = new List<Polyline>() { DriveCurve };

                while (offH.Count == 1)
                {
                    Polyline3D.Offset(offH, Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, d, p, 0.01, out offC, out offH);
                    // if there is only one offset, then it is still part of the same island
                    if (offH.Count == 1)
                    {
                        Paths.Add(offH[0]);
                    }
                    // if there is more than 1 offset, then we create two new layers / islands
                    else if (offH.Count > 1)
                    {
                        for (int i = 0; i < offH.Count; ++i)
                        {
                            Children.Add(new ACIsland(offH[i]));
                        }
                    }
                }

                foreach (ACIsland pl in Children)
                {
                    pl.Calculate(p, d);
                }
            }

            public Polyline Link()
            {
                Polyline LinkedPath = new Polyline();
                LinkedPath.AddRange(DriveCurve);

                int index;
                Point3d ClosestPoint = LinkedPath.Last;
                for (int i = 0; i < Paths.Count; ++i)
                {
                    ClosestPoint = Paths[i].ClosestPoint(ClosestPoint);
                    LinkedPath.Add(ClosestPoint);

                    index = Paths[i].ClosestIndex(ClosestPoint);
                    int real_index;
                    int count = Paths[i].Count;
                    for (int j = 0; j < Paths[i].Count; ++j)
                    {
                        real_index = (j + index) % count;
                        LinkedPath.Add(Paths[i][real_index]);
                    }
                    LinkedPath.Add(ClosestPoint);
                }

                LinkedPath.Reverse();
                LinkedPath.DeleteShortSegments(0.1);

                return LinkedPath;
            }
            public List<Polyline> GetPaths()
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (ACIsland island in Children)
                {
                    polylines.AddRange(island.GetPaths());
                }
                polylines.Add(Link());
                return polylines;
            }
        }
    }
}
