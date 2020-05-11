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
using System.Collections.Generic;

using Rhino.Geometry;

using Grasshopper.Kernel;
using Grasshopper;
using Grasshopper.Kernel.Data;
using System.Linq;

namespace tas.Lam.GH
{
    public class Cmpt_SplitGlulam : GH_Component
    {
        public Cmpt_SplitGlulam()
          : base("Split Glulam", "SplitG",
              "Splits Glulam with optional overlap.",
              "tasLam", "Modify")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input glulam blank to deconstruct.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "T", "Point on Glulam at which to split.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Overlap", "O", "Amount of overlap at split point.", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulams", "G", "Glulam pieces.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Get Glulam
            Glulam m_glulam = null;
            DA.GetData<Glulam>("Glulam", ref m_glulam);

            if (m_glulam == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid glulam input.");
                return;
            }

            List<double> m_params = new List<double>();
            DA.GetDataList("Parameter", m_params);
            if (m_params.Count < 1) return;

            double m_overlap = 0;

            DA.GetData("Overlap", ref m_overlap);

            m_params.Sort();

            List<Glulam> m_glulams = new List<Glulam>();

            //m_glulams = g.Split(m_params.ToArray(), m_overlap);

            List<Interval> domains = new List<Interval>();
            double dmin = m_glulam.Centreline.Domain.Min;

            domains.Add(new Interval(dmin, m_params.First()));
            for (int i = 0; i < m_params.Count - 1; ++i)
            {
                domains.Add(new Interval(m_params[i], m_params[i + 1]));
            }
            domains.Add(new Interval(m_params.Last(), m_glulam.Centreline.Domain.Max));

            domains = domains.Where(x => m_glulam.Centreline.GetLength(x) > m_overlap).ToList();

            for (int i = 0; i < domains.Count; ++i)
            {
                Glulam temp = m_glulam.Trim(domains[i], m_overlap);
                if (temp == null)
                    continue;

                m_glulams.Add(temp);
            }

            DA.SetDataList("Glulams", m_glulams.Select(x => new GH_Glulam(x)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_Delaminate_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{75b8a637-42a4-4d21-9192-d8787db88f41}"); }
        }
    }
}