/*
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
              "tasTools", "Glulam")
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
            object obj = null;

            if (!DA.GetData("Glulam", ref obj))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No glulam blank connected.");
                return;
            }

            Glulam g;

            if (obj is GH_Glulam)
                g = (obj as GH_Glulam).Value;
            else
                g = obj as Glulam;

            if (g == null)
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
            List<Glulam> res = new List<Glulam>();
            Glulam m_temp = g;
            for (int i = 0; i < m_params.Count; ++i)
            {
                if (m_params[i] <= g.Centreline.Domain.Min || m_params[i] >= g.Centreline.Domain.Max)
                    continue;
                res = m_temp.Split(m_params[i], m_overlap);
                if (res.Count == 2)
                {
                    m_glulams.Add(res[0]);
                    m_temp = res[1];
                }
            }
            if (res.Count > 0)
                m_glulams.Add(res.Last());

            DA.SetDataList("Glulams", m_glulams.Select(x => new GH_Glulam(x)));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tasTools_icons_Delaminate_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{75b8a637-42a4-4d21-9192-d8787db88f41}"); }
        }
    }
}