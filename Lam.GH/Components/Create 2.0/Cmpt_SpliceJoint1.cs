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
using Rhino.DocObjects;
using Rhino.Geometry;

using tas.Lam.Features;

namespace tas.Lam.GH
{
    public class Cmpt_SpliceJoint1 : GH_Component
    {
        public Cmpt_SpliceJoint1()
          : base("Create Splice Joint", "SJoint",
              "Create a scarf joint between two Glulam objects. Glulams must be overlapping.",
              "tasLam", "Create")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("GlulamA", "G", "Glulam A.", GH_ParamAccess.item);
            pManager.AddGenericParameter("GlulamB", "G", "Glulam B.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Length of the scarf joint.", GH_ParamAccess.item, 100.0);
            pManager.AddNumberParameter("Incline", "I", "Incline of the scarf joint.", GH_ParamAccess.item, 20.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Joint geometry as Brep.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Glulam gA = null, gB = null;

            if (!DA.GetData<Glulam>("GlulamA", ref gA) || !DA.GetData<Glulam>("GlulamB", ref gB))
                return;

            gA.Centreline.ClosestPoints(gB.Centreline, out Point3d pA, out Point3d pB);

            Point3d pC = (pA + pB) / 2;

            gA.Centreline.ClosestPoint(pC, out double tA);
            gB.Centreline.ClosestPoint(pC, out double tB);

            double m_length = 100.0, m_incline = 20.0;

            DA.GetData("Length", ref m_length);
            DA.GetData("Incline", ref m_incline);

            EndLapJoint2 scarf_joint = new EndLapJoint2(gA, gB, tA, tB, m_length, m_incline);
            scarf_joint.Compute();

            List<Brep> breps = scarf_joint.GetCuttingGeometry();

            if (breps.Count > 0)
                DA.SetData("Brep", breps[0]);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_StraightGlulam_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{b662cb45-c8af-4085-8df7-f7f77ce025ff}"); }
        }
    }
}