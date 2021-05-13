﻿/*
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

using tas.Core.Types;

using Rhino.Geometry;

namespace tas.Machine.Toolpaths
{
    public abstract class ToolpathStrategy
    {
        public MachineTool Tool;
        public Plane Workplane;
        public double Tolerance;
        public double MaxDepth;

        internal ToolpathStrategy()
        {
            Workplane = Plane.WorldXY;
            Tool = new MachineTool();
            MaxDepth = double.MaxValue;
        }

        public abstract void Calculate();
        public abstract List<PPolyline> GetPaths();
    }
}
