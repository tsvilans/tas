using System;
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;
using tas.Core.Types;
using tas.Core.GH;

namespace tas.Machine.GH.Toolpaths
{
    public class Cmpt_FlankMilling : ToolpathBase_Component
    {
        public Cmpt_FlankMilling()
          : base("Finishing - Flank Milling", "Flank",
              "Machine a ruled surface with the side of the tool.",
              "tasMachine", "Toolpaths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            base.RegisterInputParams(pManager);

            pManager.AddBrepParameter("Brep", "B", "Surface as a Brep.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Side", "S", "Side of the surface to use for flank machining as a bitmask.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Zigzag", "Z", "Alternate the cutting direction of each pass.", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("PathExtension", "Pe", "Extend the path by an amount.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("DepthExtension", "De", "Extend the depth of the rules by an amount.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("MaxDepth", "Md", "Maximum depth of the cut.", GH_ParamAccess.item, 50.0);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            base.RegisterOutputParams(pManager);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep m_brep = null;
            int m_side = 0;
            double m_path_extension = 10.0, m_depth_extension = 3.0, m_max_depth = 50.0;

            if (!DA.GetData("Workplane", ref Workplane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Workplane missing. Default used (WorldXY).");
            }

            if (!DA.GetData("MachineTool", ref Tool))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "MachineTool missing. Default used.");
            }

            bool zigzag = false;
            DA.GetData("Brep", ref m_brep);
            DA.GetData("Side", ref m_side);
            DA.GetData("PathExtension", ref m_path_extension);
            DA.GetData("DepthExtension", ref m_depth_extension);
            DA.GetData("MaxDepth", ref m_max_depth);
            DA.GetData("Zigzag", ref zigzag);

            if (m_brep == null) return;

            // Find the UV direction
            int D = (m_side & (1 << 0)) > 0 ? 1 : 0;
            int nD = (m_side & (1 << 0)) > 0 ? 0 : 1;

            // Find out if we are on the opposite edges
            int S = (m_side & (1 << 1)) > 0 ? 1 : 0;
            int nS = (m_side & (1 << 1)) > 0 ? 0 : 1;


            BrepFace face = m_brep.Faces[0];

            // Warn if the surface is not suitable for flank machining
            if (face.Degree(nD) != 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Surface isn't ruled in this direction!");
            }

            // Get appropriate surface edge
            Curve flank_edge = m_brep.Faces[0].IsoCurve(D, m_brep.Faces[0].Domain(nD)[S]);


            // Discretize flank edge using angle and length tolerances
            Polyline flank_pts = flank_edge.ToPolyline(0, 0, 0.01, 0.1, 0, 0.1, 0, 0, true).ToPolyline();

            // If we are on opposite edge, reverse the flank curve for consistent direction
            if (S > 0)
                flank_pts.Reverse();

            // Get corresponding edge parameter from discretized curve points
            int N = flank_pts.Count;
            double[] tt = new double[N];
            for (int i = 0; i < N; ++i)
            {
                flank_edge.ClosestPoint(flank_pts[i], out tt[i]);
            }

            // Get all isocurves corresponding to the vertices of the discretized edge curev
            var rules = tt.Select(x => m_brep.Faces[0].IsoCurve(nD, x));

            // Get the length of all isocurves
            var lengths = rules.Select(x => x.GetLength()).ToList();

            List<Vector3d> directions;

            // Make sure to use tangent at right end of the isocurve 
            // (S == 1 when we are on the opposite side)
            if (S == 0)
                directions = rules.Select(x => x.TangentAtStart).ToList();
            else
                directions = rules.Select(x => -x.TangentAtEnd).ToList();

            // Determine maximum depth based on longest isocurve and depth limit
            double deepest = Math.Min(m_max_depth, lengths.Max());

            // Determine number of passes based on deepest cut and stepdown
            int passes = (int)Math.Ceiling(deepest / Tool.StepDown);


            List<PPolyline> paths = new List<PPolyline>();

            // Declare variables for Brep closest point calculation
            double u, v;
            Vector3d normal;

            // Create paths for each pass
            for (int i = 0; i <= passes; ++i)
            {
                PPolyline path = new PPolyline();
                for (int j = 0; j < N; ++j)
                {
                    // Find closest normal
                    face.ClosestPoint(flank_pts[j], out u, out v);
                    normal = face.NormalAt(u, v);

                    // Calculate pass depth
                    double depth = Math.Min(m_max_depth, 
                        Math.Min(
                            lengths[j] + m_depth_extension, 
                            Tool.StepDown * i));

                    Point3d origin = flank_pts[j]
                      + (directions[j] * (Math.Min(m_max_depth, lengths[j] + m_depth_extension) / passes * i)
                      + (normal * Tool.StepOver / 2));

                    path.Add(new Plane(origin, directions[j]));

                }

                // If the path is extended, add extra planes at the start and end
                if (m_path_extension > 0.0)
                {
                    // Path vector at start
                    Vector3d start = new Vector3d(path.First.Origin - path[1].Origin);
                    start.Unitize();

                    // Path vector at end
                    Vector3d end = new Vector3d(path.Last.Origin - path[path.Count - 2].Origin);
                    end.Unitize();

                    Plane pStart = path.First;
                    Plane pEnd = path.Last;

                    // Shift plane origins by path vectors
                    pStart.Origin = pStart.Origin + start * m_path_extension;
                    pEnd.Origin = pEnd.Origin + end * m_path_extension;

                    // Add extension planes to path
                    path.Insert(0, pStart);
                    path.Add(pEnd);
                }
                if (zigzag && i.Modulus(2) > 0)
                {
                    path.Reverse();
                }
                paths.Add(path);
            }

            /*
                        #region OLD
                        int D = (iSide & (1 << 0)) > 0 ? 1 : 0;
                        int nD = (iSide & (1 << 0)) > 0 ? 0 : 1;

                        int S = (iSide & (1 << 1)) > 0 ? 1 : 0;
                        int nS = (iSide & (1 << 1)) > 0 ? 0 : 1;


                        double tMin = iBrep.Faces[0].Domain(nD).Min;
                        double tMax = iBrep.Faces[0].Domain(nD).Max;

                        Curve[] cEdges = new Curve[2];

                        cEdges[S] = iBrep.Faces[0].IsoCurve(D, tMin);
                        cEdges[nS] = iBrep.Faces[0].IsoCurve(D, tMax);

                        double[] len = new double[2];
                        for (int i = 0; i < 2; ++i)
                        {
                            len[i] = cEdges[i].GetLength();
                        }

                        List<Point3d>[] subdivs = new List<Point3d>[2];
                        for (int i = 0; i < 2; ++i)
                        {
                            subdivs[i] = new List<Point3d>();
                            double[] ts = cEdges[i].DivideByCount(iN - 1, true);
                            subdivs[i].AddRange(ts.Select(x => cEdges[i].PointAt(x)));
                        }

                        List<Line> rules = new List<Line>();
                        List<Vector3d> directions = new List<Vector3d>();

                        for (int i = 0; i < iN; ++i)
                        {
                            rules.Add(new Line(subdivs[0][i], subdivs[1][i]));
                            Vector3d d = new Vector3d(subdivs[0][i] - subdivs[1][i]);
                            d.Unitize();
                            directions.Add(d);
                        }


                        double max = 0;

                        for (int i = 0; i < iN; ++i)
                        {
                            max = Math.Min(Math.Max(max, rules[i].Length + iDepthExt), iMaxDepth);
                        }

                        int Nsteps = (int)Math.Ceiling(max / iStepDown);

                        List<PPolyline> paths = new List<PPolyline>();
                        Point3d cpt;
                        ComponentIndex ci;
                        double s, t;
                        Vector3d normal;

                        for (int i = 0; i < Nsteps; ++i)
                        {
                            PPolyline poly = new PPolyline();
                            for (int j = 0; j < rules.Count; ++j)
                            {
                                double l = Math.Min(rules[j].Length + iDepthExt, iMaxDepth) / Nsteps;
                                Point3d pt = rules[j].From - directions[j] * l * i;
                                iBrep.ClosestPoint(pt, out cpt, out ci, out s, out t, 3.0, out normal);
                                poly.Add(new Plane(pt + normal * iToolDiameter / 2, Vector3d.CrossProduct(directions[j], normal), normal));
                            }

                            if (iPathExt > 0.0)
                            {
                                Vector3d start = new Vector3d(poly.First.Origin - poly[1].Origin);
                                start.Unitize();

                                Vector3d end = new Vector3d(poly.Last.Origin - poly[poly.Count - 2].Origin);
                                end.Unitize();

                                Plane pStart = poly.First;
                                Plane pEnd = poly.Last;
                                pStart.Origin = pStart.Origin + start * iPathExt;
                                pEnd.Origin = pEnd.Origin + end * iPathExt;
                                poly.Insert(0, pStart);
                                poly.Add(pEnd);
                            }

                            paths.Add(poly);
                        }

                        #region Last layer

                        PPolyline last = new PPolyline();
                        for (int j = 0; j < rules.Count; ++j)
                        {
                            double l = Math.Min(rules[j].Length + iDepthExt, iMaxDepth);
                            Point3d pt = rules[j].From - directions[j] * l;
                            iBrep.ClosestPoint(pt, out cpt, out ci, out s, out t, 3.0, out normal);
                            last.Add(new Plane(pt + normal * iToolDiameter / 2, Vector3d.CrossProduct(directions[j], normal), normal));
                        }

                        if (iPathExt > 0.0)
                        {
                            Vector3d start = new Vector3d(last.First.Origin - last[1].Origin);
                            start.Unitize();

                            Vector3d end = new Vector3d(last.Last.Origin - last[last.Count - 2].Origin);
                            end.Unitize();

                            Plane pStart = last.First;
                            Plane pEnd = last.Last;
                            pStart.Origin = pStart.Origin + start * iPathExt;
                            pEnd.Origin = pEnd.Origin + end * iPathExt;
                            last.Insert(0, pStart);
                            last.Add(pEnd);
                        }

                        last.Add(paths.First().Last);

                        paths.Add(last);

                        #endregion
                        #endregion
                        */


            DA.SetDataList("Paths", paths.Select(x => new GH_PPolyline(x)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_FlowlineFinish_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("14441c25-86da-4c29-a3b7-287a3436ed54"); }
        }
    }
}