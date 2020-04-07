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

namespace tas.Lam.GH
{
    public class Cmpt_DeLam : GH_Component
    {
        public Cmpt_DeLam()
          : base("Delaminate", "Delam",
              "Extracts parameters from Glulam object.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input glulam blank to deconstruct.", GH_ParamAccess.item);
            pManager.AddTextParameter("Keys", "K", "Parameters to extract.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Params", "P", "Extracted parameters", GH_ParamAccess.tree);
            /*
            pManager.AddCurveParameter("Centreline", "C", "Centreline of glulam blank.", GH_ParamAccess.item);
            pManager.AddNumberParameter("LamellaWidth", "Lw", "Width of glulam lamellas.", GH_ParamAccess.item);
            pManager.AddNumberParameter("LamellaHeight", "Lh", "Height of glulam lamellas.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("NumWidth", "Nw", "Number of glulam lamellas horiztonally.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("NumHeight", "Nh", "Number of glulam lamellas vertically.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Frames", "F", "Glulam cross-section control frames.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Samples", "S", "Sampling density for glulam blank.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("IsValid", "K", "Does the glulam curvature fall within the lamella dimension limits? " +
                "Default radius multiplier is 200.0 (curvature limit is lamella width or height x 200.0).", GH_ParamAccess.item);
            */
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj = null;

            if (!DA.GetData("Glulam", ref obj))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No glulam blank connected.");
                return;
            }

            List<string> keys = new List<string>();

            if (!DA.GetDataList("Keys", keys))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No keys found.");
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

            DataTree<object> output = new DataTree<object>();
            Dictionary<string, object> props = g.GetProperties();
            
            for (int i = 0; i < keys.Count; ++i)
            {
                if (props.ContainsKey(keys[i]))
                {
                    if (props[keys[i]].GetType().IsArray)
                    {
                        Type t = props[keys[i]].GetType().GetElementType();
                        if (t == typeof(Rhino.Geometry.Plane))
                        {
                            var ar = props[keys[i]] as Rhino.Geometry.Plane[];
                            GH_Path path = new GH_Path(i);
                            for (int j = 0; j < ar.Length; ++j)
                            {
                                output.Add(ar[j], path);
                            }
                        }
                    }
                    else
                        output.Add(props[keys[i]], new GH_Path(i));
                }
            }
            
            DA.SetDataTree(0, output);
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
            get { return new Guid("{5f7285a8-9cc4-411e-b4fb-94857311692b}"); }
        }
    }
}