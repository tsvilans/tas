using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace tas.Abaqus
{

    public struct Node
    {
        public double[] Data;
        public int Id;

        public Node(double x, double y, double z, int id)
        {
            Id = id;
            Data = new double[] { x, y, z };
        }

        /*
        public Node()
        {
            Data = new double[3];
            Id = 0;
        }
        */
        public double X
        { get { return Data[0]; } set { Data[0] = value; } }
        public double Y
        { get { return Data[1]; } set { Data[1] = value; } }
        public double Z
        { get { return Data[2]; } set { Data[2] = value; } }
    }
}
