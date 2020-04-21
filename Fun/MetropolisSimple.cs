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

namespace tas.Extra
{
    public class MetropolisSimple
    {
        Random rnd = new Random();
        public double MaxDist = 2000.0;
        public double MaxRot = 0.03;
        public double DistanceThreshold = 0.8;

        public List<List<double>> Samples = new List<List<double>>();
        public List<double> SampleValues = new List<double>();
        public double[] Maximums = new double[] { 100, 100, 100, 1, 1, 1, 1 };
        public double[] Minimums = new double[] { -100, -100, -100, -1, -1, -1, -1 };

        public string Log { get; private set; }


        // internal vars
        double[] current;
        Mesh mesh;
        List<Point3d> points;
        double max_dist, max_rot;

        /// <summary>
        /// Compute distance between proposed pose and the data. Needs work.
        /// </summary>
        /// <param name="n">Translation and rotation, encoded as a flattened array of 7 elements
        /// ([x, y, z, q1, q2, q3, q4]).</param>
        /// <returns></returns>
        double computeDistance(double[] n)
        {
            Point3d pos = new Point3d(n[0], n[1], n[2]);
            Plane p;
            Quaternion q = new Quaternion(n[3], n[4], n[5], n[6]);
            q.Unitize();
            q.GetRotation(out p);

            p.Origin = pos;

            Transform xform = Transform.PlaneToPlane(Plane.WorldXY, p);

            Mesh mm = mesh.DuplicateMesh();
            mm.Transform(xform);

            double res = 0.0;
            for (int i = 0; i < points.Count; ++i)
            {
                double d = mm.ClosestPoint(points[i]).DistanceTo(points[i]);
                res += d;
            }
            return 1 / ((res / points.Count) + 1.0);
        }

        public Plane Compute(List<Point3d> pts, Mesh m, int N)
        {
            Log = "";
            mesh = m;
            points = pts;

            // primary configuration (x, y, z, quat)
            current = new double[] { 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0 };
            double[] next = new double[7];

            max_dist = MaxDist;
            max_rot = MaxRot;

            double distance = computeDistance(current);
            double distanceNext = 0;

            Samples.Add(current.ToList());
            SampleValues.Add(distance);
            computeMinMax();

            double u = 0.0;
            int s = 1;

            /*
            for i in xrange(N):
              rn = r + np.random.normal(size = 2)
              pn = q(rn[0], rn[1])
              if pn >= p:
              p = pn
              r = rn
              else:
              u = np.random.rand()
              if u < pn / p:
              p = pn
              r = rn
              if i % s == 0:
              samples.append(r)
            */

            bool rejected = true;
            bool breaker = false;
            //double ratio = 1.0;

            for (int i = 0; i < N; ++i)
            {
                //ratio = 1.0 - ((double) i / N);
                //max_dist = MaxDist * ratio;
                //max_rot = MaxRot * ratio;

                rejected = true;
                computeNext(current, next);
                distanceNext = computeDistance(next);
                if (distanceNext >= distance)
                {
                    rejected = false;
                    assign(current, next);
                    distance = distanceNext;
                    if (distance > DistanceThreshold)
                    {
                        Log += "Distance threshold reached!\n";
                        breaker = true;
                    }
                }
                else
                {
                    u = rnd.NextDouble();
                    if (u < (distanceNext / distance))
                    {
                        assign(current, next);
                        distance = distanceNext;
                        rejected = false;
                    }
                }
                if (i % s == 0 && !rejected)
                {
                    Samples.Add(current.ToList());
                    SampleValues.Add(distance);
                    computeMinMax();
                }
                if (breaker) break;
            }

            try
            {
                int index = SampleValues.IndexOf(SampleValues.Max());
                current = Samples[index].ToArray();

                Log += "Max value  (" + SampleValues[index].ToString() + ") at " + index.ToString() + "\n";
                Log += "Actual max (" + SampleValues.Max().ToString() + ")\n";


                Log += "Final distance: " + distance.ToString() + "\n";
                Plane plane;
                Point3d pos = new Point3d(current[0], current[1], current[2]);
                Quaternion q = new Quaternion(current[3], current[4], current[5], current[6]);
                q.Unitize();
                q.GetRotation(out plane);

                plane.Origin = pos;
                return plane;
            }
            catch (Exception e)
            {
                Log += e.Message;
                return Plane.Unset;
            }
        }

        public void assign(double[] c, double[] n)
        {
            if (c.Length != n.Length)
                throw new Exception("Current and next arrays must be same length!");

            for (int i = 0; i < n.Length; ++i)
            {
                c[i] = n[i];
            }
        }

        void computeNext(double[] c, double[] n)
        {
            for (int i = 0; i < c.Length; i++)
                n[i] = c[i];

            n[0] += (rnd.NextDouble() - 0.5) * max_dist;
            n[1] += (rnd.NextDouble() - 0.5) * max_dist;
            n[2] += (rnd.NextDouble() - 0.5) * max_dist;

            Quaternion q = new Quaternion(n[3], n[4], n[5], n[6]);
            q.A += (rnd.NextDouble() - 0.5) * max_rot;
            q.B += (rnd.NextDouble() - 0.5) * max_rot;
            q.C += (rnd.NextDouble() - 0.5) * max_rot;
            q.D += (rnd.NextDouble() - 0.5) * max_rot;

            q.Unitize();

            n[3] = q.A;
            n[4] = q.B;
            n[5] = q.C;
            n[6] = q.D;

        }

        void computeMinMax()
        {
            for (int i = 0; i < 3; ++i)
            {
                Maximums[i] = Math.Max(Maximums[i], current[i]);
                Minimums[i] = Math.Min(Minimums[i], current[i]);
            }
        }
    }

}
