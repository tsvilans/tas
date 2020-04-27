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
    public class BranchingAssembly : DirectedAssembly
    {
        Glulam MainTrunk;

        // PLACEHOLDER
        public BranchingAssembly(Glulam blank) : base(blank.Centreline)
        {
            MainTrunk = blank;
        }
        public override GlulamAssembly Duplicate()
        {
            throw new NotImplementedException();
        }

        public override Plane GetPlane(double t, int part_index = 0)
        {
            throw new NotImplementedException();
        }

        public override Mesh[] ToMesh()
        {
            throw new NotImplementedException();
        }

        public override Brep[] ToBrep()
        {
            throw new NotImplementedException();
        }

        public override void Transform(Transform x)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0)
        {
            Width = MainTrunk.Data.NumWidth * MainTrunk.Data.LamWidth;
            Height = MainTrunk.Data.NumHeight * MainTrunk.Data.LamHeight;
            return true;
        }

        public override Glulam[] GetAllGlulams()
        {
            throw new NotImplementedException();
        }

        public override Glulam GetSubElement(int part_index = 0)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "BranchingAssembly";
        }

        public override int GetClosestSubElement(Point3d p, out double t)
        {

            throw new NotImplementedException();
        }
    }
}
