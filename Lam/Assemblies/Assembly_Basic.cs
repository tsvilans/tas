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
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

using tas.Core;

namespace tas.Lam
{
    public class BasicAssembly : DirectedAssembly
    {
        public Glulam Blank;

        public BasicAssembly(Glulam blank, Plane p): base(blank.Centreline, p)
        {
            Blank = blank;
            Centrelines = new Curve[] { blank.Centreline };
        }

        public BasicAssembly(Glulam blank) : base(blank.Centreline, blank.Centreline.GetAlignedPlane(100))
        {
            Blank = blank;
            Centrelines = new Curve[] { blank.Centreline };
        }

        public override Plane GetPlane(double t, int part_index = 0)
        {
            return Blank.GetPlane(t);
        }

        public override Mesh[] ToMesh()
        {
            return new Mesh[] { Blank.GetBoundingMesh() };
        }

        public override Brep[] ToBrep()
        {
            return new Brep[] { Blank.GetBoundingBrep() };
        }

        public override void Transform(Transform x)
        {
            Blank.Transform(x);
        }

        public override Assembly Duplicate()
        {
            BasicAssembly bas = new BasicAssembly(this.Blank, this.BasePlane);
            return bas;
        }

        public override bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0)
        {
            Width = Blank.Data.NumWidth * Blank.Data.LamWidth;
            Height = Blank.Data.NumHeight * Blank.Data.LamHeight;
            return true;
        }

        public override Glulam[] GetAllGlulams()
        {
            return new Glulam[] { Blank };
        }

        public override Glulam GetSubElement(int part_index = 0)
        {
            return Blank;
        }

        public Curve Centreline()
        {
            return Centrelines[0];
        }

        public override string ToString()
        {
            return "BasicAssembly";
        }

        public override byte[] ToByteArray()
        {
            List<byte> b = new List<byte>();
            b.AddRange(base.ToByteArray());
            /*
            byte[] blank_bytes = Blank.ToByteArray();
            b.AddRange(BitConverter.GetBytes((Int32)blank_bytes.Length));
            b.AddRange(Blank.ToByteArray());
            */
            return b.ToArray();
        }

        public override int GetClosestSubElement(Point3d p, out double t)
        {
            Centrelines[0].ClosestPoint(p, out t);
            return 0;
        }
    }
}
