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
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;

namespace tas.Lam.GH
{
    public class Cmpt_FindJointConditions : GH_Component
    {

        public Cmpt_FindJointConditions()
          : base("Find Joint Conditions", "FindJoints",
              "Try to find joint conditions between all GlulamWorkpiece objects in a list.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Workpieces", "WP", "Input GlulamWorkpiece objects to check.", GH_ParamAccess.list);
            pManager.AddNumberParameter("SearchDistance", "SD", "Distance to search for intersections.", GH_ParamAccess.item, 100.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Workpieces", "WP", "Output GlulamWorkpiece objects with joints.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double SearchDist = 100.0;

            List<object> obj = new List<object>();

            if (!DA.GetDataList("Workpieces", obj))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to get GlulamWorkpieces.");
                return;
            }

            DA.GetData("SearchDistance", ref SearchDist);

            List<GlulamWorkpiece> wps = new List<GlulamWorkpiece>();

            for (int i = 0; i < obj.Count; ++i)
            {
                GlulamWorkpiece wp_temp = null;
                if (obj[i] is GlulamWorkpiece)
                    wp_temp = obj[i] as GlulamWorkpiece;
                else if (obj[i] is GH_GlulamWorkpiece)
                    wp_temp = (obj[i] as GH_GlulamWorkpiece).Value;

                if (wp_temp == null) continue;

                GlulamWorkpiece wp = wp_temp.Duplicate();
                wps.Add(wp);
            }

            for (int i = 0; i < wps.Count - 1; ++i)
            {
                for (int j = i + 1; j < wps.Count; ++j)
                {
                    //GlulamWorkpiece.FindJointConditions(wps[i], wps[j], SearchDist);
                }
            }

            DA.SetDataList("Workpieces", wps.Select(x => new GH_GlulamWorkpiece(x)));
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
            get { return new Guid("{ca3dc7d0-38fb-4a19-804f-01ed7d73a267}"); }
        }
    }
}