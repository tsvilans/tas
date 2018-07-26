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
using System.Xml.Linq;
using Rhino;

namespace tas.Core
{
    /// <summary>
    /// Network v3. using pointers to nodes and edges instead of C-style lists and indices. 
    /// Vastly simplifies syntax and modifications.
    /// </summary>
    public class Net
    {
        List<Node> NodeList;
        List<Edge> EdgeList;

        Dictionary<Guid, WeakReference> Nodes;
        Dictionary<Guid, WeakReference> Edges;

        /// <summary>
        /// Name of network.
        /// </summary>
        public string Name;

        /// <summary>
        /// Version identifier.
        /// </summary>
        public int Version { get; protected set; }

        // CONSTRUCTOR
        /// <summary>
        /// Default constructor. 
        /// </summary>
        /// <param name="name"></param>
        public Net(string name = "Net3")
        {
            Version = 3;
            Name = name;

            Nodes = new Dictionary<Guid, WeakReference>();
            Edges = new Dictionary<Guid, WeakReference>();

            NodeList = new List<Node>();
            EdgeList = new List<Edge>();
        }

        /// <summary>
        /// Create Network from Mesh. Uses vertex normals to define node normals.
        /// </summary>
        /// <param name="mesh">Mesh to convert.</param>
        /// <param name="name">Optional name of new Network.</param>
        /// <returns></returns>
        public static Net FromMesh(Mesh mesh, string name = "Net3")
        {
            Net net = new Net(name);
            Guid[] node_ids = new Guid[mesh.TopologyVertices.Count];
            for (int i = 0; i < mesh.TopologyVertices.Count; ++i)
            {
                var normal = mesh.Normals[i];
                SNode node = new SNode(new Plane(mesh.TopologyVertices[i], normal));
                node_ids[i] = node.Id;
                net.AddNode(node);
            }

            for (int i = 0; i < mesh.TopologyEdges.Count; ++i)
            {
                IndexPair ei = mesh.TopologyEdges.GetTopologyVertices(i);
                net.Link(node_ids[ei.I], node_ids[ei.J]);
            }

            return net;
        } 

        /// <summary>
        /// Base node class for generic nodes.
        /// </summary>
        public class Node
        {
            public Guid Id { get; private set; }
            public List<Edge> Edges;
            public List<string> Tags;
            public string Name;

            public int Valence {
                get
                {
                    return Edges.Count;
                }
            }

            public Node(Guid id = new Guid())
            {
                if (id == Guid.Empty)
                    Id = Guid.NewGuid();
                else
                    Id = id;

                Edges = new List<Edge>();
                Tags = new List<string>();
            }

            public Node GetConnectedNode(int index)
            {
                if (index > Edges.Count - 1 || index < 0) throw new Exception("Node::GetConnectedNode: Index out of bounds.");
                Edge e = Edges[index];
                if (e.Start == this) return e.End;
                else return e.Start;
            }

            public void Unlink()
            {
                Edges.Clear();
            }

            public static Node FromXML(XElement elem, out List<Guid> edges)
            {
                Node n;
                if (elem.Name != "node") throw new Exception("Net3::Node: Invalid XElement type!");
                string type = elem.Attribute("type").Value;
                Guid id = Guid.Parse(elem.Attribute("id").Value);
                string name = elem.Attribute("name").Value;


                if (type == "SpaceNode")
                {
                    n = new SNode();
                    Point3d p = Point3d.Origin;
                    Vector3d vx = Vector3d.XAxis, vy = Vector3d.YAxis;

                    XElement frame = elem.Element("frame");
                    p.X = Convert.ToDouble(frame.Attribute("PosX").Value);
                    p.Y = Convert.ToDouble(frame.Attribute("PosY").Value);
                    p.Z = Convert.ToDouble(frame.Attribute("PosZ").Value);

                    vx.X = Convert.ToDouble(frame.Attribute("XX").Value);
                    vx.Y = Convert.ToDouble(frame.Attribute("XY").Value);
                    vx.Z = Convert.ToDouble(frame.Attribute("XZ").Value);

                    vy.X = Convert.ToDouble(frame.Attribute("YX").Value);
                    vy.Y = Convert.ToDouble(frame.Attribute("YY").Value);
                    vy.Z = Convert.ToDouble(frame.Attribute("YZ").Value);

                    n = new SNode(new Plane(p, vx, vy), id);
                }
                else
                    n = new Node(id);

                XElement[] nedges = elem.Elements("edge").ToArray();

                edges = new List<Guid>();
                for (int j = 0; j < nedges.Length; ++j)
                {
                    edges.Add(Guid.Parse(nedges[j].Attribute("id").Value));
                }

                return n;
            }

            public virtual XElement ToXML()
            {
                XElement elem = new XElement("node");
                elem.SetAttributeValue("name", Name);
                elem.SetAttributeValue("type", this.ToString());
                elem.SetAttributeValue("id", Id.ToString());

                for (int i = 0; i < Edges.Count; ++i)
                {
                    XElement edge = new XElement("edge");
                    edge.SetAttributeValue("id", Edges[i].Id.ToString());
                    elem.Add(edge);
                }

                return elem;
            }

            public virtual Node Duplicate()
            {
                Node n = new Node(Id);
                n.Edges.AddRange(Edges);
                n.Name = Name;

                return n;
            }

            public override string ToString()
            {
                return "Node";
            }

            public override bool Equals(object obj)
            {
                if (obj is Node && (obj as Node).Id == Id)
                    return true;
                return false;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }

        /// <summary>
        /// SpaceNode. Node with spatial coordinates and an orientation, represented as a Plane.
        /// </summary>
        public class SNode : Node
        {
            public Plane Frame;

            public SNode(Plane frame = new Plane(), Guid id = new Guid()) : base(id)
            {
                Frame = frame;
            }

            public override Node Duplicate()
            {
                SNode sn = new SNode(Frame, Id);
                sn.Name = Name;
                sn.Edges.AddRange(Edges);

                return sn;
            }

            public override string ToString()
            {
                return "SpaceNode";
            }

            public override bool Equals(object obj)
            {
                if (obj is SNode && (obj as SNode).Id == Id)
                    return true;
                return false;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public override XElement ToXML()
            {
                XElement elem = base.ToXML();
                elem.SetAttributeValue("type", this.ToString());

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

                elem.Add(frame);

                return elem;
            }

        }

        /// <summary>
        /// Edge between two nodes.
        /// </summary>
        public class Edge
        {
            public Guid Id { get; private set; }
            public Node Start;
            public Node End;

            public Edge(Guid id = new Guid())
            {
                if (id == Guid.Empty)
                    Id = Guid.NewGuid();
                else
                    Id = id;
            }

            public Edge(Node n0, Node n1, Guid id = new Guid()) : this(id)
            {
                Start = n0;
                End = n1;
            }

            public Point3d GetMidPoint()
            {
                if (Start is SNode && End is SNode)
                    return ((Start as SNode).Frame.Origin + (End as SNode).Frame.Origin) / 2;
                return Point3d.Unset;
            }

            public Point3d GetPointAlong(double t)
            {
                if (Start is SNode && End is SNode)
                    return Util.Interpolation.Lerp((Start as SNode).Frame.Origin, (End as SNode).Frame.Origin, t);
                return Point3d.Unset;
            }

            public Plane GetPlaneAlong(double t)
            {
                if (Start is SNode && End is SNode)
                    return Util.Interpolation.InterpolatePlanes2((Start as SNode).Frame, (End as SNode).Frame, t);
                return Plane.Unset;
            }

            public Plane GetMidPlane() => GetPlaneAlong(0.5);

            public virtual XElement ToXML()
            {
                XElement elem = new XElement("edge");
                elem.SetAttributeValue("type", this.ToString());
                elem.SetAttributeValue("id", Id.ToString());
                elem.SetAttributeValue("end1", Start.Id.ToString());
                elem.SetAttributeValue("end2", End.Id.ToString());

                return elem;
            }

            public override bool Equals(object obj)
            {
                if (obj is Edge)
                {
                    Edge e = obj as Edge;
                    if (e.Id == Id)
                        return true;
                    if ((e.Start == Start && e.End == End) || (e.Start == End && e.End == Start))
                        return true;
                }
                return false;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public override string ToString()
            {
                return "Edge";
            }
        }

        /// <summary>
        /// Edge with weight value.
        /// </summary>
        public class WeightedEdge : Edge
        {
            public double Weight;

            public WeightedEdge(double weight = 1.0, Guid id = new Guid()) : base(id)
            {
                Weight = 1.0;
            }

            public override XElement ToXML()
            {
                XElement elem = base.ToXML();
                elem.SetAttributeValue("type", this.ToString());
                elem.SetAttributeValue("weight", Weight);
                return elem;
            }

            public override string ToString()
            {
                return "WeightedEdge";
            }
        }

        #region METHODS

        /// <summary>
        /// Clean empty references in network.
        /// </summary>
        public void Clean()
        {

            foreach (Guid key in Nodes.Keys)
            {
                WeakReference wr;
                if (Nodes.TryGetValue(key, out wr))
                {
                    if (!wr.IsAlive)
                        Nodes.Remove(key);
                }
            }

            foreach (Guid key in Edges.Keys)
            {
                WeakReference wr;
                if (Edges.TryGetValue(key, out wr))
                {
                    if (!wr.IsAlive)
                        Edges.Remove(key);
                }
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        /// <summary>
        /// Add node to network.
        /// </summary>
        /// <param name="n">Node to add.</param>
        public void AddNode(Node n)
        {
            Nodes[n.Id] = new WeakReference(n);
            if (!NodeList.Contains(n))
                NodeList.Add(n);
        }

        /// <summary>
        /// Add edge to network.
        /// </summary>
        /// <param name="e">Edge to add.</param>
        public void AddEdge(Edge e)
        {
            Edges[e.Id] = new WeakReference(e);
            if (!EdgeList.Contains(e))
                EdgeList.Add(e);
        }

        /// <summary>
        /// Access specific node by ID.
        /// </summary>
        /// <param name="id">Node ID.</param>
        /// <returns></returns>
        public Node GetNode(Guid id)
        {
            WeakReference wr;
            if (Nodes.TryGetValue(id, out wr))
                if (wr.IsAlive) return wr.Target as Node;
            return null;
        }

        /// <summary>
        /// Access specific edge by ID.
        /// </summary>
        /// <param name="id">Edge ID.</param>
        /// <returns></returns>
        public Edge GetEdge(Guid id)
        {
            WeakReference wr;
            if (Edges.TryGetValue(id, out wr))
                if (wr.IsAlive) return wr.Target as Edge;
            return null;
        }

        /// <summary>
        /// Create edge between two nodes, with optionally specified ID.
        /// </summary>
        /// <param name="n0">First node to connect.</param>
        /// <param name="n1">Second node to connect.</param>
        /// <param name="id">Optional ID to give new edge.</param>
        public void Link(Node n0, Node n1, Guid id = new Guid())
        {
            if (n0 == null || n1 == null) throw new Exception("Net3::Link: One or more nodes are null.");
            if (n0 == n1) throw new Exception("Net3::Link: Can't link to the same node!");
            if (!Nodes.ContainsKey(n0.Id))
                AddNode(n0);
            if (!Nodes.ContainsKey(n1.Id))
                AddNode(n1);

            Edge e = new Core.Net.Edge(n0, n1, id);

            n0.Edges.Add(e);
            n1.Edges.Add(e);
            AddEdge(e);
        }

        /// <summary>
        /// Create edge between two nodes specified by their IDs, with optionally specified ID.
        /// </summary>
        /// <param name="id0">ID of first node to connect.</param>
        /// <param name="id1">ID of second node to connect.</param>
        /// <param name="id">Optional ID for new edge.</param>
        public void Link(Guid id0, Guid id1, Guid id = new Guid())
        {
            Link(GetNode(id0), GetNode(id1), id);
        }

        /// <summary>
        /// Destroy edge.
        /// </summary>
        /// <param name="e">Edge to destroy.</param>
        public void Unlink(Edge e)
        {
            if (e.Start != null)
                    for (int i = 0; i < e.Start.Edges.Count; ++i)
                        if (e.Start.Edges[i] == e)
                        {
                            e.Start.Edges.RemoveAt(i);
                            break;
                        }
            if (e.End != null)
                for (int i = 0; i < e.End.Edges.Count; ++i)
                    if (e.End.Edges[i] == e)
                    {
                        e.End.Edges.RemoveAt(i);
                        break;
                    }
            Edges.Remove(e.Id);
            EdgeList.Remove(e);
        }

        /// <summary>
        /// Destroy edge by ID.
        /// </summary>
        /// <param name="id">ID of edge to destroy.</param>
        public void Unlink(Guid id)
        {
            Unlink(GetEdge(id));
        }

        /// <summary>
        /// Delete node.
        /// </summary>
        /// <param name="n">Node to delete.</param>
        public void RemoveNode(Node n)
        {
            Edge[] edges = n.Edges.ToArray();

            List<Node> connected = new List<Node>();
            foreach (Edge e in edges)
                Unlink(e);

            n.Edges = null;
            Nodes.Remove(n.Id);
            NodeList.Remove(n);
        }

        /// <summary>
        /// Delete node by ID.
        /// </summary>
        /// <param name="id">ID of node to delete.</param>
        public void RemoveNode(Guid id)
        {
            Node n = GetNode(id);
            RemoveNode(n);
        }

        /// <summary>
        /// Delete nodes that are not connected with any edges.
        /// </summary>
        public void CullOrphanedNodes()
        {
            List<Node> cull_list = new List<Node>();
            foreach (Node n in NodeList)
                if (n.Edges.Count < 1)
                    cull_list.Add(n);

            foreach (Node n in cull_list)
                NodeList.Remove(n);

            Clean();
        }

        /// <summary>
        /// Duplicate network.
        /// </summary>
        /// <returns></returns>
        public Net Duplicate()
        {
            Net net = new Net();

            net.Name = Name;
            for (int i = 0; i < NodeList.Count; ++i)
            {
                Node n = NodeList[i].Duplicate();
                n.Unlink();
                net.AddNode(n);
            }

            for (int i = 0; i < EdgeList.Count; ++i)
            {
                net.Link(EdgeList[i].Start.Id, EdgeList[i].End.Id, EdgeList[i].Id);
            }

            return net;
        }

        /// <summary>
        /// Get array of all network edges.
        /// </summary>
        /// <returns>Array of edges.</returns>
        public Edge[] GetAllEdges()
        {
            /*
            List<Edge> edges = new List<Edge>();

            foreach (WeakReference we in Edges.Values)
            {
                if (we.IsAlive)
                {
                    edges.Add(we.Target as Edge);
                }
            }

            return edges.ToArray();
            */
            return EdgeList.ToArray();
        }

        /// <summary>
        /// Get array of all network nodes.
        /// </summary>
        /// <returns>Array of nodes.</returns>
        public Node[] GetAllNodes()
        {
            /*
            List<Node> nodes = new List<Node>();

            foreach (WeakReference we in Nodes.Values)
            {
                if (we.IsAlive)
                {
                    nodes.Add(we.Target as Node);
                }
            }

            return nodes.ToArray();
            */
            return NodeList.ToArray();
        }

        /// <summary>
        /// Get array of network edges as line objects.
        /// </summary>
        /// <returns>Array of lines.</returns>
        public Line[] GetAllEdgesAsLines()
        {
            List<Line> lines = new List<Line>();
            foreach (Guid id in Edges.Keys)
            {
                Edge e = GetEdge(id);
                if (e.Start is SNode && e.End is SNode)
                {
                    SNode sn1 = e.Start as SNode;
                    SNode sn2 = e.End as SNode;
                    lines.Add(new Line(sn1.Frame.Origin, sn2.Frame.Origin));
                }
            }

            return lines.ToArray();
        }

        /// <summary>
        /// Get array of network nodes as plane objects.
        /// </summary>
        /// <returns>Array of planes.</returns>
        public Plane[] GetAllNodesAsPlanes()
        {
            List<Plane> planes = new List<Plane>();
            foreach(Guid id in Nodes.Keys)
            {
                Node n = GetNode(id);
                if (n is SNode)
                {
                    planes.Add((n as SNode).Frame);
                }
            }
            return planes.ToArray();
        }

        /// <summary>
        /// Get ordered list of node IDs.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Guid> GetIndexedNodeIDs()
        {
            Dictionary<int, Guid> dict = new Dictionary<int, Guid>();
            Guid[] ids = Nodes.Keys.ToArray();
            for (int i = 0; i < ids.Length; ++i)
                dict[i] = ids[i];
            return dict;
        }

        /// <summary>
        /// Get ordered list of edge IDs.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Guid> GetIndexedEdgeIDs()
        {
            Dictionary<int, Guid> dict = new Dictionary<int, Guid>();
            Guid[] ids = Edges.Keys.ToArray();
            for (int i = 0; i < ids.Length; ++i)
                dict[i] = ids[i];
            return dict;
        }

        /// <summary>
        /// Convert network to XML representation.
        /// </summary>
        /// <returns></returns>
        public XElement ToXML()
        {
            XElement root = new XElement("network");
            root.SetAttributeValue("version", Version);
            root.SetAttributeValue("name", Name);

            XElement nodes = new XElement("nodes");
            nodes.SetAttributeValue("count", Nodes.Count);
            foreach (Guid id in Nodes.Keys)
                nodes.Add(GetNode(id).ToXML());
            root.Add(nodes);

            XElement edges = new XElement("edges");
            edges.SetAttributeValue("count", Edges.Count);
            foreach (Guid id in Edges.Keys)
                edges.Add(GetEdge(id).ToXML());
            root.Add(edges);

            return root;
        }

        /// <summary>
        /// Write network to file as XML.
        /// </summary>
        /// <param name="path">File path to write to.</param>
        /// <param name="create_dir">Optional: create target directory if it doesn't exist.</param>
        public void Write(string path, bool create_dir = false)
        {
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                if (create_dir)
                    System.IO.Directory.CreateDirectory(dir);
                else
                    throw new Exception("Net3::Write: Directory does not exist! Try creating it?");

            XDocument doc = new XDocument();
            doc.Add(this.ToXML());
            doc.Save(path);
        }

        /// <summary>
        /// Read network from XML file.
        /// </summary>
        /// <param name="path">Path to network file.</param>
        /// <returns></returns>
        public static Net Read(string path)
        {
            if (!System.IO.File.Exists(path))
                throw new Exception("Net3::Read: File does not exist!");
            XDocument doc = XDocument.Load(path);
            if (doc.Root.Name != "network")
                throw new Exception("Net3::Read: Root element is not 'network'.");

            Net net = new Net();

            net.Version = Convert.ToInt32(doc.Root.Attribute("version").Value);
            net.Name = doc.Root.Attribute("name").Value;

            //ADD NODES

            XElement nodes = doc.Root.Element("nodes");
            if (nodes == null)
                throw new Exception("Net3::Read: Element 'nodes' not found.");

            XElement[] nlist = nodes.Elements("node").ToArray();
            Dictionary<Guid, List<Guid>> NodeEdges = new Dictionary<Guid, List<Guid>>();

            for (int i = 0; i < nlist.Length; ++i)
            {
                Node n;
                Guid id = Guid.Parse(nlist[i].Attribute("id").Value);
                string type = nlist[i].Attribute("type").Value;

                if (type == "SpaceNode")
                {
                    n = new SNode();
                    Point3d p = Point3d.Origin;
                    Vector3d vx = Vector3d.XAxis, vy = Vector3d.YAxis;

                    XElement frame = nlist[i].Element("frame");
                    p.X = Convert.ToDouble(frame.Attribute("PosX").Value);
                    p.Y = Convert.ToDouble(frame.Attribute("PosY").Value);
                    p.Z = Convert.ToDouble(frame.Attribute("PosZ").Value);

                    vx.X = Convert.ToDouble(frame.Attribute("XX").Value);
                    vx.Y = Convert.ToDouble(frame.Attribute("XY").Value);
                    vx.Z = Convert.ToDouble(frame.Attribute("XZ").Value);

                    vy.X = Convert.ToDouble(frame.Attribute("YX").Value);
                    vy.Y = Convert.ToDouble(frame.Attribute("YY").Value);
                    vy.Z = Convert.ToDouble(frame.Attribute("YZ").Value);

                    n = new SNode(new Plane(p, vx, vy), id);
                }
                else
                    n = new Node(id);

                XElement[] nedges = nlist[i].Elements("edge").ToArray();

                List<Guid> edge_ids = new List<Guid>();
                for (int j = 0; j < nedges.Length; ++j)
                {
                    edge_ids.Add(Guid.Parse(nedges[j].Attribute("id").Value));
                }

                NodeEdges[id] = edge_ids;

                net.AddNode(n);
            }

            // ADD EDGES

            XElement edges = doc.Root.Element("edges");
            if (nodes == null)
                throw new Exception("Net3::Read: Element 'edges' not found.");

            XElement[] elist = edges.Elements("edge").ToArray();
            if (elist.Length < 1) throw new Exception("Net3::Read: No edges found in file!");

            for (int i = 0; i < elist.Length; ++i)
            {
                Guid id = Guid.Parse(elist[i].Attribute("id").Value);
                Guid n1 = Guid.Parse(elist[i].Attribute("end1").Value);
                if (n1 == null) continue;
                Guid n2 = Guid.Parse(elist[i].Attribute("end2").Value);
                if (n2 == null) continue;

                net.Link(net.GetNode(n1), net.GetNode(n2), id);
                //net.GetNode(n1).Edges.Add(net.GetEdge(id));
                //net.GetNode(n2).Edges.Add(net.GetEdge(id));
            }

            // CHECK 

            foreach (Guid id in net.Nodes.Keys)
            {
                List<Guid> ids = NodeEdges[id];
                Node n = net.GetNode(id);

                if (n == null) throw new Exception("Net3: Corrupt network structure. Please format your harddrive and try again.");

                bool[] flags = new bool[n.Edges.Count];
                for (int i = 0; i < n.Edges.Count; ++i)
                {
                    Edge e = n.Edges[i];
                    for (int j = 0; j < ids.Count; ++j)
                    {
                        if (ids[j] == e.Id)
                        {
                            flags[i] = true;
                            break;
                        }
                    }
                }

                for (int i = 0; i < flags.Length; ++i)
                {
                    if (!flags[i])
                    {
                        throw new Exception("Net3: Node " + id.ToString() + ": Edge / Node relationship is not mutual.");
                    }
                }
            }
            return net;
        }

        /// <summary>
        /// Read old network format from XML file.
        /// </summary>
        /// <param name="path">Path to legacy network file.</param>
        /// <returns></returns>
        public static Net ReadLegacy(string path)
        {
            if (!System.IO.File.Exists(path))
                throw new Exception("Net::ReadLegacy: File does not exist!");

            XDocument doc = XDocument.Load(path);
            if (doc == null)
                throw new Exception("Net::ReadLegacy: Failed to load file.");

            XElement root = doc.Root;
            if (root.Name != "network")
                throw new Exception("Net::ReadLegacy: Not a valid Network file.");

            Net net = new Net();

            XElement nodes = root.Element("nodes");
            if (nodes != null)
            {
                var node_list = nodes.Elements();
                foreach (XElement node in node_list)
                {
                    if (node.Name != "node")
                        continue;

                    SNode n;

                    string type = node.Attribute("type").Value;
                    if (type == "FootNode")
                    {
                        n = new SNode();
                        n.Tags.Add("foot_node");
                    }
                    else if (type == "BranchingNode")
                    {
                        n = new SNode();
                        n.Tags.Add("branching_node");

                        //BranchingNode bn = n as BranchingNode;

                        //if (bn == null) continue;

                        XElement branch_weights = node.Element("branch_weights");
                        if (branch_weights != null)
                        {
                            var branch_weight_list = branch_weights.Elements();

                            //foreach (XElement bw in branch_weight_list)
                            //{
                            //if (bw.Name == "branch_weight")
                            //bn.BranchWeights.Add(Convert.ToInt32(bw.Value));
                            // }
                        }
                    }
                    else
                        n = new SNode();


                    XElement node_edges = node.Element("node_edges");
                    var edge_list = node_edges.Elements("node_edge");

                    foreach (XElement ne in edge_list)
                    {
                        double x = Convert.ToDouble(ne.Attribute("vX").Value);
                        double y = Convert.ToDouble(ne.Attribute("vY").Value);
                        double z = Convert.ToDouble(ne.Attribute("vZ").Value);

                        //n.Edges.Add(new NodeInterface(new Vector3d(x, y, z), Convert.ToInt32(ne.Attribute("index").Value)));
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

                    net.AddNode(n);
                }
            }

            Node[] node_array = net.GetAllNodes();

            XElement edges = root.Element("edges");
            if (edges != null)
            {
                var edge_list = edges.Elements("edge");
                foreach (XElement edge in edge_list)
                {
                    int i = Convert.ToInt32(edge.Attribute("end1").Value);
                    int j = Convert.ToInt32(edge.Attribute("end2").Value);

                    Edge e = new Edge(node_array[i], node_array[j]);

                    //e.Ends = new IndexPair(i, j);

                    //net.Edges.Add(e);
                    net.AddEdge(e);
                }
            }

            return net;
        }

        /// <summary>
        /// Orient nodes to match the surface normals of a Brep.
        /// </summary>
        /// <param name="brep">Brep to orient to.</param>
        /// <param name="max_range">Maximum search range.</param>
        /// <returns>Number of nodes changed.</returns>
        public int OrientNodes(Brep brep, double max_range = 0.0)
        {
            int N = 0;
            Vector3d normal;
            double s, t;
            ComponentIndex ci;
            Point3d cp;
            Transform rot;
            foreach (Node n in NodeList)
                if (n is SNode)
                    if (brep.ClosestPoint((n as SNode).Frame.Origin, out cp, out ci, out s, out t, max_range, out normal))
                    {
                        rot = Transform.Rotation((n as SNode).Frame.ZAxis, normal, (n as SNode).Frame.Origin);
                        (n as SNode).Frame.Transform(rot);
                        N++;
                    }
            return N;
        }

        /// <summary>
        /// Orient nodes to match the surface normals of a Mesh.
        /// </summary>
        /// <param name="mesh">Mesh to orient to.</param>
        /// <param name="max_range">Maximum search range.</param>
        /// <returns>Number of nodes changed.</returns>
        public int OrientNodes(Mesh mesh, double max_range = 0.0)
        {
            int N = 0;
            Vector3d normal;
            Point3d cp;
            Transform rot;
            foreach (Node n in NodeList)
                if (n is SNode)
                    if (mesh.ClosestPoint((n as SNode).Frame.Origin, out cp, out normal, max_range) >= 0)
                    {
                        rot = Transform.Rotation((n as SNode).Frame.ZAxis, normal, (n as SNode).Frame.Origin);
                        (n as SNode).Frame.Transform(rot);
                        N++;
                    }
            return N;
        }

        #endregion
    }
}
