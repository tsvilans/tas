using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace tas.Abaqus
{

    public abstract class Element
    {
        public int Id { get; set; }
        public int[] Data;

        public virtual string Type { get { return "BASE_ELEMENT"; } }

        public virtual double[] Centre()
        {
            return new double[0];
        }
    }

    public class C3D8 : Element
    {
        public override string Type { get { return "C3D8"; } }

        public C3D8()
        {
        }

        public C3D8(int a, int b, int c, int d, int e, int f, int g, int h)
        {
            Id = -1;
            Data = new int[] { a, b, c, d, e, f, g, h };
        }

        public C3D8(int[] data)
        {
            Id = -1;
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

    public class C3D4 : Element
    {
        public override string Type { get { return "C3D4"; } }

        public C3D4()
        {
        }

        public C3D4(int a, int b, int c, int d)
        {
            Id = -1;
            Data = new int[] { a, b, c, d };
        }

        public C3D4(int[] data)
        {
            Id = -1;
            Data = new int[4];
            Array.Copy(data, Data, Math.Min(data.Length, 4));
        }

        public int A
        { get { return Data[0]; } set { Data[0] = value; } }
        public int B
        { get { return Data[1]; } set { Data[1] = value; } }
        public int C
        { get { return Data[2]; } set { Data[2] = value; } }
        public int D
        { get { return Data[3]; } set { Data[3] = value; } }

    }

    public class C3D10M : Element
    {
        public override string Type { get { return "C3D10M"; } }

        public C3D10M()
        {
        }

        public C3D10M(int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
        {
            Id = -1;
            Data = new int[] { a, b, c, d, e, f, g, h, i, j };
        }

        public C3D10M(int[] data)
        {
            Id = -1;
            Data = new int[10];
            Array.Copy(data, Data, Math.Min(data.Length, 10));
        }

        public int A { get { return Data[0]; } set { Data[0] = value; } }
        public int B { get { return Data[1]; } set { Data[1] = value; } }
        public int C { get { return Data[2]; } set { Data[2] = value; } }
        public int D { get { return Data[3]; } set { Data[3] = value; } }
        public int E { get { return Data[4]; } set { Data[4] = value; } }
        public int F { get { return Data[5]; } set { Data[5] = value; } }
        public int G { get { return Data[6]; } set { Data[6] = value; } }
        public int H { get { return Data[7]; } set { Data[7] = value; } }
        public int I { get { return Data[8]; } set { Data[8] = value; } }
        public int J { get { return Data[9]; } set { Data[9] = value; } }

    }

}
