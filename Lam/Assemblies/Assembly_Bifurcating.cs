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
    public class BifurcatingAssembly : DirectedAssembly
    {
        Glulam Trunk;
        Glulam[] Branches;

        // PLACEHOLDER
        public BifurcatingAssembly(Glulam trunk, Glulam[] branches) : base(new Curve[1 + branches.Length])
        {
            Centrelines[0] = trunk.Centreline;
            for (int i = 0; i < branches.Length; ++i)
            {
                Centrelines[1 + i] = branches[i].Centreline;
            }

            Trunk = trunk;
            Branches = branches;

            // TODO: check for continuity between trunk and branches
        }

        public override GlulamAssembly Duplicate()
        {
            Glulam new_trunk = Trunk.Duplicate();
            Glulam[] new_branches = new Glulam[Branches.Length];
            for (int i = 0; i < Branches.Length; ++i)
            {
                new_branches[i] = Branches[i].Duplicate();
            }

            return new BifurcatingAssembly(new_trunk, new_branches);
        }

        public override Plane GetPlane(double t, int part_index = 0)
        {
            if (part_index < 0 || part_index > Branches.Length + 1) return Plane.Unset;
            if (part_index == 0)
                return Trunk.GetPlane(t);
            else
                return Branches[part_index - 1].GetPlane(t);
        }

        public override Glulam GetSubElement(int part_index = 0)
        {
            if (part_index == 0) return Trunk;
            else if (part_index < Branches.Length + 1)
            {
                return Branches[part_index - 1];
            }
            throw new Exception("Element index out of bounds.");
        }

        public override Mesh[] ToMesh()
        {
            List<Mesh> meshes = Branches.Select(x => x.GetBoundingMesh()).ToList();
            meshes.Insert(0, Trunk.GetBoundingMesh());
            return meshes.ToArray();
        }

        public override Brep[] ToBrep()
        {
            List<Brep> breps = Branches.Select(x => x.GetBoundingBrep()).ToList();
            breps.Insert(0, Trunk.GetBoundingBrep());
            return breps.ToArray();
        }

        public override void Transform(Transform x)
        {
            Trunk.Transform(x);
            for (int i = 0; i < Branches.Length; ++i)
            {
                Branches[i].Transform(x);
            }
            BasePlane.Transform(x);
        }

        public override bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0)
        {
            Width = -1; Height = -1;
            if (part_index < 0 || part_index > Branches.Length + 1) return false;

            return false;
        }

        public override Glulam[] GetAllGlulams()
        {
            List<Glulam> lams = Branches.ToList();
            lams.Insert(0, Trunk);
            return lams.ToArray();
        }

        public override string ToString()
        {
            return "BifurcatingAssembly";
        }

        public override int GetClosestSubElement(Point3d p, out double t)
        {
            int index = 0;
            double d = double.MaxValue;
            double ttemp, dtemp;
            t = 0.0;

            for (int i = 0; i < Centrelines.Length; ++i)
            {
                Centrelines[i].ClosestPoint(p, out ttemp);
                dtemp = p.DistanceTo(Centrelines[i].PointAt(ttemp));

                if (dtemp < d)
                {
                    d = dtemp;
                    index = i;
                    t = ttemp;
                }
            }
            return index;
        }
    }

    public class BifurcatingAssembly2 : DirectedAssembly
    {
        Glulam[] Branches;
        public Ray3d[] Ends;

        static public double OverlapTolerance = 0.01;
        static public double Tolerance = 0.01;

        // PLACEHOLDER
        public BifurcatingAssembly2(Glulam[] branches) : this(branches, new Ray3d[3])
        {

        }

        public BifurcatingAssembly2(Glulam[] branches, Ray3d[] ends) : base(new Curve[2])
        {
            if (branches.Length != 2) throw new Exception("BifurcatingAssembly2 :: Needs exactly 2 Glulam inputs!");

            Branches = branches;
            Centrelines = Branches.Select(x => x.Centreline).ToArray();
            Branches = branches;
            Ends = ends;
        }

        public override GlulamAssembly Duplicate()
        {
            Glulam[] new_branches = new Glulam[Branches.Length];
            for (int i = 0; i < Branches.Length; ++i)
            {
                new_branches[i] = Branches[i].Duplicate();
            }

            return new BifurcatingAssembly2(new_branches);
        }

        public override Plane GetPlane(double t, int part_index = 0)
        {
            if (part_index < 0 || part_index > Branches.Length + 1) return Plane.Unset;
            return Branches[part_index].GetPlane(t);
        }

        public override Glulam GetSubElement(int part_index = 0)
        {
            if (part_index < Branches.Length)
            {
                return Branches[part_index];
            }
            throw new Exception("Element index out of bounds.");
        }

        public override Mesh[] ToMesh()
        {
            return Branches.Select(x => x.GetBoundingMesh()).ToArray();
        }

        public override Brep[] ToBrep()
        {
            return Branches.Select(x => x.GetBoundingBrep()).ToArray();
        }

        public override void Transform(Transform x)
        {
            for (int i = 0; i < Branches.Length; ++i)
                Branches[i].Transform(x);

            BasePlane.Transform(x);
        }

        public override bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0)
        {
            Width = -1; Height = -1;
            if (part_index < 0 || part_index > Branches.Length + 1) return false;

            return false;
        }

        public override Glulam[] GetAllGlulams()
        {
            return Branches.ToArray();
        }

        public override string ToString()
        {
            return "BifurcatingAssembly2";
        }

        public override int GetClosestSubElement(Point3d p, out double t)
        {
            int index = 0;
            double d = double.MaxValue;
            double ttemp, dtemp;
            t = 0.0;

            for (int i = 0; i < Centrelines.Length; ++i)
            {
                Centrelines[i].ClosestPoint(p, out ttemp);
                dtemp = p.DistanceTo(Centrelines[i].PointAt(ttemp));

                if (dtemp < d)
                {
                    d = dtemp;
                    index = i;
                    t = ttemp;
                }
            }
            return index;
        }
    }

    public class BifurcatingModel
    {
        public Plane[] Ends;
        public Glulam[] Branches;

        public Point3d[] CurvePoints;
        public Curve Curve;

        public BifurcatingModel(Plane[] ends, Glulam[] branches)
        {
            if (ends.Length != 3) throw new Exception("BifurcatingModel only has 3 ends!");
            if (branches.Length != 2) throw new Exception("BifuractingModel only has 2 branches!");

            Ends = ends;
            Branches = branches;
        }
    }

}
