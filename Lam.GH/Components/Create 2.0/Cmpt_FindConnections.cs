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
using System.Linq;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace tas.Lam.GH
{
    public class Cmpt_FindConnections : GH_Component
    {
        public Cmpt_FindConnections()
          : base("Find Connections", "FindX",
              "Find possible connections between Glulam objects.",
              "tasLam", "Create")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulams", "G", "Glulam centreline curve.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Indices", "I", "Indices of connected Glulams, per Glulam.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Type", "T", "Type of connection: 0 = end-to-end joint, 1 = crossing joint, 2 = t-joint.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Parameters", "t", "Parameter on Glulam where connection occurs.", GH_ParamAccess.tree);
        }

        void FindIntersections(List<Glulam> glulams, out DataTree<int> indices, out DataTree<int> joint_types, out DataTree<double> parameters, double overlap = 50.0, double tolerance = 0.01)
        {
            indices = new DataTree<int>();
            joint_types = new DataTree<int>();
            parameters = new DataTree<double>();

            int num_intersections = 0;

            int type0, type1, joint_type;
            double t0, t1;

            for (int i = 0; i < glulams.Count - 1; ++i)
            {
                if (glulams[i] == null) continue;
                GH_Path path0 = new GH_Path(i);

                joint_type = 0;

                for (int j = i + 1; j < glulams.Count; ++j)
                {
                    if (glulams[j] == null) continue;
                    GH_Path path1 = new GH_Path(j);

                    var res = Rhino.Geometry.Intersect.Intersection.CurveCurve(
                      glulams[i].Centreline,
                      glulams[j].Centreline,
                      tolerance, tolerance);

                    if (res == null || res.Count < 1) continue;

                    foreach (var ci in res)
                    {
                        num_intersections++;

                        if (ci.OverlapA.Length > overlap && ci.OverlapB.Length > overlap)
                        {
                            type0 = 0;
                            type1 = 0;
                            t0 = ci.OverlapA.Mid;
                            t1 = ci.OverlapB.Mid;
                        }

                        else
                        {
                            type0 = EndOrMiddle(glulams[i].Centreline, ci.ParameterA);
                            type1 = EndOrMiddle(glulams[j].Centreline, ci.ParameterB);
                            t0 = ci.ParameterA;
                            t1 = ci.ParameterB;
                        }

                        if (type0 == 0 && type1 == 0)
                            joint_type = 0;
                        else if (type0 == 1 && type1 == 1)
                            joint_type = 1;
                        else
                            joint_type = 3;

                        indices.Add(j, path0);
                        parameters.Add(t0, path0);
                        joint_types.Add(joint_type, path0);

                        indices.Add(i, path1);
                        parameters.Add(t1, path1);
                        joint_types.Add(joint_type, path1);
                    }
                }
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Found {num_intersections} connections.");
        }


        int EndOrMiddle(Curve c, double t, double end_offset = 50.0)
        {
            if (!c.Domain.IncludesParameter(t)) return -1;

            double length = c.GetLength(new Interval(c.Domain.Min, t));
            double clength = c.GetLength();

            if (length < end_offset || (clength - length) < end_offset)
                return 0; // return END
            return 1; // return MIDDLE
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Glulam> glulams = new List<Glulam>();

            if (!DA.GetDataList<Glulam>("Glulams", glulams))
                return;


            FindIntersections(glulams, out DataTree<int> indices, out DataTree<int> joint_types, out DataTree<double> parameters, 50.0, 0.1);

            DA.SetDataTree(0, indices);
            DA.SetDataTree(1, joint_types);
            DA.SetDataTree(2, parameters);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_FreeformGlulam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{42fe0cb3-ea85-4fc2-84df-81bc46b4a80d}"); }
        }
    }
}