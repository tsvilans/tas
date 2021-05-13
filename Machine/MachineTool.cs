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

using Rhino.Geometry;

namespace tas.Machine
{
    /// <summary>
    /// Simple class for holing tool information.
    /// </summary>
    public class MachineTool
    {
        /// <summary>
        /// Name of tool.
        /// </summary>
        public string Name;

        /// <summary>
        /// Diameter of tool.
        /// </summary>
        public double Diameter;

        /// <summary>
        /// Step over distance.
        /// </summary>
        public double StepOver;

        /// <summary>
        /// Step down distance.
        /// </summary>
        public double StepDown;

        /// <summary>
        /// Length of tool.
        /// </summary>
        public double Length;

        /// <summary>
        /// Machine-specific tool number.
        /// </summary>
        public int Number;

        /// <summary>
        /// Machine-specific height offset number.
        /// </summary>
        public int OffsetNumber;

        /// <summary>
        /// Tool feed rate.
        /// </summary>
        public int FeedRate;

        /// <summary>
        /// Tool plunge rate.
        /// </summary>
        public int PlungeRate;

        /// <summary>
        /// Tool spindle speed.
        /// </summary>
        public int SpindleSpeed;

        public MachineTool(string name = "MachineTool", double diameter=12, int tool_number=0, int offset_number=0,
            double length = 0.0, int feed = 2000, int speed = 15000, int plunge = 600)
        {
            Name = name;
            Diameter = diameter;
            Length = length;
            Number = tool_number;
            FeedRate = feed;
            PlungeRate = plunge;
            SpindleSpeed = speed;
            OffsetNumber = offset_number;
            StepDown = Diameter / 2;
            StepOver = Diameter / 2;
        }

        public override bool Equals(object obj)
        {
            if (obj as MachineTool != null)
            {
                MachineTool mt = obj as MachineTool;
                if (mt.Name == this.Name &&
                    mt.Number == this.Number &&
                    mt.OffsetNumber == this.OffsetNumber &&
                    mt.Length == this.Length)
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"MachineTool ({Name}, Number: {Number}, Length: {Length})";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}