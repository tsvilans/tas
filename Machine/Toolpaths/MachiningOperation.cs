﻿#if LEGACY

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using tasTools.Types;
//using Robots;

using Path = System.Collections.Generic.List<tasTools.Toolpaths.Waypoint>;

namespace tasTools.Toolpaths
{
    public class MachiningOperation
    {
        public Guid Id { get; }
        public string Name;

        public OrientedPolyline Path;
        public double CutSpeed;
        public double RapidSpeed;
        public double PlungeSpeed;
        public double DrillSpeed;

        public List<OperationType> Types;

        public enum OperationType
        {
            Drill,
            Plunge,
            Cut,
            Rapid
        }

        MachiningOperation()
        {
            Id = Guid.NewGuid();
            Name = "Default";
            CutSpeed = 15.0;
            RapidSpeed = 100.0;
            PlungeSpeed = 10.0;
            DrillSpeed = 5.0;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            MachiningOperation mo = obj as MachiningOperation;
            if (mo == null) return false;
            return mo.Id == Id;
        }

        public override string ToString()
        {
            return string.Format("MachiningOperation({0})", Name);
        }

        public static implicit operator OrientedPolyline(MachiningOperation mo)
        {
            return mo.Path;
        }

        public static implicit operator Polyline(MachiningOperation mo)
        {
            return (Polyline)mo.Path;
        }
/*
        List<Target> ConvertToRobotsTargets(Tool tool, Zone zone = null, Frame frame = null, double[] ext = null)
        {
            if (Types.Count != Path.Count)
                throw new Exception("Invalid MachiningOperation data! Types count is different from Path count.");

            List<Target> targets = new List<Target>();

            Speed CutSpeedR = new Speed(CutSpeed, 1.57079, "CutSpeed");
            Speed RapidSpeedR = new Speed(RapidSpeed, 1.57079, "RapidSpeed");
            Speed PlungeSpeedR = new Speed(PlungeSpeed, 1.57079, "PlungeSpeed");
            Speed DrillSpeedR = new Speed(DrillSpeed, 1.57079, "DrillSpeed");

            for (int i = 0; i < Path.Count; ++i)
            {
                switch (Types[i])
                {
                    case (OperationType.Cut):
                        targets.Add(new CartesianTarget(Path[i], null, Target.Motions.Linear, tool, CutSpeedR, zone, null, frame, ext));
                        break;
                    case (OperationType.Rapid):
                        targets.Add(new CartesianTarget(Path[i], null, Target.Motions.Linear, tool, RapidSpeedR, zone, null, frame, ext));
                        break;
                    case (OperationType.Plunge):
                        targets.Add(new CartesianTarget(Path[i], null, Target.Motions.Linear, tool, PlungeSpeedR, zone, null, frame, ext));
                        break;
                    case (OperationType.Drill):
                        targets.Add(new CartesianTarget(Path[i], null, Target.Motions.Linear, tool, DrillSpeedR, zone, null, frame, ext));
                        break;
                    default:
                        break;
                }
            }

            return targets;
        }
*/
    }

    public class Toolpath
    {
        public Guid Id { get; private set; }
        public List<Path> Paths;
        public string ToolName;
        public string Name;

        public double ToolDiameter;
        public int SpindleSpeed;

        private object Safety;
        public double FeedRate { get; set; }
        public double PlungeRate { get; set; }
        public double RapidRate { get; set; }

        public double SafeZ { get; set; }
        public double RapidZ { get; set; }

        public bool IsPlanar { get; set; }


        private Waypoint RetractToSafety(Waypoint current)
        {
            if (Safety == null)
                throw new Exception("No safety defined!");

            Ray3d ray = new Ray3d(current.Plane.Origin, current.Plane.ZAxis);

            if (Safety is Plane)
            {
                double t;
                Line line = new Line(current.Plane.Origin, current.Plane.ZAxis);
                if (Rhino.Geometry.Intersect.Intersection.LinePlane(line, (Plane)Safety, out t))
                {
                    Waypoint p = new Waypoint(current);
                    p.Plane.Origin = line.PointAt(t);
                    return p;
                }
            }
            else if (Safety is Mesh)
            {
                double d = Rhino.Geometry.Intersect.Intersection.MeshRay(Safety as Mesh, ray);
                Waypoint p = new Waypoint(current);
                p.Plane.Origin = current.Plane.Origin + current.Plane.ZAxis * d;
                return p;
            }
            else if (Safety is GeometryBase)
            {
                Point3d[] pts = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, new GeometryBase[] { Safety as GeometryBase }, 1);
                if (pts.Length > 0)
                {
                    Waypoint p = new Waypoint(current);
                    p.Plane.Origin = pts[0];
                    return p;
                }
            }

            return current;
        }

        private Path LinkOnSafety(Waypoint A, Waypoint B, ref string debug)
        {
            double SkipDistance = 20.0;
            double minD = 10.0;
            double step = 10.0;
            int counter = 0;
            int maxIter = 1000;

            if (A.Plane.Origin.DistanceTo(B.Plane.Origin) < SkipDistance)
                return new Path();

            Path link_targets = new Path();
            List<Point3d> link_points = new List<Point3d>();
            List<Vector3d> normals = new List<Vector3d>();

            if (Safety is Mesh)
            {
                Mesh m = Safety as Mesh;
                Point3d point = A.Plane.Origin;
                MeshPoint mp = m.ClosestMeshPoint(point, step * 0.99);
                while (point.DistanceTo(B.Plane.Origin) > minD && counter < maxIter)
                {
                    counter++;
                    Vector3d toEnd = new Vector3d(B.Plane.Origin - mp.Point);
                    Vector3d n = m.NormalAt(mp);
                    Vector3d v = Util.ProjectToPlane(toEnd, new Plane(mp.Point, n));
                    v.Unitize();
                    point = mp.Point + v * step;
                    mp = m.ClosestMeshPoint(point, step / 2);
                    link_points.Add(mp.Point);
                    normals.Add(n);
                }

                Polyline poly = new Polyline(link_points);
                double length = 0.0;
                double total_length = poly.Length;

                for (int i = 0; i < poly.Count - 1; ++i)
                {
                    Plane p = Util.InterpolatePlanes2(A, B, length / total_length);
                    Plane pnorm = new Plane(p);
                    pnorm.Transform(Transform.Rotation(p.ZAxis, normals[i], p.Origin));

                    double t = Math.Sin(length / total_length * Math.PI);

                    p = Util.InterpolatePlanes2(p, pnorm, t);


                    p.Origin = poly[i];
                    length += poly[i].DistanceTo(poly[i + 1]);
                    link_targets.Add(new Waypoint(p, (int)WaypointType.RAPID));
                }

            }

            return link_targets;
        }


    }

    public struct Waypoint
    {
        public Plane Plane;
        public int Type;
        //public Interpolation Interpolation;

        public static Waypoint Unset { get
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

        public static implicit operator Plane(Waypoint wp)
        {
            return wp.Plane;
        }

        public static implicit operator Waypoint(Plane p)
        {
            return new Waypoint(p);
        }

        public override bool Equals(object obj)
        {
            if (obj is Waypoint)
            {
                Waypoint wp = (Waypoint)obj;
                if (wp.Plane == this.Plane
                    //&& wp.Interpolation == this.Interpolation
                    && wp.Type == this.Type)
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"Waypoint ({this.Plane.ToString()}, {this.Type.ToString()}";
        }

        public override int GetHashCode()
        {
            return this.Plane.GetHashCode();
        }
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
        RAPID       = 1,
        FEED        = 0,
        PLUNGE      = 2,
        ARC_CW      = 4,
        ARC_CCW     = 12
    }


    /// <summary>
    /// Base class for toolpath post-processor. Inherit from this
    /// to create machine-specific posts.
    /// </summary>
    public abstract class MachinePost
    {
        // Dummy variables
        public string Name = "MachinePost";
        public string Author = "Author";
        public string Date = System.DateTime.Now.ToShortDateString();
        public string ProgramTime = "X";
        public double MaterialWidth = 0, MaterialHeight = 0, MaterialDepth = 0;

        public List<Toolpath> Paths = new List<Toolpath>();

        public abstract object Compute();
        public void AddPath(Toolpath p) => Paths.Add(p);
        public void AddPaths(ICollection<Toolpath> p) => Paths.AddRange(p);

    }

    /// <summary>
    /// Post to convert to Robots targets and program.
    /// </summary>
    public class RobotsPost : MachinePost
    {
        public override object Compute()
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// Post to convert to G-code for CMS 5-axis mill.
    /// </summary>
    public class CMSPost : MachinePost
    {

        #region CMS Antares G and M code descriptions

        //G CODES
        //   G0     - Rapid
        //   G1     - Feed
        //   G2     - Arc CW
        //   G3     - Arc CCW
        //   G40    - Cutter compensation OFF
        //   G43.4  - Cutter height offset (?)
        //   G49    - Cutter length offset OFF
        //   G5.1   - High speed
        //   G52    - Work coordinates offset
        //   G53    - Move in absolute coordinates
        //   G68.2  - Tilted work planes (https://www.linkedin.com/pulse/fanuc-g682-5-axis-tilted-work-planes-tim-markoski/)
        //   G69    - Coordinate rotation OFF
        //   G80    - Cancel canned cycles
        //   G90    - Absolute distance mode (using active coordinate system)
        //   G92.1  - Reset G92 offsets to 0

        //M CODES
        //   M05    - Spindle rotation stop
        //   M25    - Pressure clamping 1 closing
        //   M3     - Spindle CW rotation
        //   M31    - B axis unlocking
        //   M32    - B axis locking
        //   M33    - C axis unlocking
        //   M34    - C axis locking
        //   M6     - Tool-change activation
        //   M99    - End of subroutine
        #endregion

        public override object Compute()
        {
            List<string> Tools = new List<string>();
            for (int i = 0; i < Paths.Count; ++i)
            {
                if (!Tools.Contains(Paths[i].ToolName))
                    Tools.Add(Paths[i].ToolName);
            }

            List<string> Program = new List<string>();
            /*
            // Dummy variables
            string Name = "Test";
            string Author = "Tom";
            string Date = System.DateTime.Now.ToShortDateString();
            string ProgramTime = "Not too long";
            double MaterialWidth = 200, MaterialHeight = 500, MaterialDepth = 25;
            */

            bool HighSpeed = true;

            // Create headers
            Program.Add("%");
            Program.Add("O0001");
            Program.Add($"(File name      : {Name} )");
            Program.Add($"(Programmed by  : {Author} )");
            Program.Add($"(Date           : {Date} )");
            Program.Add($"(Program length : {ProgramTime} )");
            Program.Add($"(Material       : W{MaterialWidth} H{MaterialHeight} D{MaterialDepth} )");
            Program.Add("");
            Program.Add("");

            // Comment on tools
            Program.Add("( * * * * * TOOLS * * * * * )");
            Program.Add($"( Number ; Diameter ; Length ; Name )");
            for (int i = 0; i < Tools.Count; ++i)
            {
                double ToolDiameter = 12.0;
                double ToolLength = 55.0;

                Program.Add($"( {i} ; {ToolDiameter} ; {ToolLength} ; {Tools[i]} )");
            }

            Program.Add("");
            Program.Add("");

            Program.Add("( * * * * * START * * * * * )");

            // Init gcode
            Program.Add("G90 G40 G80 G49 G69");

            // Reset offsets to 0
            Program.Add("G92.1 X0 Y0 Z0 B0 C0");

            // CMS pressure clamping closing
            Program.Add("M25");

            // Set to mm (G20 is inches)
            Program.Add("G21");

            // Go home. G0 = rapid, G53 = move in absolute coords
            Program.Add("G0 G53 Z0");
            Program.Add("G0 B0 C0");

            // TODO: Check out the G codes in here...

            for (int i = 0; i < Paths.Count; ++i)
            {
                Toolpath TP = Paths[i];

                // Add toolpath info
                // TODO: Add support for tool indexing to the whole thing
                Program.Add("");
                Program.Add("");
                Program.Add($"( * * * * * PATH {i:D2} * * * * * )");

                Program.Add($"( Operation : {TP.Name} )");
                Program.Add($"( Tool no.  : {Tools.IndexOf(TP.ToolName)} )");
                Program.Add($"( Tool des. : {TP.ToolName} )");
                Program.Add($"( Tool dia. : {TP.ToolDiameter} )");

                // Tool change
                Program.Add($"M6 T{Tools.IndexOf(TP.ToolName)}");

                if (HighSpeed)
                    Program.Add("G5.1 Q1");

                Program.Add($"G43.4 H{Tools.IndexOf(TP.ToolName)}");


                // If toolpath is planar, lock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M32 M34");

                // Start spindle
                Program.Add($"M3 S{TP.SpindleSpeed}");
                Program.Add("");

                // Move to first waypoint
                Waypoint prev = TP.Paths[0][0];
                if (prev.Type != (int)WaypointType.RAPID)
                    throw new Exception("First waypoint must be rapid. Check code.");

                // Calculate B and C values
                double B, C, prevB, prevC;
                Vector3d axisFirst = -prev.Plane.ZAxis;
                axisFirst.Unitize();

                prevB = Rhino.RhinoMath.ToDegrees(Math.Acos(axisFirst * -Vector3d.ZAxis));
                prevC = Rhino.RhinoMath.ToDegrees(Math.Atan2(axisFirst.X, axisFirst.Y));

                Program.Add($"G{(int)prev.Type} X{prev.Plane.Origin.X:F3} Y{prev.Plane.Origin.Y:F3}  Z{prev.Plane.Origin.Z:F3} B{prevB:F3} C{prevC:F3}");


                // Go through waypoints

                for (int j = 0; j < TP.Paths.Count; ++j)
                {
                    // Parse sub-paths
                    Path Subpath = TP.Paths[j];
                    for (int k = 0; k < Subpath.Count; ++k)
                    {
                        Waypoint wp = Subpath[k];

                        // Calculate B and C values
                        Vector3d axis = -wp.Plane.ZAxis;
                        axis.Unitize();

                        B = Rhino.RhinoMath.ToDegrees(Math.Acos(axis * -Vector3d.ZAxis));
                        C = Rhino.RhinoMath.ToDegrees(Math.Atan2(axis.X, axis.Y));

                        // Compose line
                        List<string> Line = new List<string>();

                        if (wp.Type != prev.Type)
                        {
                            if ((wp.Type & 1) == 1)
                                Line.Add($"G0");
                            else if ((wp.Type & 4) == 1)
                                if ((wp.Type & 12) == 1)
                                    Line.Add($"G3");
                                else
                                    Line.Add($"G2");
                            else
                                Line.Add($"G1");

                            /*
                            switch ((WaypointType)wp.Type)
                            {
                                case (WaypointType.RAPID):
                                    Line.Add($"G0");
                                    break;
                                case (WaypointType.FEED):
                                    Line.Add($"G1");
                                    break;
                                case (WaypointType.PLUNGE):
                                    Line.Add($"G1");
                                    break;
                                case (WaypointType.ARC_CW):
                                    Line.Add($"G2");
                                    break;
                                case (WaypointType.ARC_CCW):
                                    Line.Add($"G3");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                            */
                        }

                        if (Math.Abs(wp.Plane.Origin.X - prev.Plane.Origin.X) > 0.00001)
                            Line.Add($"X{wp.Plane.Origin.X:F3}");

                        if (Math.Abs(wp.Plane.Origin.Y - prev.Plane.Origin.Y) > 0.00001)
                            Line.Add($"Y{wp.Plane.Origin.Y:F3}");

                        if (Math.Abs(wp.Plane.Origin.Z - prev.Plane.Origin.Z) > 0.00001)
                            Line.Add($"Z{wp.Plane.Origin.Z:F3}");

                        if (Math.Abs(B - prevB) > 0.00001)
                            Line.Add($"B{B:F3}");

                        if (Math.Abs(C - prevC) > 0.00001)
                            Line.Add($"C{C:F3}");

                        if ((wp.Type & 4) == 1)
                            Line.Add($"R0.0");

                        if ((wp.Type & 3) != (prev.Type & 3))
                            if ((wp.Type & 2) != 0)
                                Line.Add($"F{TP.PlungeRate}");
                            else if ((wp.Type & 2) == 0)
                                Line.Add($"F{TP.FeedRate}");

                        // Add line to program
                        Program.Add(string.Join(" ", Line));

                        // Update previous waypoint
                        prev = new Waypoint(wp);
                        prevB = B;
                        prevC = C;
                    }
                }

                // Stop spindle
                Program.Add("M5");


                // TODO: Find out what these G codes do
                Program.Add("G49 G53 G69");

                if (HighSpeed)
                    Program.Add("G5.1 Q0");

                // TODO: Add another home position here?

                // If toolpath is planar, unlock B and C axes
                if (TP.IsPlanar)
                    Program.Add($"M31 M33");


                Program.Add("");
                
            }

            // Return home
            // TODO: Look at example, find out if G53 is global coords
            // and add if necessary
            Program.Add("G0 Z0");
            Program.Add("G0 X-2600 Y0");

            Program.Add("( * * * * *  END  * * * * * )");

            Program.Add("M7"); // This should be to return the tool, but check
            Program.Add("");
            Program.Add("M99");
            Program.Add("%");


            return Program;
        }
    }
}
#endif