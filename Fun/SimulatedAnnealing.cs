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

using tas.Core.Types;

namespace tas.Extra
{
    public class SimulatedAnnealing
    {
        Random rnd = new Random();
        public double MaxDist = 2000.0;
        public double MaxRot = 0.03;
        public double FitnessThreshold = 20.0;
        public int Tries = 3;
        public int MutationThreshold = 2400;
        public double DistFactor = 0.8;
        public double RotFactor = 0.8;

        public Pose InitialPose = new Pose();
        public double Epsilon = 1E-3;
        public double Alpha = 0.999;
        public double InitTemperature = 10000.0;

        public List<Pose> Solutions = new List<Pose>();

        public double[] Maximums = new double[] { 0, 0, 0, 1, 1, 1, 1 };
        public double[] Minimums = new double[] { 0, 0, 0, -1, -1, -1, -1 };

        public string Log { get; private set; }

        // internal vars
        Pose current;
        Pose next;

        Mesh mesh;
        List<Point3d> points;
        double max_dist, max_rot;
        int stagnation = 0;

        public void Test()
        {
            Log += "\nBEGIN_TEST\n";
            current = new Pose();
            Log += current.ToString() + "\n";
            computeFitness(current);
            Log += current.ToString() + "\n";
            Log += "END_TEST\n\n";
        }

        void computeFitness(Pose ss)
        {
            Transform xform = ss.ToTransform();

            Mesh mm = mesh.DuplicateMesh();
            mm.Transform(xform);
            double res = 0.0;

            for (int i = 0; i < points.Count; ++i)
            {
                double d = mm.ClosestPoint(points[i]).DistanceTo(points[i]);
                res += d;
            }

            //double fitness = (1.0 - 1.0 / ((res / points.Count) + 1.0));// * InitTemperature;
            ss.Fitness = res / points.Count;// * InitTemperature;
        }

        public Pose Compute(List<Point3d> pts, Mesh m, int max_iter)
        {
            Log = "";
            mesh = m;
            points = pts;

            current = new Pose(InitialPose);
            next = new Pose();

            int iteration = -1;

            //the probability
            double proba;
            double alpha = Alpha;
            double temperature = InitTemperature;
            double delta;
            computeFitness(current);
            InitialPose = new Pose(current);

            Solutions.Add(new Pose(current));

            Log += "Initial fitness: " + current.Fitness.ToString() + "\n";

            max_dist = MaxDist;
            max_rot = MaxRot;
            double E = 0.0;

            //while the temperature did not reach epsilon
            while (temperature > Epsilon)
            {
                if (iteration > max_iter)
                {
                    Log += "Max iterations reached.\n";
                    break;
                }
                if (current.Fitness < FitnessThreshold)
                {
                    Log += "Fitness threshold reached.\n";
                    break;
                }

                iteration++;

                //get the next random permutation of values
                computeNext();
                computeMaxMin();
                //compute the fitness of the new permuted configuration
                computeFitness(next);
                delta = next.Fitness - current.Fitness;
                //if the new fitness is better accept it and assign it
                if (delta < 0)
                {
                    //Log += "Closer...\n";
                    current = new Pose(next);
                    Solutions.Add(new Pose(current));
                }
                else
                {
                    //continue;
                    proba = rnd.NextDouble();
                    //if the new distance is worse accept
                    //it but with a probability level
                    //if the probability is less than
                    //E to the power -delta/temperature.
                    //otherwise the old value is kept
                    E = Math.Exp(-delta / temperature);
                    if (proba < E)
                    {
                        current = new Pose(next);
                        Solutions.Add(new Pose(current));
                    }
                    //else
                    //    continue;
                    //else
                    //{
                    //    mutateDistances();
                    //}
                    //cooling process on every iteration

                }
                temperature *= alpha;

                //if (iteration % 200 == 0)
                //{
                //    Log += "   Pos: " + current.Translation.ToString() + "\n";
                //    Log += "   Rot: " + current.Rotation.ToString() + "\n";
                //    Log += "   Fit: " + current.Fitness.ToString() + "\n";
                //    Log += "   Eps: " + E.ToString() + "\n";
                //    Log += "   Del: " + delta.ToString() + "\n";
                //    Log += "   Tmp: " + temperature.ToString() + "\n";
                //}
            }

            try
            {
                Log += "Final fitness: " + current.Fitness.ToString() + "\n";
                Log += "Final temperature: " + temperature.ToString() + "\n";
                return current;
            }
            catch
            {
                return new Pose();
            }
        }

        public double GetFinalFitness()
        {
            return current.Fitness;
        }

        public Pose GetBestPose()
        {
            if (Solutions.Count < 1) return new Pose();
            List<Pose> sorted_solutions = new List<Pose>();
            foreach (Pose p in Solutions)
                sorted_solutions.Add(p);
            sorted_solutions.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
            return sorted_solutions[0];
        }

        void mutateDistances()
        {
            if (stagnation > MutationThreshold)
            {
                max_dist *= DistFactor;
                max_rot *= RotFactor;
                stagnation = 0;
                Log += "   new MaxDist: " + max_dist.ToString() + "\n";
                Log += "   new MaxRot: " + max_rot.ToString() + "\n";
            }
            else
                stagnation++;
        }

        void computeNext()
        {
            next = new Pose(current);
            next.Perturb(max_dist, max_rot, rnd);
            //Point3d p = next.Translation;

            //Quaternion dir = Quaternion.Identity;
            //dir.A = (rnd.NextDouble() - 0.5);
            //dir.B = (rnd.NextDouble() - 0.5);
            //dir.C = (rnd.NextDouble() - 0.5);
            //dir.D = (rnd.NextDouble() - 0.5);
            //dir.Unitize();

            //Vector3d v = dir.Rotate(Vector3d.XAxis);
            //p = p + v * rnd.NextDouble() * max_dist;
            //next.Translation = p;

            //Quaternion q = next.Rotation;
            //q.A += (rnd.NextDouble() - 0.5) * max_rot;
            //q.B += (rnd.NextDouble() - 0.5) * max_rot;
            //q.C += (rnd.NextDouble() - 0.5) * max_rot;
            //q.D += (rnd.NextDouble() - 0.5) * max_rot;
            //q.Unitize();

            //next.Rotation = q;
        }

        void computeMaxMin()
        {
            for (int i = 0; i < 3; ++i)
            {
                Maximums[i] = Math.Max(Maximums[i], current.Translation[i]);
                Minimums[i] = Math.Min(Minimums[i], current.Translation[i]);
            }
        }

    }

}
