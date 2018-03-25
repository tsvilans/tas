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

using tas.Core;
using tas.Core.Types;

namespace tas.Machine.Toolpaths
{
    public class Toolpath_Flowline : ToolpathStrategy
    {
        Surface DriveSurface;
        bool Direction;
        public bool StartEnd;
        List<PPolyline> Paths;

        public Toolpath_Flowline(Surface surf, bool direction = false)
        {
            Tolerance = 0.5;
            DriveSurface = surf;
            Direction = direction;
        }

        public override void Calculate()
        {
            if (DriveSurface == null) return;

            int U = Direction ? 1 : 0;
            int V = Direction ? 0 : 1;

            Paths = new List<PPolyline>();
            Curve IsoFence;

            if (StartEnd)
                IsoFence = DriveSurface.IsoCurve(U, DriveSurface.Domain(V).Min);
            else
                IsoFence = DriveSurface.IsoCurve(U, DriveSurface.Domain(V).Max);

            double[] Uts = IsoFence.DivideByLength(Tool.StepOver, true);
            if (Uts == null) return;

            List<double> UtsList = Uts.ToList();

            UtsList.Add(IsoFence.Domain.Max);

            for (int i = 0; i < Uts.Length; ++i)
            {
                Curve Iso = DriveSurface.IsoCurve(V, Uts[i]);
                Polyline Pl = Util.CurveToPolyline(Iso, Tolerance);
                PPolyline OPl = new PPolyline();
                Vector3d nor, tan, x;

                foreach (Point3d p in Pl)
                {
                    double t, u, v;
                    DriveSurface.ClosestPoint(p, out u, out v);
                    nor = DriveSurface.NormalAt(u, v);
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


        }

        public override List<PPolyline> GetPaths()
        { 
            return Paths;
        }
    }
}
