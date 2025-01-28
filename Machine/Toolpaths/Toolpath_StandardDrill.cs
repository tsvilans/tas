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
using tas.Core.Util;

namespace tas.Machine.Toolpaths
{

    public class Toolpath_StandardDrill : ToolpathStrategy
    {

        public double Depth;
        public double DepthPass;
        //public int Resolution;

        List<Path> Paths;

        public Toolpath_StandardDrill(Plane p, double depth, MachineTool tool, double depth_pass = 30)
        {
            Workplane = p;
            //Resolution = resolution;
            Depth = depth;
            DepthPass = depth_pass;
            Tool = tool;
        }

        public override void Calculate()
        {
            Paths = new List<Path>();

            int passes = (int)Math.Ceiling(Depth / DepthPass);
            double currentDepth = 0;

            for (int k = 0; k <= passes; ++k)
            {
                currentDepth = k * -DepthPass;
                //double passDepth = Math.Min(DepthPass, Depth + currentDepth);
                double passDepth = Math.Min(Depth, k * DepthPass);

                //Point3d tip = Workplane.Origin + Workplane.ZAxis * passDepth;
                Paths.Add(
                    new Path() { 
                        Workplane, 
                        new Plane(
                            Workplane.Origin - Workplane.ZAxis * passDepth, 
                             Workplane.XAxis, Workplane.YAxis)});
            }
        }


        public override List<Path> GetPaths()
        {
            return Paths;
        }
    }
}



