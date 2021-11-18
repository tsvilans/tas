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

using Rhino.Geometry;

namespace tas.Machine.Toolpaths
{

    public class Toolpath_HelicalDrill : ToolpathStrategy
    {

        public Circle Outline;
        public double Depth;
        public double DepthPass;
        public int Resolution;

        List<Path> Paths;

        public Toolpath_HelicalDrill(Circle c, double depth, MachineTool tool, double depth_pass = 30, int resolution = 36)
        {
            Workplane = c.Plane;
            Resolution = resolution;
            Outline = c;
            Depth = depth;
            DepthPass = depth_pass;
            Tool = tool;
        }

        public override void Calculate()
        {
            Paths = new List<Path>();

            int passes = (int)Math.Ceiling(Depth / DepthPass);
            double currentDepth = 0;

            for (int k = 0; k < passes; ++k)
            {
                currentDepth = k * -DepthPass;
                double passDepth = Math.Min(DepthPass, Depth + currentDepth);

                var helix = new Polyline();

                double radius = Outline.Radius - Tool.Diameter / 2;

                int N = (int)Math.Ceiling(radius / Tool.StepOver);
                double rStep = radius / N;

                for (int i = 1; i < N + 1; ++i)
                {

                    double tempRadius = rStep * i;

                    double aa, t, a0 = 0;

                    int hN = ((int)Math.Ceiling(passDepth / Tool.StepDown) - 1) * Resolution;

                    aa = 2.0 * Math.PI; // angular speed coefficient

                    double tstep = 1.0 / Resolution;
                    helix.Add(new Point3d(0, 0, currentDepth));

                    for (int j = 0; j < hN; ++j)
                    {
                        t = tstep * j;

                        helix.Add(new Point3d(
                          tempRadius * Math.Cos(a0 + aa * t),
                          tempRadius * Math.Sin(a0 + aa * t),
                          currentDepth - Tool.StepDown * t
                          ));
                    }

                    double closeDepth = currentDepth - tstep * hN * Tool.StepDown;
                    double rest = currentDepth - passDepth - closeDepth;

                    for (int j = 0; j < Resolution; ++j)
                    {
                        t = tstep * j;

                        helix.Add(new Point3d(
                          tempRadius * Math.Cos(a0 + aa * t),
                          tempRadius * Math.Sin(a0 + aa * t),
                          closeDepth + (t * rest)
                          ));
                    }


                    for (int j = 0; j < Resolution + 1; ++j)
                    {
                        t = tstep * j + tstep * hN;

                        helix.Add(new Point3d(
                          tempRadius * Math.Cos(a0 + aa * t),
                          tempRadius * Math.Sin(a0 + aa * t),
                          currentDepth - passDepth
                          ));
                    }

                    helix.Add(new Point3d(0, 0, currentDepth - passDepth));
                }

                helix.Transform(Transform.PlaneToPlane(Plane.WorldXY, Workplane));
                var ppoly = new Path(helix, Workplane);

                Paths.Add(ppoly);
            }
        }


        public override List<Path> GetPaths()
        {
            return Paths;
        }
    }
}



