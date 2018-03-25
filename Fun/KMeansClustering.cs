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
using System.Threading.Tasks;

using Rhino.Geometry;
using System.Collections.Concurrent;

using tas.Core;

namespace tas.Extra
{
    public class KMeansClustering<T>
    {
        public List<T> Centroids { get; private set; }
        public List<List<T>> Clusters { get; private set; }

        public double SettlingDistance;
        public int MaxIterations;
        public double MaxDistance;
        public double MinDistance;

        public Func<IEnumerable<T>, T> AverageFunction;
        public Func<T, T, double> DistanceFunction;

        public KMeansClustering()
        {
            SettlingDistance = 0.01;
            MaxIterations = 100;
            MaxDistance = 200.0;
            MinDistance = 50.0;
        }

        public void ClusterThreaded(IList<T> Objects, int K)
        {
            Mesh m = new Mesh();

            System.Random Rand = new Random();
            Centroids = new List<T>(K);
            Clusters = new List<List<T>>(K);
            List<int> Deltas = new List<int>();
            var RandomIndices = Util.GenerateRandom(K, 0, Objects.Count - 1);

            for (int i = 0; i < K; ++i)
            {
                Centroids.Add(Objects[RandomIndices[i]]);
                Clusters.Add(new List<T>());
                Deltas.Add(0);
            }

            int Iter = MaxIterations;

            List<ConcurrentBag<T>> Bags = new List<ConcurrentBag<T>>(K);
            for (int i = 0; i < K; ++i)
                Bags.Add(new ConcurrentBag<T>());

            for (int iter = 0; iter < MaxIterations; ++iter)
            {
                //Parallel.For(0, MaxIterations, iter =>
                //{

                //for (int i = 0; i < Clusters.Count; ++i)
                //    Clusters[i] = new List<T>();
                for (int i = 0; i < K; ++i)
                    Bags[i] = new ConcurrentBag<T>();

                Parallel.For(0, Objects.Count, index =>
                {
                    List<Tuple<double, int>> DistancePairs = new List<Tuple<double, int>>(Centroids.Count);
                    for (int j = 0; j < Centroids.Count; ++j)
                    {
                        DistancePairs.Add(new Tuple<double, int>(DistanceFunction(Objects[index], Centroids[j]), j));
                    }
                    var MinDistance = DistancePairs.OrderBy(x => x.Item1).First();

                    Bags[MinDistance.Item2].Add(Objects[index]);
                });
                
                /*

                for (int i = 0; i < Objects.Count; ++i)
                {
                    // SO. This zips up the loop index with the distance function 
                    // result (double), orders it in ascending order based
                    // on this distance function result, then takes the 
                    // first (smallest) value.
                    List<Tuple<double, int>> DistancePairs = new List<Tuple<double, int>>();
                    for (int j = 0; j < Centroids.Count; ++j)
                    {
                        DistancePairs.Add(new Tuple<double, int>(DistanceFunction(Objects[i], Centroids[j]), j));
                    }
                    var MinDistance = DistancePairs.OrderBy(x => x.Item1).First();
                    Bags[MinDistance.Item2].Add(Objects[i]);
                }*/


                for (int i = 0; i < K; ++i)
                    Centroids[i] = AverageFunction(Bags[i].ToList());
            //});
            }

            for (int i = 0; i < K; ++i)
                Clusters[i] = Bags[i].ToList();
        }

        public void Cluster(IList<T> Objects, int K)
        {
            System.Random Rand = new Random();
            Centroids = new List<T>(K);
            Clusters = new List<List<T>>(K);
            List<int> Deltas = new List<int>();
            var RandomIndices = Util.GenerateRandom(K, 0, Objects.Count - 1);

            for (int i = 0; i < K; ++i)
            {
                Centroids.Add(Objects[RandomIndices[i]]);
                Clusters.Add(new List<T>());
                Deltas.Add(0);
            }

            int Iter = MaxIterations;



            for (int iter = 0; iter < MaxIterations; ++iter)
            {
                for (int i = 0; i < Clusters.Count; ++i)
                    Clusters[i] = new List<T>();
                
                for (int i = 0; i < Objects.Count; ++i)
                {
                    // SO. This zips up the loop index with the distance function 
                    // result (double), orders it in ascending order based
                    // on this distance function result, then takes the 
                    // first (smallest) value.
                    List<Tuple<double, int>> DistancePairs = new List<Tuple<double, int>>();
                    for (int j = 0; j < Centroids.Count; ++j)
                    {
                        DistancePairs.Add(new Tuple<double, int>(DistanceFunction(Objects[i], Centroids[j]), j));
                    }
                    var MinDistance = DistancePairs.OrderBy(x => x.Item1).First();
                    Clusters[MinDistance.Item2].Add(Objects[i]);
                }
                

                for (int i = 0; i < K; ++i)
                {
                    Centroids[i] = AverageFunction(Clusters[i].ToList());
                }
            }

            for (int i = 0; i < K; ++i)
            {
                Clusters[i] = Clusters[i].ToList();
            }
        }

        public void ClusterAdaptive(IList<T> Objects, int K)
        {
            System.Random Rand = new Random();
            Centroids = new List<T>();
            Clusters = new List<List<T>>();
            List<int> Deltas = new List<int>();

            var RandomIndices = Util.GenerateRandom(K, 0, Objects.Count - 1);

            for (int i = 0; i < K; ++i)
            {
                Centroids.Add(Objects[RandomIndices[i]]);
                Clusters.Add(new List<T>());
                Deltas.Add(0);
            }

            int Iter = 0;

            while (true)
            {
                Iter++;
                for (int i = 0; i < Clusters.Count; ++i)
                    Clusters[i] = new List<T>();
                for (int i = 0; i < Objects.Count; ++i)
                {
                    // SO. This zips up the loop index with the distance function 
                    // result (double), orders it in ascending order based
                    // on this distance function result, then takes the 
                    // first (smallest) value.
                    //var MinDistance = Centroids.Select(x => new Tuple<double, int>(DistanceFunction(Objects[i], x), i)).OrderBy(x => x.Item1).First();

                    List<Tuple<double, int>> DistancePairs = new List<Tuple<double, int>>();
                    for (int j = 0; j < Centroids.Count; ++j)
                    {
                        DistancePairs.Add(new Tuple<double, int>(DistanceFunction(Objects[i], Centroids[j]), j));
                    }
                    var MinDistance = DistancePairs.OrderBy(x => x.Item1).First();
                    Clusters[MinDistance.Item2].Add(Objects[i]);
                }
                for (int i = 0; i < Clusters.Count; ++i)
                {
                    Centroids[i] = AverageFunction(Clusters[i]);
                }

                if (Iter > 4)
                {
                    for (int i = Centroids.Count - 1; i > 0; --i)
                    {
                        for (int j = 0; j < i; ++j)
                        {
                            double d = DistanceFunction(Centroids[i], Centroids[j]);
                            if (d < MinDistance)
                            {
                                Clusters[j].AddRange(Clusters[i]);
                                Centroids[j] = AverageFunction(new T[] { Centroids[i], Centroids[j] });
                                Clusters.RemoveAt(i);
                                Centroids.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                // Safety mechanism. Maybe only for testing.
                if (Iter > MaxIterations)
                    break;

                // If it's generally settled, then abort.
                //if (Deltas.Min() < SettlingDistance)
                //    break;
            }
            
        }
    }

}