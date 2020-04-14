using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino.Geometry;
using tas.Lam;

namespace tas.Abaqus
{
    public static class AbaqusExtensions
    {
        public static void GetOrientation(this Glulam g, Point3d pt, out Plane ori)
        {
            ori = g.GetPlane(pt);
        }

        public static void CreateGlulamNodesAndElements(this Glulam g, out List<Node> nodes, out List<Element> elements, int Nx = 4, int Ny = 4, int Nz = 50)
        {
            double hWidth = g.Width() / 2;
            double hHeight = g.Height() / 2;

            double stepX = g.Width() / Nx;
            double stepY = g.Height() / Ny;

            List<Point3d> xPts = new List<Point3d>();

            for (int y = 0; y <= Ny; ++y)
            {
                for (int x = 0; x <= Nx; ++x)
                {
                    Point3d pt = new Point3d(
                        -hWidth + x * stepX,
                        -hHeight + y * stepY,
                        0);
                    xPts.Add(pt);
                }
            }

            Plane[] xplanes;
            double[] xt;

            g.GenerateCrossSectionPlanes(Nz + 1, 0, out xplanes, out xt, GlulamData.Interpolation.LINEAR);

            nodes = new List<Node>();

            int i = 0;
            for (int z = 0; z <= Nz; ++z)
            {
                Transform xform = Transform.PlaneToPlane(Plane.WorldXY, xplanes[z]);
                for (int j = 0; j < xPts.Count; ++j)
                {
                    Point3d pt = new Point3d(xPts[j]);
                    pt.Transform(xform);
                    nodes.Add(new Node(pt.X, pt.Y, pt.Z, i + 1));
                    i++;
                }
            }

            elements = new List<Element>();

            int sz = (Nx + 1) * (Ny + 1);
            int sy = (Nx + 1);

            i = 0;
            for (int z = 0; z < Nz; ++z)
            {
                int cz = z * sz;
                for (int y = 0; y < Ny; ++y)
                {
                    int cy = y * sy;
                    for (int x = 0; x < Nx; ++x)
                    {
                        int[] indices = new int[] {
                            cz + cy + x,
                            cz + cy + x + 1,
                            cz + cy + x + sy + 1,
                            cz + cy + x + sy,
                            cz + sz + cy + x,
                            cz + sz + cy + x + 1,
                            cz + sz + cy + x + sy + 1,
                            cz + sz + cy + x + sy};

                        for (int ii = 0; ii < 8; ++ii)
                        {
                            indices[ii] = nodes[indices[ii]].Id;
                        }

                        Element ele = new Element(indices);
                        ele.Id = i + 1;

                        elements.Add(ele);
                        ++i;
                    }
                }
            }

        }

        public static void CreateElementOrientations(this Glulam g, List<Node> nodes, List<Element> elements, out List<ElementOrientation> orientations)
        {
            orientations = new List<ElementOrientation>(elements.Count);

            foreach (Element ele in elements)
            {
                Point3d centre = new Point3d(
                    ele.Data.Select(x => nodes[x-1].X).ToArray().Sum() / 8,
                    ele.Data.Select(x => nodes[x-1].Y).ToArray().Sum() / 8,
                    ele.Data.Select(x => nodes[x-1].Z).ToArray().Sum() / 8
                    );

                Plane ori = Plane.Unset;

                GetOrientation(g, centre, out ori);

                orientations.Add(new ElementOrientation(ori, ele.Id));
            }
        }
    }

}
