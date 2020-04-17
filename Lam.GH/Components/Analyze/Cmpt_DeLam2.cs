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
    public class Cmpt_DeLaminate : GH_Component
    {
        public Cmpt_DeLaminate()
          : base("Delaminate 2.0", "Delam 2.0",
              "Gets individual lamellas from Glulam.",
              "tasLam", "Analyze")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Glulam", "G", "Input glulam blank to deconstruct.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Type", "T", "Type of output: 0 = centreline curves, 1 = Mesh, 2 = Brep.", GH_ParamAccess.item, 0);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Lamellae", "L", "Lamellae objects from Glulam.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Species", "S", "Lamellae species.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Reference", "R", "Lamellae IDs.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            object obj = null;

            if (!DA.GetData("Glulam", ref obj))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No glulam blank connected.");
                return;
            }

            int type = 0;
            DA.GetData("Type", ref type);

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
            DataTree<string> species = new DataTree<string>();
            DataTree<Guid> ids = new DataTree<Guid>();

            int i = 0;
            GH_Path path;
            switch (type)
            {
                case (1):
                    var meshes = g.GetLamellaMeshes();

                    for (int x = 0; x < g.Data.NumWidth; ++x)
                    {
                        path = new GH_Path(x);
                        for (int y = 0; y < g.Data.NumHeight; ++y)
                        {
                            output.Add(meshes[i], path);
                            if (g.Data.Lamellae[x, y] != null)
                            {
                                species.Add(g.Data.Lamellae[x, y].Species, path);
                                ids.Add(g.Data.Lamellae[x, y].Reference, path);
                            }
                            i++;
                        }
                    }
                    break;
                case (2):
                    var breps = g.GetLamellaBreps();

                    for (int x = 0; x < g.Data.NumWidth; ++x)
                    {
                        path = new GH_Path(x);
                        for (int y = 0; y < g.Data.NumHeight; ++y)
                        {
                            output.Add(breps[i], path);
                            if (g.Data.Lamellae[x, y] != null)
                            {
                                species.Add(g.Data.Lamellae[x, y].Species, path);
                                ids.Add(g.Data.Lamellae[x, y].Reference, path);
                            }
                            i++;
                        }
                    }
                    break;
                default:
                    var crvs = g.GetLamellaCurves();

                    for (int x = 0; x < g.Data.NumWidth; ++x)
                    {
                        path = new GH_Path(x);
                        for (int y = 0; y < g.Data.NumHeight; ++y)
                        {
                            output.Add(crvs[i], path);
                            if (g.Data.Lamellae[x, y] != null)
                            {
                                species.Add(g.Data.Lamellae[x, y].Species, path);
                                ids.Add(g.Data.Lamellae[x, y].Reference, path);
                            }
                            i++;
                        }
                    }
                    break;
            }

            DA.SetDataTree(0, output);
            DA.SetDataTree(1, species);
            DA.SetDataTree(2, ids);
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
            get { return new Guid("{6ff945b5-b4c7-4a71-9ab2-f3e7eea899ed}"); }
        }
    }
}