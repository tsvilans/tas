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
using System.Security.Policy;
using Rhino.Geometry;
using StudioAvw.Geometry;

using tas.Core;
using tas.Core.Util;

namespace tas.Machine.Toolpaths
{
    public enum PathOffset
    {
        Left = -1,
        None = 0,
        Right = 1,
    }

    public class Toolpath_Contour: ToolpathStrategy
    {
        public double Depth;
        public bool StartEnd;

        public List<Curve> DriveCurves;
        public List<Path> Paths = null;
        public PathOffset Offset = 0;
        public CurveOffsetCornerStyle OffsetStyle = CurveOffsetCornerStyle.Sharp;


        public Toolpath_Contour(IEnumerable<Curve> drive_curves, PathOffset offset = PathOffset.None, double tolerance=0.01)
        {
            Workplane = Plane.WorldXY;
            Tolerance = tolerance;
            StartEnd = false;

            //DriveCurves = Misc.CurvesToPolylines(drive_curves.ToList(), Tolerance);
            DriveCurves = drive_curves.ToList();
            Offset = offset;

        }

        public override void Calculate()
        {
            int N = (int)Math.Ceiling(Math.Min(MaxDepth, Depth) / Tool.StepDown);
            Plane temp = Workplane;

            List<Curve> OffsetDriveCurves;
            Paths = new List<Path>();


            if (Offset != PathOffset.None)
            {
                OffsetDriveCurves = DriveCurves.SelectMany(x => x.Offset(Workplane, Tool.Diameter / 2 * (int)Offset, Tolerance, OffsetStyle)).ToList();
            }
            else
            {
                OffsetDriveCurves = DriveCurves;
            }

            var OffsetPolylines = new List<Polyline>();
            var PolyCurves = OffsetDriveCurves.Select(x => x.ToPolyline(0, 0, 0, 0, 0, Tolerance, 0, 0, true));

            List<Curve> tempCurves = null;
            for (int i = 0; i < N; ++i)
            {
                //temp.Origin = Workplane.Origin - Workplane.ZAxis * Tool.StepDown * i;
                tempCurves = OffsetDriveCurves.Select(x => x.DuplicateCurve()).ToList();
                for (int j = 0; j < tempCurves.Count; ++j)
                {
                    tempCurves[j].Transform(Transform.Translation(Workplane.ZAxis * -Tool.StepDown * i));
                    var polycurve = tempCurves[j].ToPolyline(0, 0, 0, 0, 0, Tolerance, 0, 0, true);
                    Paths.Add(new Path(polycurve.ToPolyline(), Workplane));
                }
            }

            // create bottom pocket layer
            tempCurves = OffsetDriveCurves.Select(x => x.DuplicateCurve()).ToList();
            for (int j = 0; j < tempCurves.Count; ++j)
            {
                tempCurves[j].Transform(Transform.Translation(Workplane.ZAxis * -Depth));
                var polycurve = tempCurves[j].ToPolyline(0, 0, 0, 0, 0, Tolerance, 0, 0, true);
                Paths.Add(new Path(polycurve.ToPolyline(), Workplane));
            }
        }

        public override List<Path> GetPaths()
        {
            return Paths;
        }
    }


}
