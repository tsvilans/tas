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

    /// <summary>
    /// !!! WIP !!!
    /// Voxel Assembly (aka: Voxel Blank)
    /// Composed of straight lamellas or cross-lam, with multiple child 
    /// oriented glulams that describe the outer laminations.
    /// Voxel generation is driven by input mesh, but this has to
    /// take into account the child laminations, which it doesn't
    /// at the moment...
    /// </summary>
    public class VoxelAssembly : Assembly
    {
        List<Glulam> Blanks;

        private List<List<List<bool>>> VoxelGrid;
        private double VoxX, VoxY, VoxZ;
        private int ResX, ResY, ResZ;

        public void GenerateVoxels(Mesh m, double x, double y, double z)
        {
            VoxX = x; VoxY = y; VoxZ = z;
            VoxelGrid = new List<List<List<bool>>>();
            BoundingBox bb = m.GetBoundingBox(true);
            double hx = x / 2;
            double hy = y / 2;
            double hz = z / 2;

            ResX = (int)((bb.Max.X - bb.Min.X) / x);
            ResY = (int)((bb.Max.Y - bb.Min.Y) / y);
            ResZ = (int)((bb.Max.Z - bb.Min.Z) / z);

            for (int i = 0; i < ResX; ++i)
            {
                VoxelGrid.Add(new List<List<bool>>());
                for (int j = 0; j < ResY; ++j)
                {
                    VoxelGrid[i].Add(new List<bool>());
                    for (int k = 0; k < ResZ; ++k)
                    {
                        Point3d p = new Point3d(x * i + hx + bb.Min.X, y * j + hy + bb.Min.Y, z * k + hz + bb.Min.Z);
                        VoxelGrid[i][j].Add(m.IsPointInside(p, 0.01, false));
                    }
                }
            }

            List<Mesh> VoxelMeshes = new List<Mesh>();
            for (int i = 0; i < ResX; ++i)
            {
                for (int j = 0; j < ResY; ++j)
                {
                    for (int k = 0; k < ResZ; ++k)
                    {
                        if (VoxelGrid[i][j][k] == true) continue;
                        Box b = new Box(Plane.WorldXY,
                          new Interval(bb.Min.X + i * x, bb.Min.X + (i + 1) * x),
                          new Interval(bb.Min.Y + j * y, bb.Min.Y + (j + 1) * y),
                          new Interval(bb.Min.Z + k * z, bb.Min.Z + (k + 1) * z));
                        Mesh mbox = Rhino.Geometry.Mesh.CreateFromBox(b, 1, 1, 1);

                        VoxelMeshes.Add(mbox);
                    }
                }
            }

            Rhino.Geometry.Intersect.MeshClash[] clashes =
                 Rhino.Geometry.Intersect.MeshClash.Search(new Mesh[] { m }, VoxelMeshes, 5.0, VoxelMeshes.Count);
            for (int i = 0; i < clashes.Length; ++i)
            {
                Mesh clash = clashes[i].MeshB;
                Point3d c = m.GetBoundingBox(true).Center;
                int ix = (int)((c.X - bb.Min.X) / x);
                int iy = (int)((c.Y - bb.Min.Y) / y);
                int iz = (int)((c.Z - bb.Min.Z) / z);

                VoxelGrid[ix][iy][iz] = true;
            }
        }

        public List<Line> GetVoxelLamellas()
        {
            double hx, hy, hz;
            hx = VoxX / 2; hy = VoxY / 2; hz = VoxZ / 2;

            List<Line> Lines = new List<Line>();
            Point3d Start = new Point3d(hx, 0, hz);
            Point3d End = new Point3d(Start);
            bool Active = false;

            for (int i = 0; i < ResZ; ++i)
            {
                for (int j = 0; j < ResX; ++j)
                {
                    if (Active)
                    {
                        Lines.Add(new Line(Start, End));
                        Start = new Point3d(End);
                        Active = false;
                    }

                    for (int k = 0; k < ResY; ++k)
                    {
                        //counter++;
                        if (VoxelGrid[i][j][k])
                        {
                            End.Y += VoxY;
                            Active = true;
                        }
                        else
                        {
                            if (Active)
                            {
                                Lines.Add(new Line(Start, End));
                                Start = new Point3d(End);
                                Active = false;
                            }
                        }
                    }
                }
            }
            return Lines;
        }

        public override Mesh[] ToMesh()
        {
            double hx, hy, hz;
            hx = VoxX / 2; hy = VoxY / 2; hz = VoxZ / 2;



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

        public override Assembly Duplicate()
        {
            VoxelAssembly vox = new VoxelAssembly();
            vox.BasePlane = this.BasePlane;
            vox.VoxX = VoxX;
            vox.VoxY = VoxY;
            vox.VoxZ = VoxZ;
            vox.ResX = ResX;
            vox.ResY = ResY;
            vox.ResZ = ResZ;

            for (int x = 0; x < VoxelGrid.Count; ++x)
            {
                vox.VoxelGrid.Add(new List<List<bool>>());
                for (int y = 0; y < VoxelGrid[x].Count; ++y)
                {
                    vox.VoxelGrid[x].Add(new List<bool>());
                    for (int z = 0; z < VoxelGrid[x][y].Count; ++z)
                    {
                        vox.VoxelGrid[x][y].Add(VoxelGrid[x][y][z]);
                    }
                }
            }
            return vox;
        }

        public override bool TryGetWidthAndHeight(out double Width, out double Height, int part_index = 0)
        {
            Width = -1; Height = -1;
            return false;
        }

        public override Glulam[] GetAllGlulams()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return "VoxelAssembly";
        }

        public override Glulam GetSubElement(int part_index = 0)
        {
            throw new NotImplementedException();
        }

        public override int GetClosestSubElement(Point3d p, out double t)
        {
            throw new NotImplementedException();
        }
    }
}
