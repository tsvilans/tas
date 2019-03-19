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
using System.Runtime.Serialization;

namespace tas.Core.Network
{
    /// <summary>
    /// Network v3. using pointers to nodes and edges instead of C-style lists and indices. 
    /// Vastly simplifies syntax and modifications.
    /// </summary>
    [Serializable]
    public class Net : ISerializable
    {
        List<Node> NodeList;
        List<Edge> EdgeList;
        public List<NodeGroup> NodeGroups;

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
        public Net(string name = "Net4")
        {
            Version = 4;
            Name = name;

            Nodes = new Dictionary<Guid, WeakReference>();
            Edges = new Dictionary<Guid, WeakReference>();

            NodeList = new List<Node>();
            EdgeList = new List<Edge>();
            NodeGroups = new List<NodeGroup>();
        }

        public Net(SerializationInfo info, StreamingContext context) : this()
        {
            Name = (string)info.GetValue("name", typeof(string));
            var snodes = (List<SerializableNode>)info.GetValue("nodes", typeof(List<SerializableNode>));
            var sedges = (List<SerializableEdge>)info.GetValue("edges", typeof(List<SerializableEdge>));

            foreach (var snode in snodes)
            {
                var node = new Node(snode.Frame, snode.Id);
                node.CustomData = snode.CustomData;
                node.Name = snode.Name;
                AddNode(node);
            }

            foreach (var sedge in sedges)
            {
                var edge = new Edge(sedge.Id);
                edge.CustomData = sedge.CustomData;
                edge.EndData = sedge.EndData;
                edge.StartData = sedge.StartData;
                AddEdge(edge);
            }

            foreach (var snode in snodes)
            {
                var node = GetNode(snode.Id);
                if (snode.Parent != null)
                    node.Parent = GetElement(snode.Parent);
                foreach (var child in snode.Children)
                    node.Children.Add(GetElement(child));

                foreach (var edge in snode.Edges)
                    node.Edges.Add(GetEdge(edge));
            }

            foreach (var sedge in sedges)
            {
                var edge = GetEdge(sedge.Id);
                if (sedge.Parent != null)
                    edge.Parent = GetElement(sedge.Parent);
                foreach (var child in sedge.Children)
                    edge.Children.Add(GetElement(child));

                edge.Start = GetNode(sedge.Start);
                edge.End = GetNode(sedge.End);
            }
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
                Node node = new Node(new Plane(mesh.TopologyVertices[i], normal));
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

        public NOb GetElement(Guid id)
        {
            NOb temp = GetNode(id);
            if (temp == null)
                return GetEdge(id);
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

            Edge e = new Edge(n0, n1, id);

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
                if (e.Start.Frame.IsValid && e.End.Frame.IsValid)
                    lines.Add(new Line(e.Start.Frame.Origin, e.End.Frame.Origin));
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
                if (n.Frame.IsValid)
                {
                    planes.Add(n.Frame);
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

        public static Net FromSerialized(List<SerializableNode> snodes, List<SerializableEdge> sedges, string name = "Net4")
        {
            Net net = new Net();

            foreach (var snode in snodes)
            { 
                var node = new Node(snode.Frame, snode.Id);
                node.CustomData = snode.CustomData;
                node.Name = snode.Name;
                net.AddNode(node);
            }

            foreach (var sedge in sedges)
            {
                var edge = new Edge(sedge.Id);
                edge.CustomData = sedge.CustomData;
                edge.EndData = sedge.EndData;
                edge.StartData = sedge.StartData;
                net.AddEdge(edge);
            }

            foreach (var snode in snodes)
            {
                var node = net.GetNode(snode.Id);
                if (snode.Parent != null)
                    node.Parent = net.GetElement(snode.Parent);
                foreach (var child in snode.Children)
                    node.Children.Add(net.GetElement(child));

                foreach (var edge in snode.Edges)
                    node.Edges.Add(net.GetEdge(edge));
            }

            foreach (var sedge in sedges)
            {
                var edge = net.GetEdge(sedge.Id);
                if (sedge.Parent != null)
                    edge.Parent = net.GetElement(sedge.Parent);
                foreach (var child in sedge.Children)
                    edge.Children.Add(net.GetElement(child));

                edge.Start = net.GetNode(sedge.Start);
                edge.End = net.GetNode(sedge.End);
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
                if (n.Frame.IsValid)
                    if (brep.ClosestPoint(n.Frame.Origin, out cp, out ci, out s, out t, max_range, out normal))
                    {
                        rot = Transform.Rotation(n.Frame.ZAxis, normal, n.Frame.Origin);
                        n.Frame.Transform(rot);
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
                if (n.Frame.IsValid)
                    if (mesh.ClosestPoint(n.Frame.Origin, out cp, out normal, max_range) >= 0)
                    {
                        rot = Transform.Rotation(n.Frame.ZAxis, normal, n.Frame.Origin);
                        n.Frame.Transform(rot);
                        N++;
                    }
            return N;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("nodes", NodeList, typeof(List<Node>));
            info.AddValue("edges", EdgeList, typeof(List<Edge>));
            info.AddValue("name", Name, typeof(string));
        }

        #endregion
    }

    /// <summary>
    /// Base class for all network elements
    /// </summary>
    public abstract class NOb : ISerializable
    {
        public Guid Id { get; protected set; }
        public List<NOb> Children;
        public NOb Parent;

        protected NOb()
        {
            Children = new List<NOb>();
            Parent = null;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("id", Id);
            if (Parent != null)
                info.AddValue("parent", Parent.Id);
            else
                info.AddValue("parent", Guid.Empty);
            info.AddValue("children", Children.Where(x => x != null).Select(x => x.Id));
        }
    }

    /// <summary>
    /// Base node class for generic nodes.
    /// </summary>
    [Serializable]
    public class Node : NOb, ISerializable
    {
        public Plane Frame;

        // Relations
        public List<Edge> Edges;


        public List<string> Tags;
        public Dictionary<string, object> CustomData;

        public string Name;

        public int Valence
        {
            get
            {
                return Edges.Count;
            }
        }

        public Node(Plane frame = new Plane(), Guid id = new Guid()) : base()
        {
            if (id == Guid.Empty)
                Id = Guid.NewGuid();
            else
                Id = id;

            Edges = new List<Edge>();
            Tags = new List<string>();
            CustomData = new Dictionary<string, object>();
            Frame = frame;
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

        public virtual Node Duplicate()
        {
            Node n = new Node(Frame, Id);
            n.Edges.AddRange(Edges);
            n.Name = Name;
            n.Tags.AddRange(this.Tags);
            n.CustomData = new Dictionary<string, object>(this.CustomData);
            n.Parent = Parent;
            n.Children = new List<NOb>(Children);

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

        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("frame", Frame);
            info.AddValue("edges", Edges.Where(x => x != null).Select(x => x.Id));
            info.AddValue("type", ToString());
            info.AddValue("custom_data", CustomData);

            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// Edge between two nodes.
    /// </summary>
    public class Edge : NOb, ISerializable
    {
        public Node Start;
        public Node End;

        public Dictionary<string, object> CustomData;

        public object StartData;
        public object EndData;

        public Edge(Guid id = new Guid()) : base()
        {
            if (id == Guid.Empty)
                Id = Guid.NewGuid();
            else
                Id = id;

            StartData = null;
            EndData = null;
            CustomData = new Dictionary<string, object>();
        }

        public Edge(Node n0, Node n1, Guid id = new Guid()) : this(id)
        {
            Start = n0;
            End = n1;
        }

        public Point3d GetMidPoint()
        {
            if (Start.Frame.IsValid  && End.Frame.IsValid)
                return (Start.Frame.Origin + End.Frame.Origin) / 2;
            return Point3d.Unset;
        }

        public Point3d GetPointAlong(double t)
        {
            if (Start.Frame.IsValid && End.Frame.IsValid)
                return Util.Interpolation.Lerp(Start.Frame.Origin, End.Frame.Origin, t);
            return Point3d.Unset;
        }

        public Plane GetPlaneAlong(double t)
        {
            if (Start.Frame.IsValid && End.Frame.IsValid)
                return Util.Interpolation.InterpolatePlanes2(Start.Frame, End.Frame, t);
            return Plane.Unset;
        }

        public Plane GetMidPlane() => GetPlaneAlong(0.5);

        public virtual XElement ToXml()
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


        public virtual Edge Duplicate()
        {
            Edge e = new Edge(Id);
            e.Start = Start;
            e.End = End;
            e.StartData = StartData;
            e.EndData = EndData;
            e.CustomData = new Dictionary<string, object>(CustomData);

            e.Parent = Parent;
            e.Children = new List<NOb>(Children);

            return e;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return "Edge";
        }


        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (Start != null)
                info.AddValue("start", Start.Id);
            else
                info.AddValue("start", Guid.Empty);

            if (End != null)
                info.AddValue("end", End.Id);
            else
                info.AddValue("end", Guid.Empty);

            info.AddValue("custom_data", CustomData);
            info.AddValue("start_data", StartData);
            info.AddValue("end_data", EndData);
            info.AddValue("type", ToString());

            base.GetObjectData(info, context);
        }
    
    }

    #region Serialization

    [Serializable]
    public class SerializableNOb : ISerializable
    {
        public Guid Id;
        public Guid Parent;
        public List<Guid> Children;

        public SerializableNOb()
        {
        }

        public SerializableNOb(SerializationInfo info, StreamingContext context)
        {
            Id = (Guid)info.GetValue("id", typeof(Guid));
            Parent = (Guid)info.GetValue("parent", typeof(Guid));
            Children = (List<Guid>)info.GetValue("children", typeof(List<Guid>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class SerializableNode : SerializableNOb, ISerializable
    {
        public SerializableNode() : base()
        {
        }

        public string Name;
        public Plane Frame;
        public List<Guid> Edges;
        public Dictionary<string, object> CustomData;

        public SerializableNode(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Name = (string)info.GetValue("name", typeof(string));
            Frame = (Plane)info.GetValue("frame", typeof(Plane));
            Edges = (List<Guid>)info.GetValue("edges", typeof(List<Guid>));
            CustomData = (Dictionary<string, object>)info.GetValue("custom_data", typeof(Dictionary<string, object>));
        }
    }

    [Serializable]
    public class SerializableEdge : SerializableNOb, ISerializable
    {
            public SerializableEdge() : base()
            {
            }

        // The value to serialize.
        public Guid Start;
        public Guid End;
        public Dictionary<string, object> CustomData;

        public object StartData;
        public object EndData;

        // The special constructor is used to deserialize values.
        public SerializableEdge(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // Reset the property value using the GetValue method.
            Start = (Guid)info.GetValue("start", typeof(Guid));
            End = (Guid)info.GetValue("end", typeof(Guid));
            CustomData = (Dictionary<string, object>)info.GetValue("custom_data", typeof(Dictionary<string, object>));
            StartData = info.GetValue("start_data", typeof(object));
            EndData = info.GetValue("end_data", typeof(object));
        }
    }

    #endregion


    public class NodeGroup
    {
        public List<Node> Nodes;
        public Plane Frame;
        public Dictionary<string, object> CustomData;

        public NodeGroup()
        {
            Nodes = new List<Node>();
            Frame = Plane.Unset;
            CustomData = new Dictionary<string, object>();
        }

        public void GetShared(NodeGroup ng, out List<Node> SharedNodes, out List<Edge> SharedEdges)
        {
            SharedNodes = this.Nodes.Where(x => ng.Nodes.Contains(x)).ToList();
            var theseEdges = this.Nodes.SelectMany(x => x.Edges).ToList();
            var thoseEdges = ng.Nodes.SelectMany(x => x.Edges).ToList();
            SharedEdges = theseEdges.Where(x => thoseEdges.Contains(x)).ToList();
        }
    }

}
