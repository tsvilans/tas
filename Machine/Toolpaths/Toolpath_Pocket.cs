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
using StudioAvw.Geometry;

using tas.Core;
using tas.Core.Types;
using tas.Core.Util;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_Pocket : ToolpathStrategy
    {
        public double Depth;
        public bool StartEnd;

        List<PocketLayer> Layers;

        public List<Polyline> DriveCurves;

        private class PocketLayer
        {
            public Plane Workplane;
            public List<PocketIsland> Islands;
            public void Calculate(double d)
            {
                foreach (PocketIsland pi in Islands)
                {
                    pi.Calculate(Workplane, d);
                }
            }

            public PocketLayer(IEnumerable<Polyline> drive_curves, Plane plane)
            {
                Islands = new List<PocketIsland>();
                Workplane = plane;
                foreach (Polyline poly in drive_curves)
                {
                    // project curve onto workplane
                    List<Point3d> points = new List<Point3d>();
                    foreach (Point3d p in poly)
                    {
                        points.Add(p.ProjectToPlane(Workplane));
                    }
                    Islands.Add(new PocketIsland(new Polyline(points)));
                }
            }
            public List<Polyline> GetPaths()
            {
                List<Polyline> polylines = new List<Polyline>();
                foreach (PocketIsland island in Islands)
                {
                    polylines.AddRange(island.GetPaths());
                }
                return polylines;
            }
        }

        // crafting entails a familiarity with material performance
        // me ask for a dialogue, twice a week

        private class PocketIsland
        {
            public List<PocketIsland> Children;
            public Polyline DriveCurve;
            public List<Polyline> Paths;

            public PocketIsland(Polyline poly)
            {
                DriveCurve = poly;
                Children = new List<PocketIsland>();
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
                            Children.Add(new PocketIsland(offH[i]));
                        }
                    }
                }

                foreach (PocketIsland pl in Children)
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
                foreach (PocketIsland island in Children)
                {
                    polylines.AddRange(island.GetPaths());
                }
                polylines.Add(Link());
                return polylines;
            }
        }

        public Toolpath_Pocket(IEnumerable<Polyline> drive_curves)
        {
            Workplane = Plane.WorldXY;
            Layers = new List<PocketLayer>();
            Tolerance = 0.5;
            StartEnd = false;
            DriveCurves = drive_curves.ToList();
        }

        public Toolpath_Pocket(IEnumerable<Curve> drive_curves, double tolerance=0.01)
        {
            Workplane = Plane.WorldXY;
            Layers = new List<PocketLayer>();
            Tolerance = tolerance;
            StartEnd = false;

            DriveCurves = Misc.CurvesToPolylines(drive_curves.ToList(), Tolerance);
        }

        public override void Calculate()
        {
            if (Tool.StepOver < 0.1) return;
            // create pocket layers
            int N = (int)Math.Ceiling(Math.Min(MaxDepth, Depth) / Tool.StepDown);
            Plane temp = Workplane;
            List<Polyline> OffsetDriveCurves, DriveCurveContours;
            Polyline3D.Offset(
                DriveCurves, 
                Polyline3D.OpenFilletType.Butt, Polyline3D.ClosedFilletType.Miter, 
                Tool.Diameter / 2, Workplane, 0.01, 
                out DriveCurveContours, out OffsetDriveCurves);

            for (int i = 0; i < N; ++i)
            {
                temp.Origin = Workplane.Origin - Workplane.ZAxis * Tool.StepDown * i;
                Layers.Add(new PocketLayer(OffsetDriveCurves, temp));
            }

            // create bottom pocket layer
            temp.Origin = Workplane.Origin - Workplane.ZAxis * Depth;
            Layers.Add(new PocketLayer(OffsetDriveCurves, temp));

            foreach (PocketLayer pl in Layers)
            {
                pl.Calculate(Tool.StepOver);
            }
        }

        public override List<PPolyline> GetPaths()
        {
            List<PPolyline> paths = new List<PPolyline>();
            List<Polyline> polylines = new List<Polyline>();

            foreach (PocketLayer layer in Layers)
            {
                polylines.AddRange(layer.GetPaths());
            }

            foreach (Polyline p in polylines)
            {
                if (StartEnd)
                    p.Reverse();
                paths.Add(new PPolyline(p, Workplane));
            }

            return paths;
        }
    }


}
