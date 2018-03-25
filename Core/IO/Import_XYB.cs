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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

namespace tas.Core.IO
{
    public class XYB_Importer
    {
        public XYB_Importer()
        {
            this.log = "";
            this.scale = 1000.0;
        }

        // num points
        public int num_points;
        private int length;
        private int header_length;
        public double scale;
        private int resolution;

        // scanner position and orientation
        public Point3d scan_position;
        public Quaternion scan_orientation;

        public string log;

        private System.IO.BinaryReader br;

        private bool OpenFile(string path, out string header)
        {
            header = "";
            br = new System.IO.BinaryReader(
                System.IO.File.Open(path, System.IO.FileMode.Open));
            if (br == null)
                return false;

            byte b;
            int pos = 0;
            int zero = 0;
            this.length = (int)br.BaseStream.Length;
            this.log += string.Format("Byte stream length: {0}\n", length);

            while (zero < 4 && pos < 500)
            {
                b = br.ReadByte();
                if (b == 0)
                {
                    zero++;
                }
                else
                {
                    zero = 0;
                }
                pos++;
            }
            this.header_length = pos;

            this.log += string.Format("Approx. number of points: {0}\n", (length - pos) / 26);
            this.br.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] header_bytes = br.ReadBytes(this.header_length);
            header = System.Text.Encoding.Default.GetString(header_bytes);
            this.log += header + "\n";
            return true;
        }

        public bool Peek(string path)
        {
            string header;
            if (!OpenFile(path, out header))
            {
                if (this.br != null)
                    this.br.Close();
                return false;
            }
            return true;
        }

        int UInt16toColor(ushort us)
        {
            int col = us / 16;
            col |= col << 8;
            col |= col << 16;
            return col;
        }

        void AddXYBPointToCloud(ref PointCloud pc, ref System.IO.BinaryReader reader)
        {
            double x, y, z;
            x = reader.ReadDouble() * this.scale;
            y = reader.ReadDouble() * this.scale;
            z = reader.ReadDouble() * this.scale;
            ushort i = reader.ReadUInt16();
            pc.Add(new Point3d(x, y, z));
            System.Drawing.Color col = System.Drawing.Color.FromArgb(UInt16toColor(i));

            pc[pc.Count - 1].Color = col;

            for (int j = 0; j < resolution - 1; ++j)
            {
                x = reader.ReadDouble() * this.scale;
                y = reader.ReadDouble() * this.scale;
                z = reader.ReadDouble() * this.scale;
                i = reader.ReadUInt16();
            }
        }

        public bool Import(string path, out PointCloud pc, bool intensity = false, int res = 4)
        {
            resolution = res;
            pc = new PointCloud();
            string header;
            if (!OpenFile(path, out header))
            {
                if (this.br != null)
                    this.br.Close();
                return false;
            }
            int pos = this.header_length;

            while (pos < (this.length - 26 * resolution))
            {
                pos += 26 * resolution;
                AddXYBPointToCloud(ref pc, ref br);
            }

            br.Close();
            return true;
        }


    }
}
