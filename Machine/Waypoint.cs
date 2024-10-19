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

    public struct AxisValues
    {
        public double X
        {
            get { return m_values[0]; }
            set { m_values[0] = value; }
        }
        public double Y
        {
            get { return m_values[1]; }
            set { m_values[1] = value; }
        }
        public double Z
        {
            get { return m_values[2]; }
            set { m_values[2] = value; }
        }

        public double B
        {
            get { return m_values[4]; }
            set { m_values[4] = value; }
        }

        public double C
        {
            get { return m_values[5]; }
            set { m_values[5] = value; }
        }

        public double Speed
        {
            get { return m_values[6]; }
            set { m_values[6] = value; }
        }

        double[] m_values;

        public AxisValues(double x = 0, double y = 0, double z = 0, double b = 0, double c = 0, double speed = 0.0)
        {
            m_values = new double[7];
            m_values[0] = x;
            m_values[1] = y;
            m_values[2] = z;
            m_values[4] = b;
            m_values[5] = c;
            m_values[6] = speed;
        }
        public override bool Equals(object obj)
        {
            if (obj is AxisValues)
            {
                AxisValues av = (AxisValues)obj;
                return X == av.X &&
                    Y == av.Y &&
                    Z == av.Z &&
                    B == av.B &&
                    C == av.C &&
                    Speed == av.Speed;
            }
            return false;
        }

        public override string ToString() => $"AxisValues (X{X:0.00} Y{Y:0.00} Z{Z:0.00} B{B:0.00} C{C:0.00} Speed{Speed:0}";
        public override int GetHashCode()
        {
            int hc = m_values.Length;
            foreach (double val in m_values)
            {
                hc = unchecked(hc * 314159 + (int)(val * 1000));
            }
            return hc;
        }

    }
    public struct Waypoint
    {
        /// <summary>
        /// Position and orientation of the waypoint.
        /// </summary>
        public Plane Plane;

        /// <summary>
        /// Type of movement (any bitwise combination of WaypointType).
        /// </summary>
        public int Type;

        /// <summary>
        /// If the waypoint is an Arc movement, the radius of the arc.
        /// </summary>
        public double Radius;

        public static Waypoint Unset
        {
            get
            {
                return new Waypoint(Plane.Unset);
            }
        }
        /// <summary>
        /// The waypoint is a Rapid movement
        /// </summary>
        public bool Rapid
        {
            get
            {
                return (Type & 1) != 0;
            }
        }
        /// <summary>
        /// The waypoint is a Plunge movement.
        /// </summary>
        public bool Plunge
        {
            get
            {
                return (Type & 2) != 0;
            }
        }
        /// <summary>
        /// The waypoint is a Feed movement.
        /// </summary>
        public bool Feed
        {
            get
            {
                return (Type & 3) != 0;
            }
        }
        /// <summary>
        /// The waypoint is an Arc movement.
        /// </summary>
        public bool Arc
        {
            get
            {
                return (Type & 4) != 0;
            }
        }
        /// <summary>
        /// If Arc, returns whether or not it is clockwise.
        /// </summary>
        public bool Clockwise
        {
            get
            {
                return (Type & 12) != 0;
            }
        }

        public bool IsRapid() => (Type & (int)WaypointType.RAPID) != 0; // if bit 1 is on
        public bool IsPlunge() => (Type & (int)WaypointType.PLUNGE) != 0; // if bit 2 is on
        public bool IsFeed() => (Type & ((int)WaypointType.RAPID | (int)WaypointType.PLUNGE)) == 0; // if neither bit 1 or 2 are on
        public bool IsArc() => (Type & (int)WaypointType.ARC_CW) != 0; // if bit 3 is on

        public bool IsClockwise() => (Type & (int)WaypointType.ARC_CCW) != 0;
        public bool IsCounterClockwise() => (Type & (int)WaypointType.ARC_CCW) == 0;

        public Waypoint(Plane p, int t = (int)WaypointType.FEED, double r = 0.0)
        {
            Plane = p;
            Type = t;
            Radius = r;
        }

        public Waypoint(Waypoint wp)
        {
            Plane = wp.Plane;
            Type = wp.Type;
            Radius = wp.Radius;
        }

        public bool Transform(Transform xform)
        {
            Plane p = this.Plane;
            if (!p.Transform(xform))
            {
                return false;
                //throw new System.Exception(string.Format(
                //    "Failed to transform Waypoint: {0} {1}", p.IsValid, p));
            }
            this.Plane = p;
            return true;
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
        ARC_CCW = 12,
        UNKNOWN = 255
    }
}