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
    public class Cmpt_OptimalCrvFrames: GH_Component
    {
        public Cmpt_OptimalCrvFrames()
          : base("FindOptimalFrames", "OptFrames",
              "Find optimal frames along curve to minimize bending in one direction.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to analyze.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number", "N", "Number of frames to get.", GH_ParamAccess.item, 3);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Frames", "F", "Frames for minimal double-curvature.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;

            if (!DA.GetData("Curve", ref crv))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Curve connected.");
                return;
            }

            int N = 3;
            DA.GetData("Number", ref N);

            double[] t;

            if (N <3)
            {
                t = new double[] { crv.Domain.Min, crv.Domain.Max };
            }
            else
                t = crv.DivideByCount(N - 1, true);

            Vector3d[] k = t.Select(x => crv.CurvatureAt(x)).ToArray();
            Plane[] frames = new Plane[N];

            double angle;
            for (int i = 0; i < N; ++i)
            {
                crv.PerpendicularFrameAt(t[i], out frames[i]);
                angle = Vector3d.VectorAngle(k[i], frames[i].YAxis, frames[i]);
                frames[i].Transform(Transform.Rotation(-angle, frames[i].ZAxis, frames[i].Origin));
            }

            DA.SetDataList("Frames", frames);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
                return Properties.Resources.tasTools_icons_Delaminate_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{99654aa4-795b-43de-ab93-01b64f6addac}"); }
        }
    }
}