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
    public class Cmpt_GetEdgePoints : GH_Component
    {
        public Cmpt_GetEdgePoints()
          : base("Get Edge Points", "GetEdges",
              "Gets list of points for each Glulam section edge.",
              "tasTools", "Glulam")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input glulam blank to deconstruct.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Samples", "S", "Optional number of samples to use instead of Glulam data. " + 
                "Must be >= 2.", GH_ParamAccess.item, -1);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("TopRight", "TR", "Top right points (+Y, +X).", GH_ParamAccess.list);
            pManager.AddPointParameter("TopLeft", "TL", "Top left points (+Y, -X).", GH_ParamAccess.list);
            pManager.AddPointParameter("BottomRight", "BR", "Bottom right points (-Y, +X).", GH_ParamAccess.list);
            pManager.AddPointParameter("BottomLeft", "BL", "Bottom left points (-Y, -X).", GH_ParamAccess.list);
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

            int N = -1;

            DA.GetData("Number", ref N);

            if (N < 2) N = g.Data.Samples;

            double[] tt = g.Centreline.DivideByCount(N, true);

            Plane[] planes = tt.Select(x => g.GetPlane(x)).ToArray();

            double w = g.Data.LamWidth * g.Data.NumWidth;
            double h = g.Data.LamHeight * g.Data.NumHeight;
            double hw = w / 2;
            double hh = h / 2;

            Point3d[] ptsTR = planes.Select(x => x.PointAt(hw, hh)).ToArray();
            Point3d[] ptsTL = planes.Select(x => x.PointAt(-hw, hh)).ToArray();
            Point3d[] ptsBR = planes.Select(x => x.PointAt(hw, -hh)).ToArray();
            Point3d[] ptsBL = planes.Select(x => x.PointAt(-hw, -hh)).ToArray();

            DA.SetDataList("TopRight", ptsTR);
            DA.SetDataList("TopLeft", ptsTL);
            DA.SetDataList("BottomRight", ptsBR);
            DA.SetDataList("BottomLeft", ptsBL);
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
            get { return new Guid("{06db3a40-97a4-4368-a4ae-b4984712950a}"); }
        }
    }
}