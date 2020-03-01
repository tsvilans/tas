using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using tas.Core;
using tas.Lam;

namespace tas.Lam.GH.Components
{
    public class Cmpt_AnalyzeRibbonEdges : GH_Component
    {
        List<Line> m_line_list = new List<Line>();
        List<double> m_length_list = new List<double>();

        tas.Core.Util.Gradient m_grad;

        bool m_drawK = false;
        bool m_display_enabled = true;
        double m_radius_factor = 0.005;

        public Cmpt_AnalyzeRibbonEdges()
          : base("Analyze Ribbon Edges", "Ribbon",
              "Tests if a Glulam can be made out of a ribbon-like lamella, instead of sticks.",
              "tasLam", "Analyze")
        {

            if (m_grad == null)
            {
                List<double> stops = new List<double> { 0.0, 0.25, 0.5, 0.75, 1.0 };
                List<System.Drawing.Color> colors = new List<System.Drawing.Color>{
            System.Drawing.Color.FromArgb(255, 0, 0),
            System.Drawing.Color.FromArgb(255, 128, 64),
            System.Drawing.Color.FromArgb(255, 255, 255),
            System.Drawing.Color.FromArgb(64, 128, 255),
            System.Drawing.Color.FromArgb(0, 0, 255)};

                m_grad = new tas.Core.Util.Gradient(stops, colors);
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam blank", "B", "Glulam blank to analyze.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius factor", "R", "Multiplier factor for bending limit (default = 200).", GH_ParamAccess.item, 200.0);
            pManager.AddNumberParameter("Offset", "O", "Offset in blank cross-section.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("FlipXY", "F", "Flip X- and Y-axes of cross-section.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Threshold", "T", "Display as gradient or as true / false.", GH_ParamAccess.item, false);
            pManager[0].DataMapping = GH_DataMapping.Flatten;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Max curvature", "MaxK", "Maximum curvature found.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //string msg = "";
            if (m_display_enabled)
            {
                List<object> RawBlanks = new List<object>();
                List<Glulam> Blanks = new List<Glulam>();

                if (!DA.GetDataList("Glulam blank", RawBlanks)) return;

                foreach (object obj in RawBlanks)
                {
                    if (obj is Glulam glulam)
                        Blanks.Add(glulam);
                    else if (obj is GH_Glulam gh_glulam)
                        Blanks.Add(gh_glulam.Value);
                }

                //if (DA.GetData("Radius factor", ref m_radius_factor))
                //{
                    //if (m_radius_factor <= 0.0) m_radius_factor = 0.001;
                    //m_radius_factor = 1 / m_radius_factor;
                //}

                double m_offset = 0.0;
                DA.GetData("Offset", ref m_offset);

                bool m_threshold = false;
                DA.GetData("Threshold", ref m_threshold);
                m_drawK = m_threshold;

                bool m_flip = false;
                DA.GetData("FlipXY", ref m_flip);


                m_line_list = new List<Line>();
                m_length_list = new List<double>();

                foreach (Lam.Glulam blank in Blanks)
                {
                    GetRibbonEdges(blank, m_offset, m_line_list, m_length_list, m_flip);
                }

                if (m_length_list.Count != m_line_list.Count)
                    throw new Exception("Something went awry...");
            }
        }

        void GetRibbonEdges(Glulam g, double Offset, List<Line> lines, List<double> lengths, bool FlipXY = false)
        {
            double[] DivParams;
            Plane[] xPlanes;

            g.GenerateCrossSectionPlanes(g.Data.Samples, 0, out xPlanes, out DivParams, g.Data.InterpolationType);
            Plane pplane, prevplane;

            double w;

            if (FlipXY)
                w = g.Data.LamHeight * g.Data.NumHeight;
            else
                w = g.Data.LamWidth * g.Data.NumWidth;

            double hw = w / 2;
            double l;

            Point3d p0, p1;
            Point3d p2, p3;

            // First section
            pplane = xPlanes.First();

            if (FlipXY)
            {
                p0 = pplane.Origin + pplane.YAxis * hw + pplane.XAxis * Offset;
                p1 = pplane.Origin + pplane.YAxis * -hw + pplane.XAxis * Offset;
            }
            else
            {
                p0 = pplane.Origin + pplane.XAxis * hw + pplane.YAxis * Offset;
                p1 = pplane.Origin + pplane.XAxis * -hw + pplane.YAxis * Offset;
            }

            p2 = p0;
            p3 = p1;
            prevplane = pplane;

            for (int i = 1; i < g.Data.Samples; ++i)
            {
                pplane = xPlanes[i];

                if (FlipXY)
                {
                    p0 = pplane.Origin + pplane.YAxis * hw + pplane.XAxis * Offset;
                    p1 = pplane.Origin + pplane.YAxis * -hw + pplane.XAxis * Offset;
                }
                else
                {
                    p0 = pplane.Origin + pplane.XAxis * hw + pplane.YAxis * Offset;
                    p1 = pplane.Origin + pplane.XAxis * -hw + pplane.YAxis * Offset;
                }

                if (FlipXY)
                    l = (pplane.Origin + pplane.XAxis * Offset).DistanceTo(prevplane.Origin + prevplane.XAxis * Offset);
                else
                    l = (pplane.Origin + pplane.YAxis * Offset).DistanceTo(prevplane.Origin + prevplane.YAxis * Offset);

                lines.Add(new Line(p0, p2));
                lines.Add(new Line(p1, p3));

                lengths.Add((p0.DistanceTo(p2) / l - 1.0) / m_radius_factor);
                lengths.Add((p1.DistanceTo(p3) / l - 1.0) / m_radius_factor);

                p2 = p0;
                p3 = p1;
                prevplane = pplane;

            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("dd45ff0a-f0a7-46f0-abfb-593a81110f6f"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            double c;
            double l;
            if (m_display_enabled)
            {
                for (int i = 0; i < m_line_list.Count; ++i)
                {
                    l = m_length_list[i];
                    if (m_drawK)
                    {
                        if (l <= -1.0)
                            c = 0;
                        else if (l >= 1.0)
                            c = 1;
                        else
                            c = 0.5;
                        //c = Math.Abs(0.5 - m_length_list[i]) > m_radius_factor ? 1.0 : 0.0;
                    }
                    else
                        c = Math.Max(
                            0, Math.Min(
                                1.0, ((l + 1.0) / 2)));

                    args.Display.DrawLine(m_line_list[i], m_grad.GetColor(c), 1);

                }
            }
        }
    }
}