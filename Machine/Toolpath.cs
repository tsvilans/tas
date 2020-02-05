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
using System.Linq;
using Rhino.Geometry;

using Path = System.Collections.Generic.List<tas.Machine.Waypoint>;

namespace tas.Machine
{

    public class Toolpath
    {
        public Guid Id { get; private set; }
        public List<Path> Paths;
        public MachineTool Tool = null;
        //public string ToolName;
        public string Name;

        public object Safety;
        public double SafeZ { get; set; }
        public double RapidZ { get; set; }
        public bool IsPlanar { get; set; }
        public bool FlipWrist;

        public Toolpath()
        {
            Id = Guid.NewGuid();
            Paths = new List<Path>();
        }

        public Toolpath(Toolpath tp)
        {
            Id = Guid.NewGuid();
            Paths = new List<Path>();

            Name = tp.Name;
            PlaneRetractVertical = tp.PlaneRetractVertical;
            RapidZ = tp.RapidZ;
            SafeZ = tp.SafeZ;
            Tool = tp.Tool;
            Safety = tp.Safety;
            IsPlanar = tp.IsPlanar;
            FlipWrist = tp.FlipWrist;

            for (int i = 0; i < tp.Paths.Count; ++i)
            {
                Path p = new Path();
                for (int j = 0; j < tp.Paths[i].Count; ++j)
                    p.Add(new Waypoint(tp.Paths[i][j]));

                Paths.Add(p);
            }
        }

        /// <summary>
        /// If true, the tool will retract along the safety plane's
        /// Z-axis vector to the plane. This only works if the Safety is a 
        /// plane, obviously.
        /// If false (default), the tool will retract along the axis of the 
        /// last target until it intersects the plane.
        /// </summary>
        public bool PlaneRetractVertical { get; set; }

        public void CreateLeadsAndLinks()
        {
            Waypoint LastTarget = new Waypoint();
            bool last = false;

            for (int i = 0; i < Paths.Count; ++i)
            {
                Path new_path = new Path();
                Plane p;
                if (Paths[i].Count < 1) continue;

                p = Paths[i][0].Plane;
                p.Origin = p.Origin + p.ZAxis * SafeZ;

                // first target retracted
                Waypoint temp = RetractToSafety(p);

                
                // add link if necessary
                if (last)
                    if (Safety is Mesh || Safety is Brep || Safety is Surface)
                        new_path.AddRange(LinkOnSafety(LastTarget, temp));
                
                new_path.Add(temp);
                new_path.Add(new Waypoint(p, (int)WaypointType.FEED));

                p = Paths[i][0].Plane;
                new_path.Add(new Waypoint(p, (int)WaypointType.PLUNGE));

                for (int j = 1; j < Paths[i].Count; ++j)
                {
                    new_path.Add(new Waypoint(Paths[i][j], Paths[i][j].Type));
                }

                p = Paths[i].Last().Plane;
                p.Origin = p.Origin + p.ZAxis * SafeZ;
                new_path.Add(new Waypoint(p, (int)WaypointType.FEED));


                temp = RetractToSafety(p);

                LastTarget = temp;
                last = true;

                new_path.Add(new Waypoint(temp, (int)WaypointType.RAPID));
                Paths[i] = new_path;
            }
        }

        public Waypoint RetractToSafety(Waypoint current)
        {
            if (Safety == null)
                throw new Exception("No safety defined!");

            Ray3d ray = new Ray3d(current.Plane.Origin, current.Plane.ZAxis);

            if (Safety is Plane)
            {
                if (PlaneRetractVertical)
                {
                    Plane p = current.Plane;
                    Plane sp = (Plane)Safety;
                    Vector3d v = sp.Origin - p.Origin;
                    p.Origin = p.Origin + sp.ZAxis * (sp.ZAxis * v);
                    Waypoint wp = new Waypoint(p, (int)WaypointType.RAPID);
                    return wp;
                }

                double t;
                Line line = new Line(current.Plane.Origin, current.Plane.ZAxis);
                if (Rhino.Geometry.Intersect.Intersection.LinePlane(line, (Plane)Safety, out t))
                {
                    Plane p = current.Plane;
                    p.Origin = line.PointAt(t);
                    Waypoint wp = new Waypoint(p, (int)WaypointType.RAPID);
                    return wp;
                }
            }
            else if (Safety is Mesh)
            {
                double d = Rhino.Geometry.Intersect.Intersection.MeshRay(Safety as Mesh, ray);
                Plane p = current.Plane;
                p.Origin = current.Plane.Origin + current.Plane.ZAxis * d;
                Waypoint wp = new Waypoint(p, (int)WaypointType.RAPID);
                return wp;
            }
            else if (Safety is GeometryBase)
            {
                Point3d[] pts = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, new GeometryBase[] { Safety as GeometryBase }, 1);
                if (pts.Length > 0)
                {
                    Plane p = current.Plane;
                    p.Origin = pts[0];
                    Waypoint wp = new Waypoint(p, (int)WaypointType.RAPID);
                    return wp;
                }
            }

            //throw new Exception("Failed to find correct Safety type...");
            return current;
        }

        public Path LinkOnSafety(Waypoint A, Waypoint B)
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

            Mesh m = null;

            if (Safety is Mesh)
                m = Safety as Mesh;
            else if (Safety is Brep)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(Safety as Brep, MeshingParameters.QualityRenderMesh);
                m = new Mesh();
                foreach (Mesh mesh in meshes)
                    m.Append(mesh);
            }
            else if (Safety is Surface)
            {
                Mesh[] meshes = Mesh.CreateFromBrep((Safety as Surface).ToBrep(), MeshingParameters.QualityRenderMesh);
                m = new Mesh();
                foreach (Mesh mesh in meshes)
                    m.Append(mesh);
            }
            else
                throw new NotImplementedException();

            Point3d point = A.Plane.Origin;
            MeshPoint mp = m.ClosestMeshPoint(point, step * 0.99);
            while (point.DistanceTo(B.Plane.Origin) > minD && counter < maxIter)
            {
                counter++;
                Vector3d toEnd = new Vector3d(B.Plane.Origin - mp.Point);
                Vector3d n = m.NormalAt(mp);
                Vector3d v = ProjectToPlane(toEnd, new Plane(mp.Point, n));
                v.Unitize();
                point = mp.Point + v * step;
                mp = m.ClosestMeshPoint(point, step / 2);
                //link_points.Add(mp.Point);
                //normals.Add(n);
            }

            Polyline poly = new Polyline(link_points);
            double length = 0.0;
            double total_length = poly.Length;

            for (int i = 0; i < poly.Count - 1; ++i)
            {
                Plane p = InterpolatePlanes2(A, B, length / total_length);
                Plane pnorm = new Plane(p);
                pnorm.Transform(Transform.Rotation(p.ZAxis, normals[i], p.Origin));

                double t = Math.Sin(length / total_length * Math.PI);

                p = InterpolatePlanes2(p, pnorm, t);


                p.Origin = poly[i];
                length += poly[i].DistanceTo(poly[i + 1]);
                link_targets.Add(new Waypoint(p, (int)WaypointType.RAPID));
            }

            return link_targets;
        }

        /// <summary>
        /// Better plane interpolation using quaternions.
        /// </summary>
        /// <param name="A">Plane A.</param>
        /// <param name="B">Plane B.</param>
        /// <param name="t">t-value.</param>
        /// <returns></returns>
        private static Plane InterpolatePlanes2(Plane A, Plane B, double t)
        {
            Quaternion qA = Quaternion.Rotation(Plane.WorldXY, A);
            Quaternion qB = Quaternion.Rotation(Plane.WorldXY, B);

            Quaternion qC = Slerp(qA, qB, t);
            Point3d p = InterpolatePoints(A.Origin, B.Origin, t);

            Plane plane;
            qC.GetRotation(out plane);
            plane.Origin = p;

            return plane;
        }

        /// <summary>
        /// Spherical interpolation using quaternions.
        /// </summary>
        /// <param name="qA">Quaternion A.</param>
        /// <param name="qB">Quaternion B.</param>
        /// <param name="t">t-value.</param>
        /// <returns></returns>
        private static Quaternion Slerp(Quaternion qA, Quaternion qB, double t)
        {
            if (t == 0) return qA;
            if (t == 1.0) return qB;

            Quaternion qC = new Quaternion();
            double cosHT = qA.A * qB.A + qA.B * qB.B + qA.C * qB.C + qA.D * qB.D;

            if (cosHT < 0.0)
            {
                qC.A = -qB.A;
                qC.B = -qB.B;
                qC.C = -qB.C;
                qC.D = -qB.D;
                cosHT = -cosHT;
            }
            else
                qC = qB;

            if (cosHT >= 1.0)
            {
                qC.A = qA.A;
                qC.B = qA.B;
                qC.C = qA.C;
                qC.D = qA.D;
                return qC;
            }
            double HT = Math.Acos(cosHT);
            double sinHT = Math.Sqrt(1.0 - cosHT * cosHT);

            if (Math.Abs(sinHT) < 0.001)
            {
                qC.A = 0.5 * (qA.A + qC.A);
                qC.B = 0.5 * (qA.B + qC.B);
                qC.C = 0.5 * (qA.C + qC.C);
                qC.D = 0.5 * (qA.D + qC.D);
                return qC;
            }

            double ratioA = Math.Sin((1 - t) * HT) / sinHT;
            double ratioB = Math.Sin(t * HT) / sinHT;

            qC.A = qA.A * ratioA + qC.A * ratioB;
            qC.B = qA.B * ratioA + qC.B * ratioB;
            qC.C = qA.C * ratioA + qC.C * ratioB;
            qC.D = qA.D * ratioA + qC.D * ratioB;
            return qC;
        }

        /// <summary>
        /// Simple linear interpolation between two points.
        /// </summary>
        /// <param name="A">Point A.</param>
        /// <param name="B">Point B.</param>
        /// <param name="t">t-value.</param>
        /// <returns></returns>
        private static Point3d InterpolatePoints(Point3d A, Point3d B, double t)
        {
            return A + t * (B - A);
        }

        /// <summary>
        /// Simple linear interpolation between two vectors.
        /// </summary>
        /// <param name="A">Vector A.</param>
        /// <param name="B">Vector B.</param>
        /// <param name="t">t-value.</param>
        /// <returns></returns>
        private static Vector3d InterpolateVectors(Vector3d A, Vector3d B, double t)
        {
            return A + t * (B - A);
        }

        /// <summary>
        /// Project point onto plane.
        /// </summary>
        /// <param name="p">Point to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected point.</returns>
        private static Point3d ProjectToPlane(Point3d p, Plane pl)
        {
            Vector3d op = new Vector3d(p - pl.Origin);
            double dot = Vector3d.Multiply(pl.ZAxis, op);
            Vector3d v = pl.ZAxis * dot;
            return new Point3d(p - v);
        }

        /// <summary>
        /// Project vector onto plane.
        /// </summary>
        /// <param name="v">Vector to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected vector.</returns>
        private static Vector3d ProjectToPlane(Vector3d v, Plane pl)
        {
            double dot = Vector3d.Multiply(pl.ZAxis, v);
            Vector3d v2 = pl.ZAxis * dot;
            return new Vector3d(v - v2);
        }

        /// <summary>
        /// Create lead-in or lead-out ramp from list of planes. Stolen from tasCore.Util...
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="pl"></param>
        /// <param name="height"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static Path CreateRamp(Path poly, Plane pl, double height, double length)//, ref string debug)
        {
            poly.Reverse();
            int N = poly.Count;
            if (poly.Last().Plane.Origin.DistanceTo(poly.First().Plane.Origin) < 0.001)
                N--;
            double th = 0.0;
            double td = 0.0;
            int i = 0;
            int next = 1;
            Path rpts = new Path();

            while (th < length)
            {
                // distance between i and next
                td = poly[next].Plane.Origin.DistanceTo(poly[i].Plane.Origin);

                if (th + td >= length)
                {
                    double t = ((th + td) - length) / td; // get t value for lerp
                    rpts.Add(poly[i]);
                    rpts.Add(InterpolatePlanes2(poly[i], poly[next], 1.0 - t));

                    break;
                }
                th += td;
                rpts.Add(poly[i]);
                i = (i + 1) % N;
                next = (i + 1) % N;
            }

            double L = 0.0;
            List<double> el = new List<double>();
            for (int j = 0; j < rpts.Count - 1; ++j)
            {
                double d = rpts[j].Plane.Origin.DistanceTo(rpts[j + 1].Plane.Origin);
                L += d;
                el.Add(d);
            }

            double LL = 0.0;
            for (int j = 1; j < rpts.Count; ++j)
            {
                LL += el[j - 1];

                double z = (LL / L) * height;
                Plane p = new Plane(rpts[j]);
                //p.Origin = p.Origin + pl.ZAxis * z;
                p.Origin = p.Origin + p.ZAxis * z;
                rpts[j] = p;
            }
            rpts.Reverse();

            return rpts;
        }

        public void CreateRamps(double height, double length)
        {
            for (int i = 0; i < Paths.Count; ++i)
            {
                Path ramp = CreateRamp(Paths[i], Paths[i].First(), height, length);
                Paths[i].RemoveAt(0);
                Paths[i].InsertRange(0, ramp);
            }
        }

        public Toolpath Duplicate() => new Toolpath(this);
    }

}
