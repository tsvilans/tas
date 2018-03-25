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

namespace tas.Lam
{
    public abstract class Assembly
    {
        protected Assembly(Plane p)
        {
            Id = Guid.NewGuid();
            BasePlane = p;
        }

        protected Assembly() : this(Plane.WorldXY) { }

        public Plane BasePlane;
        public Guid Id { get; protected set; }

        public abstract Mesh[] ToMesh();
        public abstract Brep[] ToBrep();
        public abstract void Transform(Transform x);
        public abstract Assembly Duplicate();
        public abstract bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0);
        public abstract Glulam[] GetAllGlulams();
        public abstract Glulam GetSubElement(int part_index = 0);
        public abstract int GetClosestSubElement(Point3d p, out double t);

        public override bool Equals(object obj)
        {
            if (obj.GetType() == this.GetType() && obj.GetHashCode() == this.GetHashCode())
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return "GlulamAssembly";
        }

        public virtual byte[] ToByteArray()
        {
            List<byte> b = new List<byte>();
            b.AddRange(Id.ToByteArray());
            b.AddRange(BitConverter.GetBytes(BasePlane.OriginX));
            b.AddRange(BitConverter.GetBytes(BasePlane.OriginY));
            b.AddRange(BitConverter.GetBytes(BasePlane.OriginZ));

            b.AddRange(BitConverter.GetBytes(BasePlane.XAxis.X));
            b.AddRange(BitConverter.GetBytes(BasePlane.XAxis.Y));
            b.AddRange(BitConverter.GetBytes(BasePlane.XAxis.Z));

            b.AddRange(BitConverter.GetBytes(BasePlane.YAxis.X));
            b.AddRange(BitConverter.GetBytes(BasePlane.YAxis.Y));
            b.AddRange(BitConverter.GetBytes(BasePlane.YAxis.Z));

            return b.ToArray();
        }
    }

    public abstract class DirectedAssembly : Assembly
    {
        public Curve[] Centrelines;

        protected DirectedAssembly(Curve centreline, Plane p) : base(p)
        {
            Centrelines = new Curve[] { centreline };
        }

        protected DirectedAssembly(Curve[] centrelines, Plane p) : base(p)
        {
            Centrelines = centrelines;
        }

        protected DirectedAssembly(Curve centreline) : base()
        {
            Centrelines = new Curve[] { centreline };
        }

        protected DirectedAssembly(Curve[] centrelines) : base()
        {
            Centrelines = centrelines;
        }

        public abstract Plane GetPlane(double t, int part_index = 0);

        public override string ToString()
        {
            return "DirectedAssembly";
        }
    }
}
