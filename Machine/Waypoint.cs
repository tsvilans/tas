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

using Rhino.Geometry;

namespace tas.Machine
{
    public struct Waypoint
    {
        public Plane Plane;
        public int Type;

        public static Waypoint Unset
        {
            get
            {
                return new Waypoint(Plane.Unset);
            }
        }

        public bool IsRapid() => (Type & 1) != 0;
        public bool IsFeed() => (Type & 3) == 0;
        public bool IsPlunge() => (Type & 2) != 0;
        public bool IsArc() => (Type & 4) != 0;

        public Waypoint(Plane p, int t = (int)WaypointType.FEED, bool plunging = false)
        {
            Plane = p;
            Type = t;
        }

        public Waypoint(Waypoint wp)
        {
            Plane = wp.Plane;
            Type = wp.Type;
        }

        public static implicit operator Plane(Waypoint wp) => wp.Plane;
        public static implicit operator Waypoint(Plane p) => new Waypoint(p);

        public override bool Equals(object obj)
        {
            if (obj is Waypoint)
            {
                Waypoint wp = (Waypoint)obj;
                if (wp.Plane == this.Plane
                    && wp.Type == this.Type)
                    return true;
            }
            return false;
        }

        public override string ToString() => $"Waypoint ({this.Plane.ToString()}, {this.Type.ToString()}";

        public override int GetHashCode() => this.Plane.GetHashCode();

    }

    /// <summary>
    /// Bit flags used to distinguish between types of targets. 
    /// Bit 1 : rapid move if true, overrides everything else
    /// Bit 2 : feed move if false, plunge move if true
    /// Bit 3 : arc move if true
    /// Bit 4 : CW arc if false, CCW if true
    /// These can therefore be combined: PLUNGE | ARC_CW, etc.
    /// </summary>
    public enum WaypointType
    {
        RAPID = 1,
        FEED = 0,
        PLUNGE = 2,
        ARC_CW = 4,
        ARC_CCW = 12
    }
}