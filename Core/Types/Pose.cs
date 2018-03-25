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

using Rhino.Geometry;

namespace tas.Core.Types
{
    public class Pose
    {
        public Point3d Translation;
        public Quaternion Rotation;
        public double Fitness;

        /// <summary>
        /// Create random pose within radius of origin.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Pose Random(Point3d origin, double radius)
        {
            Pose pose = new Pose();
            Random rnd = new Random();
            Quaternion dir = Quaternion.Identity;
            dir.A = (rnd.NextDouble() - 0.5);
            dir.B = (rnd.NextDouble() - 0.5);
            dir.C = (rnd.NextDouble() - 0.5);
            dir.D = (rnd.NextDouble() - 0.5);
            dir.Unitize();

            Vector3d v = dir.Rotate(Vector3d.XAxis);
            pose.Translation = origin + v * rnd.NextDouble() * radius;

            Quaternion q = Quaternion.Identity;
            q.A = (rnd.NextDouble() - 0.5);
            q.B = (rnd.NextDouble() - 0.5);
            q.C = (rnd.NextDouble() - 0.5);
            q.D = (rnd.NextDouble() - 0.5);
            q.Unitize();

            pose.Rotation = q;

            return pose;
        }

        /// <summary>
        /// Create random pose within radius of origin, using initial orientation as starting point
        /// and constrained by rotation_spread.
        /// </summary>
        /// <param name="init_location">Seed location. Location will be randomized around this point.</param>
        /// <param name="location_spread">The spread radius. The location will be somewhere within this radius of init_location.</param>
        /// <param name="init_rotation">Seed rotation. Rotation will be ranodmized around this orientation.</param>
        /// <param name="rotation_spread">Same as location_spread except for rotation.</param>
        /// <returns>New Pose.</returns>
        public static Pose Random(Point3d init_location, double location_spread, Quaternion init_rotation, double rotation_spread)
        {
            Pose pose = new Pose();
            Random rnd = new Random();
            Quaternion dir = Quaternion.Identity;
            dir.A = (rnd.NextDouble() - 0.5);
            dir.B = (rnd.NextDouble() - 0.5);
            dir.C = (rnd.NextDouble() - 0.5);
            dir.D = (rnd.NextDouble() - 0.5);
            dir.Unitize();

            Vector3d v = dir.Rotate(Vector3d.XAxis);
            pose.Translation = init_location + v * rnd.NextDouble() * location_spread;

            Quaternion q = init_rotation;
            q.A += (rnd.NextDouble() - 0.5) * rotation_spread;
            q.B += (rnd.NextDouble() - 0.5) * rotation_spread;
            q.C += (rnd.NextDouble() - 0.5) * rotation_spread;
            q.D += (rnd.NextDouble() - 0.5) * rotation_spread;
            q.Unitize();

            pose.Rotation = q;

            return pose;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Pose() : this(Point3d.Origin, Quaternion.Identity)
        {
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="s">Pose to copy.</param>
        public Pose(Pose s)
        {
            Translation = s.Translation;
            Rotation = s.Rotation;
            Fitness = s.Fitness;
        }

        public Pose(double x, double y, double z, double q1, double q2, double q3, double q4) : 
            this(new Point3d(x, y, z), new Quaternion(q1, q2, q3, q4))
        {
        }

        public Pose(Point3d pos, Quaternion rot, double fitness = 0.0)
        {
            Translation = pos;
            Rotation = rot;
            Fitness = fitness;
        }

        /// <summary>
        /// Pose to Plane.
        /// </summary>
        /// <returns></returns>
        public Plane ToPlane()
        {
            Plane plane;
            Rotation.GetRotation(out plane);
            plane.Origin = Translation;
            return plane;
        }

        /// <summary>
        /// Pose to transformation matrix.
        /// </summary>
        /// <returns></returns>
        public Transform ToTransform()
        {
            return Transform.PlaneToPlane(Plane.WorldXY, ToPlane());
        }

        /// <summary>
        /// Perturb the pose by factors.
        /// </summary>
        /// <param name="t_fac">Amount to perturb Translation in a random direction.</param>
        /// <param name="r_fac">Amount to perturb Rotation randomly.</param>
        public void Perturb(double t_fac, double r_fac)
        {
            Random rnd = new Random();
            Perturb(t_fac, r_fac, rnd);
            return;
        }

        public void Perturb(double t_fac, double r_fac, Random rnd)
        {
            Point3d p = Translation;
            Quaternion dir = Quaternion.Identity;
            dir.A = (rnd.NextDouble() - 0.5);
            dir.B = (rnd.NextDouble() - 0.5);
            dir.C = (rnd.NextDouble() - 0.5);
            dir.D = (rnd.NextDouble() - 0.5);
            dir.Unitize();

            Vector3d v = dir.Rotate(Vector3d.XAxis);
            p = p + v * rnd.NextDouble() * t_fac;
            Translation = p;

            Quaternion q = Rotation;
            q.A += (rnd.NextDouble() - 0.5) * r_fac;
            q.B += (rnd.NextDouble() - 0.5) * r_fac;
            q.C += (rnd.NextDouble() - 0.5) * r_fac;
            q.D += (rnd.NextDouble() - 0.5) * r_fac;
            q.Unitize();

            Rotation = q;
        }

        public override string ToString()
        {
            string s = "Pose\n{\n";
            s += "   Translation(" + Translation.X.ToString() + " " + Translation.Y.ToString() + " " + Translation.Z.ToString() + ")\n";
            s += "   Rotation(" + Rotation.A.ToString() + " " + Rotation.B.ToString() + " " + Rotation.C.ToString() + " " + Rotation.D.ToString() + ")\n";
            s += "   Fitness(" + Fitness.ToString() + ")\n";
            s += "}";
            return s;
        }

    }

}
