using System;
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core.Types;
using tas.Core.GH;

namespace tas.Machine.GH.Toolpaths
{
    public class Cmpt_FlankMilling : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Cmpt_FlankMilling class.
        /// </summary>
        public Cmpt_FlankMilling()
          : base("Finishing - Flank Milling", "Flank",
              "Machine a ruled surface with the side of the tool.",
              "tasTools", "Toolpaths")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Surface as a Brep.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Side", "S", "Side of the surface to use for flank machining as a bitmask.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("NumSamples", "N", "Number of samples for the path.", GH_ParamAccess.item, 20);
            pManager.AddNumberParameter("ToolDiameter", "TD", "Tool diameter.", GH_ParamAccess.item, 12.0);
            pManager.AddNumberParameter("PathExtension", "Pe", "Extend the path by an amount.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("DepthExtension", "De", "Extend the depth of the rules by an amount.", GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("MaxDepth", "Md", "Maximum depth of the cut.", GH_ParamAccess.item, 50.0);
            pManager.AddNumberParameter("StepDown", "Sd", "Stepdown amount for each pass.", GH_ParamAccess.item, 8.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "P", "Toolpath as list of PPolyline objects.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep iBrep = null;
            int iSide = 0, iN = 20;
            double iToolDiameter = 12.0, iPathExt = 10.0, iDepthExt = 3.0, iMaxDepth = 50.0, iStepDown = 8.0;

            DA.GetData("Brep", ref iBrep);
            DA.GetData("Side", ref iSide);
            DA.GetData("NumSamples", ref iN);
            DA.GetData("ToolDiameter", ref iToolDiameter);
            DA.GetData("PathExtension", ref iPathExt);
            DA.GetData("DepthExtension", ref iDepthExt);
            DA.GetData("MaxDepth", ref iMaxDepth);
            DA.GetData("StepDown", ref iStepDown);


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