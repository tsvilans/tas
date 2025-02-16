﻿/*
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
    public enum ToolShape
    {
        Flat,
        Ball
    }

    /// <summary>
    /// Simple class for holding tool information.
    /// </summary>
    public class MachineTool
    {
        /// <summary>
        /// Name of tool.
        /// </summary>
        public string Name;

        /// <summary>
        /// Shape of tool tip.
        /// </summary>
        public ToolShape Shape;

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
        public double FeedRateIPM { get { return (double)FeedRate / 25.4; } set { FeedRate = (int)(value * 25.4); } }

        /// <summary>
        /// Tool rapid rate.
        /// </summary>
        public int RapidRate;
        public double RapidRateIPM { get { return (double)RapidRate / 25.4; } set { RapidRate = (int)(value * 25.4); } }

        /// <summary>
        /// Tool plunge rate.
        /// </summary>
        public int PlungeRate;
        public double PlungeRateIPM { get { return (double)PlungeRate / 25.4; } set { PlungeRate = (int)(value * 25.4); } }

        /// <summary>
        /// Tool spindle speed.
        /// </summary>
        public int SpindleSpeed;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MachineTool()
        {
            Name = "DefaultTool";
            Diameter = 12;
            Length = 100;
            Number = 1;
            RapidRate = 10000;
            FeedRate = 2000;
            PlungeRate = 1000;
            SpindleSpeed = 18000;
            OffsetNumber = 1;
            StepDown = Diameter / 2;
            StepOver = Diameter / 2;
            Shape = ToolShape.Flat;
        }

        public MachineTool(string name = "MachineTool", double diameter=12, int tool_number=0, int offset_number=0,
            double length = 0.0, int feed = 2000, int speed = 15000, int plunge = 600, ToolShape shape = ToolShape.Flat)
        {
            Name = name;
            Diameter = diameter;
            Length = length;
            Number = tool_number;
            RapidRate = 10000;
            FeedRate = feed;
            PlungeRate = plunge;
            SpindleSpeed = speed;
            OffsetNumber = offset_number;
            StepDown = Diameter / 2;
            StepOver = Diameter / 2;
            Shape = shape;
        }

        public override bool Equals(object obj)
        {
            if (obj as MachineTool != null)
            {
                MachineTool mt = obj as MachineTool;
                if (mt.Name == this.Name &&
                    mt.Number == this.Number &&
                    mt.OffsetNumber == this.OffsetNumber &&
                    mt.Length == this.Length &&
                    mt.Shape == this.Shape)
                    return true;
            }
            return false;
        }

        public MachineTool Duplicate()
        {
            return new MachineTool
            {
                Diameter = Diameter,
                FeedRate = FeedRate,
                Length = Length,
                Shape = Shape,
                SpindleSpeed = SpindleSpeed,
                StepDown = StepDown,
                StepOver = StepOver,
                Name = Name,
                Number = Number,
                OffsetNumber = OffsetNumber,
                PlungeRate = PlungeRate
            };
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