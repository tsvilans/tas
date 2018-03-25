using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

using Rhino.Geometry;
using Rhino;
using System.Diagnostics;
using tas.Core;

namespace tas.Core.Legacy
{


    public class NetworkLegacy
    {
        public enum BranchCondition
        {
            SplitToLeft,
            SplitToRight,
            MergeFromLeft,
            MergeFromRight
        }

        public static double MeshMaxDistance = 100.0;
        public static bool EnforceContinuityInPairs = true;
        public static double NodeMergeDistance = 5.0;
        public List<NodeLegacy> Nodes;
        public List<EdgeLegacy> Edges;
        public List<Chain> Chains;

        #region Constructors

        public NetworkLegacy()
        {
            Nodes = new List<NodeLegacy>();
            Edges = new List<EdgeLegacy>();
            Chains = new List<Chain>();
        }

        public NetworkLegacy(Mesh m, bool Branching = false) : this()
        {

            for (int i = 0; i < m.TopologyVertices.Count; ++i)
            {
                NodeLegacy n;
                if (Branching && m.TopologyVertices.ConnectedTopologyVertices(i).Length == 3)
                    n = new BranchingNode();
                else if (m.TopologyVertices.ConnectedTopologyVertices(i).Length == 1)
                    n = new FootNode();
                else
                    n = new NodeLegacy();

                MeshPoint mp = m.ClosestMeshPoint(m.TopologyVertices[i], MeshMaxDistance);
                n.Frame = new Plane(mp.Point, m.NormalAt(mp));
                Nodes.Add(n);
            }

            for (int i = 0; i < m.TopologyEdges.Count; ++i)
            {
                EdgeLegacy e = new EdgeLegacy();
                e.Ends = m.TopologyEdges.GetTopologyVertices(i);
                Edges.Add(e);
            }

            Edges = new HashSet<EdgeLegacy>(Edges).ToList();
            BuildNodeData(false, false);
            BuildBranchingData();
        }

        public NetworkLegacy(List<Line> NetworkLines, Mesh M = null, bool Branching = false) : this()
        {
            // Init variables
            Point3d[] RawPoints = new Point3d[NetworkLines.Count * 2];
            int[] RawPointRemapping = new int[NetworkLines.Count * 2];
            bool[] RemappingFlags = new bool[NetworkLines.Count * 2];
            Tuple<int, int>[] RawEdges = new Tuple<int, int>[NetworkLines.Count];

            // Fill raw data lists
            int ii;
            for (int i = 0; i < NetworkLines.Count; ++i)
            {
                ii = i * 2;
                RawPoints[ii] = NetworkLines[i].From;
                RawPoints[ii + 1] = NetworkLines[i].To;
                RawEdges[i] = new Tuple<int, int>(ii, ii + 1);
            }

            // TODO: speed up dupli matching w/ RTree
            //RTree tree = new RTree();

            // Remap vertices to get rid of duplicates
            List<Point3d> Points = new List<Point3d>();
            int Index = 0;
            for (int i = 0; i < RawPoints.Length; ++i)
            {
                if (RemappingFlags[i])
                {
                    RemappingFlags[i] = true;
                    continue;
                }
                else
                {
                    RawPointRemapping[i] = Index;
                    Point3d p = RawPoints[i];
                    Points.Add(p);

                    //MeshPoint mp = M.ClosestMeshPoint(p, 500.0);

                    // Construct node for each point, using Mesh for
                    // node frame construction
                    NodeLegacy n = new NodeLegacy();
                    //n.Frame = new Plane(p, M.NormalAt(mp));
                    n.Frame = new Plane(p, Vector3d.ZAxis);
                    Nodes.Add(n);

                }

                for (int j = i; j < RawPoints.Length; ++j)
                {
                    if (RemappingFlags[j]) continue;
                    if (RawPoints[j].DistanceTo(RawPoints[i]) < NodeMergeDistance)
                    {
                        RawPointRemapping[j] = Index;
                        RemappingFlags[j] = true;
                    }
                }
                ++Index;
            }

            // Remap edge indices

            for (int i = 0; i < RawEdges.Length; ++i)
            {
                RawEdges[i] = new Tuple<int, int>(
                  RawPointRemapping[RawEdges[i].Item1],
                  RawPointRemapping[RawEdges[i].Item2]
                  );
            }


            // Construct network edges
            List<Line> NewLines = new List<Line>();

            //RawEdges = RawEdges.Distinct(new RawEdgeComparer()).ToArray();

            for (int i = 0; i < RawEdges.Length; ++i)
            {
                int a = RawEdges[i].Item1;
                int b = RawEdges[i].Item2;

                Line l = new Line(Points[a], Points[b]);
                NewLines.Add(l);
                EdgeLegacy e = new EdgeLegacy();
                e.Ends = new IndexPair(a, b);
                e.Curve = l.ToNurbsCurve();
                Edges.Add(e);
            }


            if (M != null)
            {
                try
                {
                    if (M.FaceNormals == null || M.FaceNormals.Count < 1)
                        M.FaceNormals.ComputeFaceNormals();
                    if (M.Normals == null || M.Normals.Count < 1)
                        M.Normals.ComputeNormals();

                    foreach (NodeLegacy n in Nodes)
                    {
                        MeshPoint mp = M.ClosestMeshPoint(n.Frame.Origin, MeshMaxDistance);
                        if (mp == null) continue;
                        n.Frame = new Plane(n.Frame.Origin, M.NormalAt(mp));
                    }
                }
                catch
                {
                    Debug.WriteLine("Something wrong with the mesh...");
                }
            }

            BuildNodeData(false, true);

            if (Branching)
            {
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    if (Nodes[i].Edges.Count == 3)
                    {
                        BranchingNode bn = new BranchingNode();
                        bn.Edges = Nodes[i].Edges;
                        bn.Frame = Nodes[i].Frame;
                        bn.Chains = Nodes[i].Chains;

                        //bn.SortEdgesLR(Nodes[i].Frame.ZAxis);

                        Nodes[i] = bn;
                    }
                }

                BuildBranchingData();
            }


        }

        #endregion

        #region Load /Save

        public bool Save(string Path)
        {
            XDocument doc = new XDocument();
            XElement root = new XElement("network");

            XElement edges = new XElement("edges");

            for (int i = 0; i < Edges.Count; ++i)
            {
                XElement edge = new XElement("edge");
                edge.SetAttributeValue("index", i);
                edge.SetAttributeValue("end1", Edges[i].Ends.I);
                edge.SetAttributeValue("end2", Edges[i].Ends.J);

                edges.Add(edge);
            }

            XElement nodes = new XElement("nodes");

            for (int i = 0; i < Nodes.Count; ++i)
            {
                XElement node = new XElement("node");
                node.SetAttributeValue("index", i);
                node.SetAttributeValue("type", Nodes[i].ToString());
                XElement node_edges = new XElement("node_edges");

                for (int j = 0; j < Nodes[i].Edges.Count; ++j)
                {
                    XElement node_edge = new XElement("node_edge");
                    node_edge.SetAttributeValue("index", Nodes[i].Edges[j].Index);
                    node_edge.SetAttributeValue("vX", Nodes[i].Edges[j].Vector.X);
                    node_edge.SetAttributeValue("vY", Nodes[i].Edges[j].Vector.Y);
                    node_edge.SetAttributeValue("vZ", Nodes[i].Edges[j].Vector.Z);
                    node_edge.SetAttributeValue("weight", Nodes[i].Edges[j].Weight);
                    node_edge.SetAttributeValue("user_data", Nodes[i].Edges[j].UserData);

                    node_edges.Add(node_edge);
                }

                node.Add(node_edges);

                XElement frame = new XElement("frame");
                frame.SetAttributeValue("PosX", Nodes[i].Frame.OriginX);
                frame.SetAttributeValue("PosY", Nodes[i].Frame.OriginY);
                frame.SetAttributeValue("PosZ", Nodes[i].Frame.OriginZ);
                frame.SetAttributeValue("XX", Nodes[i].Frame.XAxis.X);
                frame.SetAttributeValue("XY", Nodes[i].Frame.XAxis.Y);
                frame.SetAttributeValue("XZ", Nodes[i].Frame.XAxis.Z);
                frame.SetAttributeValue("YX", Nodes[i].Frame.YAxis.X);
                frame.SetAttributeValue("YY", Nodes[i].Frame.YAxis.Y);
                frame.SetAttributeValue("YZ", Nodes[i].Frame.YAxis.Z);

                if (Nodes[i] is BranchingNode)
                {
                    BranchingNode bn = Nodes[i] as BranchingNode;

                    XElement branch_weights = new XElement("branch_weights");
                    for (int j = 0; j < bn.BranchWeights.Count; ++j)
                    {
                        XElement branch_weight = new XElement("branch_weight");
                        branch_weight.SetAttributeValue("index", j);
                        branch_weight.Value = bn.BranchWeights[j].ToString();

                        branch_weights.Add(branch_weight);
                    }

                    node.Add(branch_weights);
                }

                node.Add(frame);

                nodes.Add(node);
            }

            root.Add(edges);
            root.Add(nodes);

            doc.Add(root);

            doc.Save(Path);

            return true;
        }

        public static NetworkLegacy Load(string Path)
        {
            if (!System.IO.File.Exists(Path))
                throw new Exception("Network::Load: File does not exist!");

            XDocument doc = XDocument.Load(Path);
            if (doc == null)
                throw new Exception("Network::Load: Failed to load file.");

            XElement root = doc.Root;
            if (root.Name != "network")
                throw new Exception("Network::Load: Not a valid Network file.");

            NetworkLegacy net = new NetworkLegacy();

            XElement nodes = root.Element("nodes");
            if (nodes != null)
            {
                var node_list = nodes.Elements();
                foreach (XElement node in node_list)
                {
                    if (node.Name != "node")
                        continue;

                    NodeLegacy n;

                    string type = node.Attribute("type").Value;
                    if (type == "FootNode")
                        n = new FootNode();
                    else if (type == "BranchingNode")
                    {
                        n = new BranchingNode();
                        BranchingNode bn = n as BranchingNode;

                        if (bn == null) continue;

                        XElement branch_weights = node.Element("branch_weights");
                        if (branch_weights != null)
                        {
                            var branch_weight_list = branch_weights.Elements();

                            foreach (XElement bw in branch_weight_list)
                            {
                                if (bw.Name == "branch_weight")
                                    bn.BranchWeights.Add(Convert.ToInt32(bw.Value));
                            }
                        }
                    }
                    else
                        n = new NodeLegacy();


                    XElement node_edges = node.Element("node_edges");
                    var edge_list = node_edges.Elements("node_edge");

                    foreach (XElement ne in edge_list)
                    {
                        double x = Convert.ToDouble(ne.Attribute("vX").Value);
                        double y = Convert.ToDouble(ne.Attribute("vY").Value);
                        double z = Convert.ToDouble(ne.Attribute("vZ").Value);

                        n.Edges.Add(new NodeInterface(new Vector3d(x, y, z), Convert.ToInt32(ne.Attribute("index").Value)));
                    }

                    XElement frame = node.Element("frame");
                    double fx = Convert.ToDouble(frame.Attribute("PosX").Value);
                    double fy = Convert.ToDouble(frame.Attribute("PosY").Value);
                    double fz = Convert.ToDouble(frame.Attribute("PosZ").Value);

                    double fxx = Convert.ToDouble(frame.Attribute("XX").Value);
                    double fxy = Convert.ToDouble(frame.Attribute("XY").Value);
                    double fxz = Convert.ToDouble(frame.Attribute("XZ").Value);

                    double fyx = Convert.ToDouble(frame.Attribute("YX").Value);
                    double fyy = Convert.ToDouble(frame.Attribute("YY").Value);
                    double fyz = Convert.ToDouble(frame.Attribute("YZ").Value);

                    n.Frame = new Plane(new Point3d(fx, fy, fz), new Vector3d(fxx, fxy, fxz), new Vector3d(fyx, fyy, fyz));

                    net.Nodes.Add(n);
                }
            }

            XElement edges = root.Element("edges");
            if (edges != null)
            {
                var edge_list = edges.Elements("edge");
                foreach (XElement edge in edge_list)
                {
                    int i = Convert.ToInt32(edge.Attribute("end1").Value);
                    int j = Convert.ToInt32(edge.Attribute("end2").Value);

                    EdgeLegacy e = new EdgeLegacy();
                    e.Ends = new IndexPair(i, j);

                    net.Edges.Add(e);
                }
            }

            return net;
        }

        #endregion

        /// <summary>
        /// RawEdgeComparer class for Network constructor
        /// </summary>
        private class RawEdgeComparer : IEqualityComparer<Tuple<int, int>>
        {
            public bool Equals(Tuple<int, int> a, Tuple<int, int> b)
            {
                return ((a.Item1 == b.Item1 && a.Item2 == b.Item2) ||
                  (a.Item1 == b.Item2 && a.Item2 == b.Item1));
            }

            public int GetHashCode(Tuple<int, int> a)
            {
                return (a.Item1 ^ a.Item2 * a.Item1 - a.Item2) / (a.Item1 + 1);
            }
        }

        /// <summary>
        /// EdgeComparer class for Network
        /// </summary>
        private class EdgeLegacyComparer : IEqualityComparer<EdgeLegacy>
        {
            public bool Equals(EdgeLegacy a, EdgeLegacy b)
            {
                return ((a.Ends.I == b.Ends.I && a.Ends.J == b.Ends.J) ||
                  (a.Ends.I == b.Ends.J && a.Ends.J == b.Ends.I));
            }

            public int GetHashCode(EdgeLegacy a)
            {
                return (a.Ends.I ^ a.Ends.J * a.Ends.I - a.Ends.J) / (a.Ends.I + 1);
            }
        }

        /// <summary>
        /// Tests the chain edge list for continuity (Consecutive edges share nodes). 
        /// </summary>
        /// <param name="index">Index of chain to check for continuity.</param>
        /// <returns>-1 if continuous, otherwise index of the first discontinuity.</returns>
        public int CheckContinuity(int index)
        {
            for (int i = 0; i < Chains[index].Edges.Count - 1; ++i)
            {
                if ((Edges[Chains[index].Edges[i]].Ends.I != Edges[Chains[index].Edges[i + 1]].Ends.I)
                    && (Edges[Chains[index].Edges[i]].Ends.J != Edges[Chains[index].Edges[i + 1]].Ends.I)
                    && (Edges[Chains[index].Edges[i]].Ends.I != Edges[Chains[index].Edges[i + 1]].Ends.J)
                    && (Edges[Chains[index].Edges[i]].Ends.J != Edges[Chains[index].Edges[i + 1]].Ends.J))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Extract joined curve geometry from whole chain.
        /// </summary>
        /// <param name="i">Index of chain to get.</param>
        /// <returns>Curve geometry.</returns>
        public Curve GetCurveGeometry(int i)
        {
            List<Curve> Curves = Chains[i].Edges.Select(x => Edges[x].Curve).ToList();
            //return Curves[0];

            Curve[] JoinedCurves = Curve.JoinCurves(Curves);
            if (JoinedCurves.Length < 1) return null;
            else return JoinedCurves[0];
        }

        /// <summary>
        /// Build node data, including edge connectivity, edge vectors, etc.
        /// </summary>
        /// <param name="CalcFrames">Calculate node frames based on the average normal of edge vectors.</param>
        /// <param name="ProjectVectors">Project edge vectors onto the node frame (flatten).</param>
        public void BuildNodeData(bool CalcFrames = false, bool ProjectVectors = false)
        {
            for (int i = 0; i < Nodes.Count; ++i)
            {
                Nodes[i].Edges.Clear();
            }

            for (int e = 0; e < Edges.Count; ++e)
            {
                int ei = Edges[e].Ends.I;
                int ej = Edges[e].Ends.J;

                Vector3d v = new Vector3d(Nodes[ei].Frame.Origin - Nodes[ej].Frame.Origin);
                v.Unitize();

                NodeInterface niI = new NodeInterface(-v, e);
                Nodes[ei].Edges.Add(niI);

                NodeInterface niJ = new NodeInterface(v, e);
                Nodes[ej].Edges.Add(niJ);
            }

            // Calculate foot / end nodes
            for (int i = 0; i < Nodes.Count; ++i)
            {
                if (Nodes[i].Edges.Count == 1)
                {
                    FootNode fn = new FootNode();
                    fn.Frame = Nodes[i].Frame;
                    fn.Edges = Nodes[i].Edges;
                    fn.Chains = Nodes[i].Chains;
                    Nodes[i] = fn;
                }
            }

            if (CalcFrames)
            {
                foreach (NodeLegacy n in Nodes)
                {
                    n.CalculateAverageFrame();
                }
            }

            if (ProjectVectors)
            {
                foreach (NodeLegacy n in Nodes)
                {
                    if (n is FootNode) continue;
                    n.ProjectEdgeVectors();
                }
            }

            if (EnforceContinuityInPairs)
            {
                foreach (NodeLegacy n in Nodes)
                {
                    if (n.Edges.Count == 2)
                    {
                        Vector3d av = n.Edges[0].Vector - n.Edges[1].Vector;
                        av.Unitize();

                        n.Edges[0].Vector = av;
                        n.Edges[1].Vector = -av;
                    }
                }
            }
        }

        public void BuildBranchingData()
        {
            for (int i = 0; i < Nodes.Count; ++i)
            {
                if (Nodes[i] is BranchingNode)
                {
                    BranchingNode bn = Nodes[i] as BranchingNode;
                    double[] DistValues = new double[bn.Edges.Count];
                    for (int j = 0; j < bn.Edges.Count; ++j)
                    {
                        for (int k = 0; k < bn.Edges.Count; ++k)
                        {
                            if (j == k) continue;
                            double dot = bn.Edges[j].Vector * bn.Edges[k].Vector;
                            DistValues[j] += -dot;
                        }
                    }

                    int index = Array.IndexOf(DistValues, DistValues.Max());
                    bn.TrunkIndex = index;
                    bn.SortEdgesLR(bn.Frame.ZAxis);

                    Vector3d TrunkProj = bn.Trunk().Vector.ProjectToPlane(bn.Frame);
                    double angle = Vector3d.VectorAngle(bn.Frame.YAxis, TrunkProj);
                    double xangle = bn.Frame.XAxis * TrunkProj;
                    if (xangle < 0)
                        bn.Frame.Transform(Transform.Rotation(angle, bn.Frame.ZAxis, bn.Frame.Origin));
                    else
                        bn.Frame.Transform(Transform.Rotation(-angle, bn.Frame.ZAxis, bn.Frame.Origin));
                }
            }
        }

        /// <summary>
        /// Crawls the network for a chain, beginning at edge i. Basically, tries to find an edge loop.
        /// </summary>
        /// <param name="i">Index of edge to crawl from.</param>
        /// <param name="limit">Angle limit between the current edge and the next one
        /// at a node junction. If an edge vector at a node is less than this, it is continuous
        /// and will proceed along that path.</param>
        /// <returns>Returns a list of node indices that represent a continuous chain.</returns>
        public List<int> CrawlForChain(int i, double limit)
        {
            Random rand = new Random();
            List<int> forward = new List<int>();
            NodeLegacy n;

            int ei = i;
            EdgeLegacy crawler = Edges[ei];
            int vi = crawler.Ends.J; // node index

            bool ok = true;

            int counter = 100;

            // crawl forward
            while (crawler != null && counter > 0)
            {
                forward.Add(ei);
                n = Nodes[vi];

                int nei = n.Edges.Select(x => x.Index).ToList().IndexOf(ei);
                if (nei < 0)
                {
                    break;
                }

                ok = false;
                List<int> possible_routes = new List<int>();
                for (int ne = 0; ne < n.Edges.Count; ++ne)
                {
                    if (ne == nei) continue;

                    double dot = n.Edges[nei].Vector * n.Edges[ne].Vector;

                    if (dot < -limit)
                    {
                        possible_routes.Add(ne);
                    }
                }

                if (possible_routes.Count > 0)
                {
                    int next = rand.Next(0, possible_routes.Count);
                    ok = true;
                    crawler = Edges[n.Edges[possible_routes[next]].Index];
                    ei = n.Edges[possible_routes[next]].Index;
                    if (crawler.Ends.I == vi) vi = crawler.Ends.J;
                    else vi = crawler.Ends.I;
                }
                if (ei == i) break;
                if (!ok) crawler = null;
                counter--;
            }

            // go the other way...
            List<int> backward = new List<int>();

            ei = i;
            crawler = Edges[ei];
            vi = crawler.Ends.I; // node index

            ok = true;

            counter = 100;

            while (crawler != null && counter > 0)
            {
                backward.Add(ei);
                n = Nodes[vi];

                int nei = n.Edges.Select(x => x.Index).ToList().IndexOf(ei);
                if (nei < 0)
                {
                    break;
                }

                ok = false;
                List<int> possible_routes = new List<int>();

                for (int ne = 0; ne < n.Edges.Count; ++ne)
                {
                    if (ne == nei) continue;

                    double dot = n.Edges[nei].Vector * n.Edges[ne].Vector;

                    if (dot < -limit)
                    {
                        ok = true;
                        crawler = Edges[n.Edges[ne].Index];
                        ei = n.Edges[ne].Index;
                        if (crawler.Ends.I == vi) vi = crawler.Ends.J;
                        else vi = crawler.Ends.I;

                        break;
                    }
                }

                if (possible_routes.Count > 0)
                {
                    int next = rand.Next(0, possible_routes.Count);
                    ok = true;
                    crawler = Edges[n.Edges[possible_routes[next]].Index];
                    ei = n.Edges[possible_routes[next]].Index;
                    if (crawler.Ends.I == vi) vi = crawler.Ends.J;
                    else vi = crawler.Ends.I;
                }

                if (ei == i) break;

                if (!ok) crawler = null;
                counter--;
            }
            backward.Reverse();
            forward.InsertRange(0, backward.GetRange(0, backward.Count - 1));

            return forward;
        }

        /// <summary>
        /// Aligns edge vectors at nodes along the chains to ensure continuity.
        /// </summary>
        public void RelaxNodesAlongChains()
        {
            // TODO: find where chains have overlapping continuous bits and how to manage that...

            foreach (Chain ch in Chains)
            {
                for (int i = 0; i < ch.Edges.Count - 1; ++i)
                {
                    int ei1 = Nodes[ch.Vertices[i + 1]].Edges.Select(x => x.Index).ToList().IndexOf(ch.Edges[i]);
                    int ei2 = Nodes[ch.Vertices[i + 1]].Edges.Select(x => x.Index).ToList().IndexOf(ch.Edges[i + 1]);

                    Vector3d av = Nodes[ch.Vertices[i + 1]].Edges[ei1].Vector - Nodes[ch.Vertices[i + 1]].Edges[ei2].Vector;
                    av.Unitize();
                    Nodes[ch.Vertices[i + 1]].Edges[ei1].Vector = av;
                    Nodes[ch.Vertices[i + 1]].Edges[ei2].Vector = -av;
                }
            }
        }

        /// <summary>
        /// Identifies chains that have N or more overlapping edges.
        /// </summary>
        /// <param name="N">Minimum number of overlapping edges to look for.</param>
        /// <returns></returns>
        public List<Tuple<int, int>> FindOverlappingChains(int N)
        {
            List<Tuple<int, int>> Indices = new List<Tuple<int, int>>();
            for (int i = 0; i < Chains.Count; ++i)
                for (int j = i + 1; j < Chains.Count; ++j)
                    for (int k = 0; k < Chains[j].Edges.Count - N + 1; ++k)
                        if (Chains[i].Edges.ContainsPattern(Chains[j].Edges.GetRange(k, N)))
                        {
                            Indices.Add(new Tuple<int, int>(i, j));
                            break;
                        }

            for (int i = 0; i < Chains.Count; ++i)
            {
                List<int> ReversedList = Chains[i].Edges;
                ReversedList.Reverse();
                for (int j = i + 1; j < Chains.Count; ++j)
                    for (int k = 0; k < Chains[j].Edges.Count - N + 1; ++k)
                        if (ReversedList.ContainsPattern(Chains[j].Edges.GetRange(k, N)))
                        {
                            Indices.Add(new Tuple<int, int>(i, j));
                            break;
                        }
            }
            return Indices;
        }

        public NodeLegacy GetNextNode(int NodeIndex, int NodeInterfaceIndex, out int EdgeIndex, out int NewNodeIndex)
        {
            EdgeIndex = Nodes[NodeIndex].Edges[NodeInterfaceIndex].Index;
            EdgeLegacy e = Edges[EdgeIndex];
            if (e.Ends.I == NodeIndex)
            {
                NewNodeIndex = e.Ends.J;
                return Nodes[NewNodeIndex];

            }
            NewNodeIndex = e.Ends.I;
            return Nodes[NewNodeIndex];
        }

        public List<Line> GetLines()
        {
            List<Line> lines = new List<Line>();

            for (int i = 0; i < Edges.Count; ++i)
            {
                Line l = new Line(Nodes[Edges[i].Ends.I].Frame.Origin, Nodes[Edges[i].Ends.J].Frame.Origin);
                lines.Add(l);
            }

            return lines;
        }

        public List<Plane> GetPlanes()
        {
            List<Plane> planes = new List<Plane>();

            for (int i = 0; i < Nodes.Count; ++i)
            {
                planes.Add(Nodes[i].Frame);
            }

            return planes;
        }

        public void DeleteNode(int index, bool reconnect = false)
        {
            if (index > Nodes.Count - 1) throw new Exception("Network::DeleteNode: Index out of range!");

            NodeLegacy n = Nodes[index];

            NodeInterface[] interfaces = n.Edges.ToArray();

            int[] edge_indices = interfaces.Select(x => x.Index).ToArray();
            EdgeLegacy[] edges = interfaces.Select(x => Edges[x.Index]).ToArray();

            NodeLegacy[] surrounding_nodes = new NodeLegacy[edges.Length];

            for (int i = 0; i < edges.Length; ++i)
            {
                if (edges[i].Ends.I == index)
                    surrounding_nodes[i] = Nodes[edges[i].Ends.J];
                else
                    surrounding_nodes[i] = Nodes[edges[i].Ends.I];
            }




        }
    }

    public class NodeLegacy
    {
        public List<NodeInterface> Edges;
        public List<int> Chains;

        public Plane Frame;
        protected Guid Id;

        public NodeLegacy()
        {
            Edges = new List<NodeInterface>();
            Chains = new List<int>();
            Id = Guid.NewGuid();
        }

        private void AddEdge(int i, Vector3d v)
        {
            v.Unitize();
            NodeInterface ni = new NodeInterface(v, i);
            Edges.Add(ni);
        }

        public void CalculateAverageFrame()
        {
            Vector3d av_normal = new Vector3d(0, 0, 0);

            if (Edges.Count < 2) return;

            if (Edges.Count == 2)
            {
                Vector3d v = -Edges[0].Vector - Edges[1].Vector;
                v.Unitize();
                Frame = new Plane(Frame.Origin, v);
                return;
            }

            List<int> SI = new List<int>();
            Util.SortVectorsAroundPoint(Edges.Select(x => x.Vector).ToList(),
              Frame.Origin, out SI);

            for (int i = 0; i < SI.Count; ++i)
            {
                int ii = (i + 1) % SI.Count;
                Vector3d v = Vector3d.CrossProduct(Edges[SI[i]].Vector,
                  Edges[SI[ii]].Vector);

                av_normal += v;
            }

            if (av_normal.Length < 0.000001)
            {
                av_normal = Vector3d.CrossProduct(Edges[0].Vector.Unitized(), Edges[1].Vector.Unitized());
            }

            av_normal.Unitize();

            Frame = new Plane(Frame.Origin, av_normal);

        }

        public void ProjectEdgeVectors()
        {
            for (int i = 0; i < Edges.Count; ++i)
            {
                Vector3d v = Edges[i].Vector.ProjectToPlane(Frame);
                v.Unitize();
                Edges[i].Vector = v;
            }
        }

        public override bool Equals(object obj)
        {
            NodeLegacy item = obj as NodeLegacy;
            return item.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return "Node";
        }

        public void Write(ref XElement elem)
        {
            XElement n = new XElement("node");
            n.SetAttributeValue("id", Id);
            n.SetAttributeValue("type", ToString());

            XElement nedges = new XElement("node_edges");
            for (int i = 0; i < Edges.Count; ++i)
            {
                // write edges
            }

            n.Add(nedges);

            XElement frame = new XElement("frame");
            frame.SetAttributeValue("PosX", Frame.Origin.X);
            frame.SetAttributeValue("PosY", Frame.Origin.Y);
            frame.SetAttributeValue("PosZ", Frame.Origin.Z);

            frame.SetAttributeValue("XX", Frame.XAxis.X);
            frame.SetAttributeValue("XY", Frame.XAxis.Y);
            frame.SetAttributeValue("XZ", Frame.XAxis.Z);

            frame.SetAttributeValue("YX", Frame.YAxis.X);
            frame.SetAttributeValue("YY", Frame.YAxis.Y);
            frame.SetAttributeValue("YZ", Frame.YAxis.Z);

            n.Add(frame);

            elem.Add(n);
        }

        public static NodeLegacy Read(XElement elem)
        {
            NodeLegacy n;
            if (elem.Name != "node")
                throw new Exception("XElement is not a valid Node!");

            XAttribute attr;

            Guid id;

            attr = elem.Attribute("id");
            if (attr != null)
                id = Guid.Parse(attr.Value);

            attr = elem.Attribute("type");
            if (attr == null)
                throw new Exception("XElement does not contain a node type!");
            string type = attr.Value;

            if (type == "FootNode")
            {
                n = new FootNode();
            }
            else if (type == "BranchingNode")
                n = new BranchingNode();
            else
                n = new NodeLegacy();

            XElement nedges = elem.Element("node_edges");
            XElement[] edges = nedges.Elements("node_edge").ToArray();

            for (int i = 0; i < edges.Length; ++i)
            {
                attr = edges[i].Attribute("index");
                if (attr == null) throw new Exception("NodeInterface needs an index!");

                int index = Convert.ToInt32(attr.Value);
                double vx, vy, vz;

                attr = edges[i].Attribute("vX");
                if (attr == null) throw new Exception("NodeInterface is missing part of its vector!");
                vx = Convert.ToDouble(attr.Value);

                attr = edges[i].Attribute("vY");
                if (attr == null) throw new Exception("NodeInterface is missing part of its vector!");
                vy = Convert.ToDouble(attr.Value);

                attr = edges[i].Attribute("vZ");
                if (attr == null) throw new Exception("NodeInterface is missing part of its vector!");
                vz = Convert.ToDouble(attr.Value);

                int w;
                attr = edges[i].Attribute("weight");
                if (attr == null) throw new Exception("NodeInterface is missing part of its vector!");
                w = Convert.ToInt32(attr.Value);

                int ud = 0;
                attr = edges[i].Attribute("user_data");
                if (attr != null)
                    ud = Convert.ToInt32(attr.Value);

                NodeInterface ni = new NodeInterface(new Vector3d(vx, vy, vz), index);
                ni.Weight = w;
                ni.UserData = ud;

                n.Edges.Add(ni);
            }

            if (n is BranchingNode)
            {
                BranchingNode bn = n as BranchingNode;
                XElement bweights = elem.Element("branch_weights");
                if (bweights != null)
                {
                    XElement[] bw_list = bweights.Elements("branch_weight").ToArray();
                    for (int i = 0; i < bw_list.Length; ++i)
                    {

                        bn.BranchWeights.Add(Convert.ToInt32(bw_list[i].Value));
                    }
                }
                n = bn;
            }

            return n;
        }
    }

    public class BranchingNode : NodeLegacy
    {
        public BranchingNode() : base()
        {
            BranchWeights = new List<int>();
        }

        public int LeftIndex, RightIndex;
        public int TrunkIndex;
        public List<int> BranchWeights;

        public NodeInterface Trunk()
        {
            return Edges[TrunkIndex];
        }

        public void SortEdgesLR(Vector3d normal)
        {

            Vector3d SortVector = Vector3d.CrossProduct(Edges[TrunkIndex].Vector.Unitized(), normal.Unitized());

            NodeInterface[] NI = Edges.Where(x => x.Index != Trunk().Index).ToArray();

            foreach (NodeInterface ni in NI)
            {
                ni.UserData = ni.Vector * SortVector;
            }

            Array.Sort(NI.Select(x => x.UserData).ToArray(), NI);

            List<NodeInterface> NewEdges = new List<NodeInterface>();

            NewEdges.Add(Edges[TrunkIndex]);
            NewEdges.AddRange(NI);

            //RightIndex = NI[1].Index;
            //LeftIndex = NI[2].Index;
            TrunkIndex = 0;
            RightIndex = 2;
            LeftIndex = 1;

            Edges = NewEdges;
        }

        public override string ToString()
        {
            return "BranchingNode";
        }

    }

    public class FootNode : NodeLegacy
    {
        public override string ToString()
        {
            return "FootNode";
        }
    }

    public class NodeInterface
    {
        public Vector3d Vector;
        public int Index;
        public int Weight;
        public double UserData;

        public NodeInterface(Vector3d v, int i)
        {
            Vector = v;
            Index = i;
            Weight = 1;
        }


    }

    public class EdgeLegacy
    {
        public IndexPair Ends;
        public Plane Frame;
        public Curve Curve;
        public double Weight;

        protected Guid Id;

        public EdgeLegacy()
        {
        }

        public override bool Equals(object obj)
        {
            EdgeLegacy item = obj as EdgeLegacy;
            return item.Id == this.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public void Write(ref XElement elem)
        {
            XElement e = new XElement("edge");
            e.SetAttributeValue("id", Id);
            e.SetAttributeValue("end1", Ends.I);
            e.SetAttributeValue("end2", Ends.J);
            e.SetAttributeValue("weight", Weight);

            elem.Add(e);
        }

        public static EdgeLegacy Read(ref XElement elem)
        {
            EdgeLegacy e = new EdgeLegacy();
            if (elem.Name != "edge")
                throw new Exception("XElement is not a valid Edge!");

            XAttribute attr;

            attr = elem.Attribute("id");
            if (attr != null)
                e.Id = Guid.Parse(attr.Value);

            attr = elem.Attribute("end1");
            if (attr != null)
                e.Ends.I = Convert.ToInt32(attr.Value);

            attr = elem.Attribute("end2");
            if (attr != null)
                e.Ends.J = Convert.ToInt32(attr.Value);

            attr = elem.Attribute("weight");
            if (attr != null)
                e.Weight = Convert.ToDouble(attr.Value);

            return e;
        }
    }

    public class Chain
    {
        public List<int> Edges;
        public List<int> Vertices;
        protected Guid ID;

        public Chain()
        {
            Edges = new List<int>();
            Vertices = new List<int>();
            ID = Guid.NewGuid();
        }

        /// <summary>
        /// Given a list of edges, find all contained Nodes (vertices) that form the links between edges)
        /// </summary>
        public bool FindVertices(ref NetworkLegacy net)
        {
            bool continuous = true;
            Vertices = new List<int>();
            for (int i = 0; i < Edges.Count; ++i)
            {
                Vertices.Add(net.Edges[Edges[i]].Ends.I);
                Vertices.Add(net.Edges[Edges[i]].Ends.J);
            }

            Vertices = Vertices.Distinct().ToList();

            // TO DO: check for continuity along vertices
            // TO DO: decide if continuity is actually necessary

            return continuous;

            /*
            for (int i = 0; i < Edges.Count - 1; ++i)
            {
                if (net.Edges[Edges[i]].Ends.I == net.Edges[Edges[i + 1]].Ends.I) Vertices.Add(net.Edges[Edges[i]].Ends.I);
                else if (net.Edges[Edges[i]].Ends.I == net.Edges[Edges[i + 1]].Ends.J) Vertices.Add(net.Edges[Edges[i]].Ends.I);
                else if (net.Edges[Edges[i]].Ends.J == net.Edges[Edges[i + 1]].Ends.I) Vertices.Add(net.Edges[Edges[i]].Ends.J);
                else if (net.Edges[Edges[i]].Ends.J == net.Edges[Edges[i + 1]].Ends.J) Vertices.Add(net.Edges[Edges[i]].Ends.J);
            }
            */
        }

        public override bool Equals(object obj)
        {
            Chain item = obj as Chain;
            return item.ID == this.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}