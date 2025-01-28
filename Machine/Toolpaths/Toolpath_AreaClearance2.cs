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
using System.Threading;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_AreaClearance2 : ToolpathStrategy
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
        public List<Mesh> Bounds;

        // debug
        public List<Polyline> ShadowPolylines;

        List<Polyline> ResultPaths;

        public Toolpath_AreaClearance2(Mesh Geometry) : this(new List<Mesh> { Geometry }, new List<Mesh> { Mesh.CreateFromBox(Geometry.GetBoundingBox(true), 1, 1, 1) })
        {
        }

        public Toolpath_AreaClearance2(List<Mesh> Geometry, List<Mesh> StockGeometry) :
            this(Geometry, StockGeometry, new List<Mesh>(), new MachineTool())
        {
        }

        public Toolpath_AreaClearance2(List<Mesh> Geometry, List<Mesh> StockGeometry, List<Mesh> Bounds) :
            this(Geometry, StockGeometry, Bounds, new MachineTool())
        {
        }

        public Toolpath_AreaClearance2(List<Mesh> geometry, List<Mesh> stock_geometry, List<Mesh> bounds, MachineTool tool)
        {
            Tool = tool;
            Workplane = Plane.WorldXY;
            StartEnd = false;
            DriveGeometry = geometry;
            Stock = stock_geometry;
            Bounds = bounds;
            MaxDepth = double.MaxValue;

            SmallPathThreshold = Tool.Diameter;
            Tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        }


        public override void Calculate()
        {
            ResultPaths = new List<Polyline>();

            if (Stock == null || Stock.Count < 1 || DriveGeometry == null || DriveGeometry.Count < 1)
                throw new Exception("Stock or drive geometry not set!");

            BoundingBox bb = BoundingBox.Empty;
            if (Bounds != null && Bounds.Count > 0)
            {
                foreach (Mesh m in Bounds)
                {
                    bb.Union(m.GetBoundingBox(Workplane));
                }
            }
            else
            {
                foreach (Mesh m in Stock)
                {
                    bb.Union(m.GetBoundingBox(Workplane));
                }
            }

            if (bb.IsDegenerate(0.1) > 0)
                throw new Exception("Bounding Box is degenerate.");// return;
            Point3d top = bb.Center; top.Z = bb.Corner(false, false, false).Z;
            Point3d bottom = bb.Center; bottom.Z = bb.Corner(true, true, true).Z;

            //double TotalDepth = top.DistanceTo(bottom);
            double TotalDepth = bb.Max.Z - bb.Min.Z;

            TotalDepth = Math.Min(MaxDepth, TotalDepth);

            int N = (int)(Math.Ceiling(TotalDepth / Tool.StepDown));

            double difference = N * Tool.StepDown - TotalDepth;

            Plane CuttingPlane = new Plane(Workplane);

            Point3d top_xform = new Point3d(top);
            top_xform.Transform(Transform.Translation(0, 0, difference));

            top_xform.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));
            CuttingPlane.Origin = top_xform;

            Tuple<CPaths, CPaths, CPaths> Polygons;
            Polygons = GeneratePolygons(CuttingPlane);

            CPaths Shadow = new CPaths(Polygons.Item1);
            double Area = PathsArea(Shadow);

            for (int i = 0; i < N; ++i)
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

                List<Polyline> Output = new List<Polyline>();
                var OutputRaw = new List<CPath>();

                foreach (PolyNode child in tree.Childs)
                {
                    OffsetChild(child, OutputRaw, -Tool.StepOver / Tolerance, LOOP_LIMIT);
                }

                foreach (var path in OutputRaw)
                {
                    Output.Add(path.ToPolyline(CuttingPlane, Tolerance, true));
                }

                ResultPaths.AddRange(Output);
            }

            ShadowPolylines = new List<Polyline>();
            foreach (CPath p in Shadow)
            {
                ShadowPolylines.Add(p.ToPolyline(CuttingPlane, Tolerance, true));
            }

        }

        void OffsetChild(PolyNode node, List<CPath> results, double distance, int iteration = 500)
        {
            if (iteration < 1) return;
            var offset = new ClipperOffset(0.25, 0.25);
            var tree = new PolyTree();

            var paths = new List<CPath>() { node.Contour };
            paths.AddRange(node.Childs.Select(x => x.Contour));

            offset.AddPaths(paths, JoinType.jtMiter, EndType.etClosedPolygon);
            results.AddRange(paths);

            offset.Execute(ref tree, distance);

            if (tree.Childs.Count < 1) return;

            foreach (var child in tree.Childs)
            {
                OffsetChild(child, results, distance, iteration - 1);
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
                if (RemoveSmallPaths && p.Length > SmallPathThreshold)
                        OPs.Add(new Path(p, Workplane));
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

            CPaths SolutionMesh = new CPaths(),
                SolutionStock = new CPaths (),
                SolutionPolygon = new CPaths(),
                SolutionBounds = new CPaths(); ;

            Clipper clipper;

            var BoundsSlices = new List<Polyline>();
            if (Bounds != null && Bounds.Count > 0)
            {
                foreach (Mesh m in Bounds)
                {
                    Polyline[] x = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, P);
                    if (x == null) continue;
                    BoundsSlices.AddRange(x);
                }

                // Check that there are actual bound slices
                // If the bounds are not hit, then the slice is out of bounds
                if (BoundsSlices.Count < 1)
                    return new Tuple<CPaths, CPaths, CPaths>(SolutionMesh, SolutionStock, SolutionPolygon);
            }

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

                if (BoundsSlices.Count > 0)
                {
                    clipper = new Clipper(0);
                    clipper.AddPaths(BoundsSlices.Select(x => x.ToPath2D(P, Tolerance)).ToList(), PolyType.ptClip, true);
                    clipper.Execute(ClipType.ctUnion, SolutionBounds, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

                    clipper = new Clipper(0);
                    clipper.AddPaths(SolutionStock, PolyType.ptClip, true);
                    clipper.AddPaths(SolutionBounds, PolyType.ptSubject, true);

                    clipper.Execute(ClipType.ctIntersection, SolutionStock, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
                }

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

            /*
            for (int i = 0; i < SolutionPolygon.Count; ++i)
            {
                if (Clipper.Orientation(SolutionPolygon[i]))
                {
                    SolutionPolygon.Reverse();
                }
            }
            */

            return new Tuple<CPaths, CPaths, CPaths>(SolutionMesh, SolutionStock, SolutionPolygon);
        }
    }
}
