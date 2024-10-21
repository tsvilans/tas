using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH.Components
{
    public class Cmpt_DeconstructPath : GH_Component
    {
        public Cmpt_DeconstructPath()
          : base("Deconstruct Path", "DePath",
              "Convert path to planes.",
              "tasMachine", UiNames.PathSection)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Paths to deconstruct.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "Tree of planes.", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Plane> plane_tree = new GH_Structure<GH_Plane>();

            DA.GetDataTree<IGH_Goo>(0, out GH_Structure<IGH_Goo> input_tree);

            foreach (var tree_path in input_tree.Paths)
            {
                for (int i = 0; i < input_tree[tree_path].Count; ++i)
                {
                    var path = input_tree[tree_path][i] as GH_tasPath;
                    if (path == null) continue;

                    var npath = tree_path.AppendElement(i);
                    plane_tree.EnsurePath(npath);
                    plane_tree[npath].AddRange(path.Value.ToList().Select(x => new GH_Plane(x)));
                }
            }

            DA.SetDataTree(0, plane_tree);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tas_icons_CreateToolpath_24x24;

        public override Guid ComponentGuid => new Guid("36abf236-a2af-46d4-9f24-3985007469b5"); 
    }
}