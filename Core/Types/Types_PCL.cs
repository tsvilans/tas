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

using Rhino.Geometry;
using System.Globalization;

namespace tas.Core.IO
{
    internal enum Fields
    {
        x, y, z, r, g, b, intensity, normal_x, normal_y, normal_z
    };

    internal class PCD_PointBuilder
    {
        public PCD_PointBuilder()
        {
            for (int i = 0; i < 10; ++i)
            {
                data[i] = 0.0;
            }
        }
        public object[] data = new object[10];
        public Point3d Point()
        {
            return new Point3d(
                System.Convert.ToDouble(data[0], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(data[1], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(data[2], CultureInfo.InvariantCulture));
        }
        public Vector3d Normal()
        {
            return new Vector3d(
                System.Convert.ToDouble(data[7], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(data[8], CultureInfo.InvariantCulture),
                System.Convert.ToDouble(data[9], CultureInfo.InvariantCulture));
        }
        public int Color()
        {
            int col = System.Convert.ToInt32(data[3], CultureInfo.InvariantCulture);
            col |= System.Convert.ToInt32(data[4], CultureInfo.InvariantCulture) << 8;
            col |= System.Convert.ToInt32(data[5], CultureInfo.InvariantCulture) << 16;
            col |= System.Convert.ToInt32(data[6], CultureInfo.InvariantCulture) << 24;
            return col;
        }
        public int Intensity()
        {
            int col = System.Convert.ToInt32(data[6], CultureInfo.InvariantCulture);
            col |= col << 8;
            col |= col << 16;
            return col;
        }
    }

    internal class PCD_Field
    {
        public virtual void read(byte[] buffer, int pos, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = null;
        }
        public virtual void readASCII(string str, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = null;
        }
        public int id = 0;
        public int size = 0;
    }

    internal class PCD_Field_4F : PCD_Field
    {
        public PCD_Field_4F() { size = 4; }
        public override void read(byte[] buffer, int pos, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = (double)System.BitConverter.ToSingle(buffer, pos);
        }
        public override void readASCII(string str, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = System.Convert.ToSingle(str, CultureInfo.InvariantCulture);
        }
    }

    internal class PCD_Field_4U : PCD_Field
    {
        public PCD_Field_4U() { size = 4; }
        public override void read(byte[] buffer, int pos, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = (int)System.BitConverter.ToUInt32(buffer, pos);
        }
        public override void readASCII(string str, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = System.Convert.ToInt32(str, CultureInfo.InvariantCulture);
        }
    }

    internal class PCD_Field_1U : PCD_Field
    {
        public PCD_Field_1U() { size = 1; }
        public override void read(byte[] buffer, int pos, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = (int)buffer[pos];
        }
        public override void readASCII(string str, ref PCD_PointBuilder pbb)
        {
            if (id > -1)
                pbb.data[id] = System.Convert.ToInt32(str, CultureInfo.InvariantCulture);
        }
    }

    internal class PCD_Field_None : PCD_Field
    {
        public override void read(byte[] buffer, int pos, ref PCD_PointBuilder pbb) { }
    }

}