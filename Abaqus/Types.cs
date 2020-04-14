using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace tas.Abaqus
{

    public class Element
    {
        public int Id;
        public readonly int[] Data;
        double[] centre;

        public Element(int a, int b, int c, int d, int e, int f, int g, int h)
        {
            Data = new int[] { a, b, c, d, e, f, g, h };
            centre = new double[3];
        }
        public Element()
        {
            Data = new int[8];
            centre = new double[3];
        }

        public Element(int[] data)
        {
            Data = new int[8];
            Array.Copy(data, Data, Math.Min(data.Length, 8));
        }

        public int A
        { get { return Data[0]; } set { Data[0] = value; } }
        public int B
        { get { return Data[1]; } set { Data[1] = value; } }
        public int C
        { get { return Data[2]; } set { Data[2] = value; } }
        public int D
        { get { return Data[3]; } set { Data[3] = value; } }
        public int E
        { get { return Data[4]; } set { Data[4] = value; } }
        public int F
        { get { return Data[5]; } set { Data[5] = value; } }
        public int G
        { get { return Data[6]; } set { Data[6] = value; } }
        public int H
        { get { return Data[7]; } set { Data[7] = value; } }
    }

    public class ElementOrientation
    {
        public int Id;
        public Plane Data;
        public ElementOrientation(Plane plane, int id)
        {
            Id = id;
            Data = plane;
        }
    }

    public class Node
    {
        public readonly double[] Data;
        public readonly int Id;

        public Node(double x, double y, double z, int id)
        {
            Id = id;
            Data = new double[] { x, y, z };
        }

        public Node()
        {
            Data = new double[3];
            Id = 0;
        }

        public double X
        { get { return Data[0]; } set { Data[0] = value; } }
        public double Y
        { get { return Data[1]; } set { Data[1] = value; } }
        public double Z
        { get { return Data[2]; } set { Data[2] = value; } }
    }

    public class Part
    {
        public string Name;
        public List<Node> Nodes;
        public List<Element> Elements;
        public List<ElementOrientation> ElementOrientations;
        public Material Material;

        public Part()
        {
            Name = "Part";
            Nodes = new List<Node>();
            Elements = new List<Element>();
            ElementOrientations = new List<ElementOrientation>();
            Material = new Material();
        }
    }

    public class Assembly
    {
        public string Name;
        public List<Part> Parts;

        public Assembly()
        {
            Parts = new List<Part>();
        }
    }

    public class Material
    {
        public string Name;
        public double Density;
        public double[] EngineeringConstants;

        public Material()
        {
            Name = "Material";
            Density = 1.0;
            EngineeringConstants = new double[9];
        }
    }

    public class BoundaryCondition
    {
        public string Name;
        public string Type;
        // NodeSet ... 

        public BoundaryCondition()
        {
            Name = "BC-1";
            Type = "Displacement/Rotation";
        }
    }

    public class Load
    {
        public string Name;
        public string Type;

        public Load()
        {
            Name = "Load-1";
            Type = "Gravity";
        }
    }
}
