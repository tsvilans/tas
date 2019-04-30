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
    public class Cmpt_AnalyzeLamellaBending : GH_Component
    {
        List<List<Polyline>> m_curve_list = new List<List<Polyline>>();
        List<double> m_maxk_list = new List<double>();
        List<List<List<double>>> m_k_list = new List<List<List<double>>>();
        List<List<int>> m_color_list = new List<List<int>>();

        tas.Core.Gradient m_grad;

        bool m_drawK = false;
        bool m_display_enabled = true;
        double m_maxk_found = 0.0;

        public Cmpt_AnalyzeLamellaBending()
          : base("Analyze Lamella Bending", "LamK",
              "Displays curvature limits of a glulam blank.",
              "tasLam", "Analyze")
        {

            if (m_grad == null)
            {
                List<double> stops = new List<double> { 0.0, 0.25, 0.5, 0.75, 1.0 };
                List<System.Drawing.Color> colors = new List<System.Drawing.Color>{
            System.Drawing.Color.FromArgb(0, 255, 0),
            System.Drawing.Color.FromArgb(128, 255, 0),
            System.Drawing.Color.FromArgb(255, 255, 0),
            System.Drawing.Color.FromArgb(255, 128, 0),
            System.Drawing.Color.FromArgb(255, 0, 0)};

                m_grad = new Gradient(stops, colors);
            }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam blank", "B", "Glulam blank to analyze.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radius factor", "R", "Multiplier factor for bending limit (default = 200).", GH_ParamAccess.item, 200.0);
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
                m_maxk_list = new List<double>();

                if (!DA.GetDataList("Glulam blank", RawBlanks)) return;

                m_maxk_found = 0.0;

                foreach (object obj in RawBlanks)
                {
                    if (obj is Lam.Glulam)
                        Blanks.Add(obj as Lam.Glulam);
                    else if (obj is GH.GH_Glulam)
                        Blanks.Add((obj as GH.GH_Glulam).Value);
                }

                double rad_fac = 200.0;
                DA.GetData("Radius factor", ref rad_fac);
                if (rad_fac <= 0.0) rad_fac = 0.001;

                bool thresh = false;
                DA.GetData("Threshold", ref thresh);

                m_drawK = thresh;

                //MaxK = 1 / Math.Max(1.0, min_r);

                m_curve_list = new List<List<Polyline>>();

                List<GeometryBase> geo = new List<GeometryBase>();

                m_k_list = new List<List<List<double>>>();

                foreach (Lam.Glulam blank in Blanks)
                {
                    m_curve_list.Add(new List<Polyline>());
                    List<Polyline> CurveListCurrent = m_curve_list.Last();
                    if (blank == null) continue;

                    List<Curve> lam_crvs = blank.GetLamellaCurves();

                    double min_r = (double)Math.Min(blank.Data.LamHeight, blank.Data.LamWidth) * rad_fac;
                    m_maxk_list.Add(1 / min_r);

                    //MaxK = 1 / Math.Max(1.0, (Math.Max(blank.LamellaHeight, blank.LamellaWidth) * 100));


                    foreach (Curve c in lam_crvs)
                    {
                        if (c.IsPolyline())
                        {
                            Polyline pl;
                            c.TryGetPolyline(out pl);
                            CurveListCurrent.Add(pl);
                        }
                        else
                            CurveListCurrent.Add(tas.Core.Util.CurveToPolyline(c, 0.1));
                    }
                }

                for (int i = 0; i < m_curve_list.Count; ++i)
                {
                    m_k_list.Add(new List<List<double>>());

                    List<Polyline> CurveListCurrent = m_curve_list[i];
                    for (int j = 0; j < CurveListCurrent.Count; ++j)
                    {
                        m_k_list[i].Add(new List<double>());

                        m_k_list[i][j].Add(0.0);
                        double K = 0.0;
                        for (int k = 1; k < CurveListCurrent[j].Count - 1; ++k)
                        {
                            Circle c = new Circle(CurveListCurrent[j][k - 1], CurveListCurrent[j][k], CurveListCurrent[j][k + 1]);
                            K = 1 / c.Radius;

                            //K = tas.Core.Util.CurvatureFrom3Points(CurveListCurrent[j][k - 1], CurveListCurrent[j][k], CurveListCurrent[j][k + 1]);

                            m_k_list[i][j][k - 1] = (m_k_list[i][j][k - 1] + K) / 2;
                            m_k_list[i][j].Add(K);
                            m_maxk_found = Math.Max(m_maxk_found, K);
                            //geo.Add(c.ToNurbsCurve());

                            //MaxK = Math.Max(MaxK, k);
                        }
                        m_k_list[i][j][0] = m_k_list[i][j][0] * 2;
                    }
                }

                //msg = string.Format("KList: {0}\nMaxK: {1}\nBlanks: {2}\nCurveList: {3}\n\n",
                //    KList.Count, MaxK.Count, Blanks.Count, CurveList.Count);
            }
            DA.SetData("Max curvature", m_maxk_found);
            /*
            for (int i = 0; i < MaxK.Count; ++i)
            {
                msg += string.Format("  {0}\n", MaxK[i]);
            }

            for (int i = 0; i < CurveList.Count; ++i)
            {
                msg += string.Format("  CL {0}: {1}\n", i, CurveList[i].Count);
                for (int j = 0; j < CurveList[i].Count; ++j)
                    msg += string.Format("     CLC {0}: {1}\n", j, CurveList[i][j].Count);
            }

            for (int i = 0; i < KList.Count; ++i)
            { 
                msg += string.Format("  KList {0}: {1}\n", i, KList[i].Count);
                for (int j = 0; j < KList[i].Count; ++j)
                {

                    msg += string.Format("     KList {0}: {1}\n", j, KList[i][j].Count);
                    for (int k = 0; k < KList[i][j].Count; ++k)
                    {
                        msg += string.Format("       {0}\n", KList[i][j][k]);
                    }
                }
            }

            DA.SetData("debug", msg);*/
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
            get { return new Guid("f74cbdee-c638-4979-a45e-b1bb2677ccd6"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (m_display_enabled)
            {

                for (int i = 0; i < m_curve_list.Count; ++i)
                {
                    for (int j = 0; j < m_curve_list[i].Count; ++j)
                    {
                        for (int k = 0; k < m_curve_list[i][j].Count - 1; ++k)
                        {
                            double c = Math.Min(1.0, (m_k_list[i][j][k] / m_maxk_list[i]));
                            if (m_drawK) c = m_k_list[i][j][k] > m_maxk_list[i] ? 1.0 : 0.0;

                            args.Display.DrawLine(new Line(m_curve_list[i][j][k], m_curve_list[i][j][k + 1]), m_grad.GetColor(c), 1);
                        }
                    }
                }
            }
        }
    }
}