﻿/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
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

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Lam.GH
{
    public class Cmpt_EstimateK : GH_Component
    {
        public Cmpt_EstimateK()
          : base("Estimate Curvature", "GetK",
              "Estimate the maximum curvature for the width and height of a glulam cross-section, using the RMF of the input curve.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Curve", "C", "Glulam to offset.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Num samples", "N", "Number of times to sample the curve. Higher is more accurate.", GH_ParamAccess.item, 100);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("MaxK X", "kX", "The maximum curvature in the RMF's X-axis.", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaxK Y", "kY", "The maximum curvature in the RMF's Y-axis.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve m_curve = null;
            if (!DA.GetData("Curve", ref m_curve) || m_curve == null)
            {
                return;
            }

            int N = 100;

            DA.GetData("Num samples", ref N);
            N = Math.Max(2, N);

            double[] t = m_curve.DivideByCount(N, false);

            Plane RMF;
            Vector3d vK;

            double kx = 0, ky = 0;

            for (int i = 0; i < t.Length; ++i)
            {
                m_curve.PerpendicularFrameAt(t[i], out RMF);
                vK = m_curve.CurvatureAt(t[i]);

                kx = Math.Max(kx, Math.Abs(vK * RMF.XAxis));
                ky = Math.Max(ky, Math.Abs(vK * RMF.YAxis));
            }

            DA.SetData("MaxK X", kx);
            DA.SetData("MaxK Y", ky);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CurvatureAnalysis_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("2507c79e-0bce-462a-afcb-268204ee3053"); }
        }
    }
}