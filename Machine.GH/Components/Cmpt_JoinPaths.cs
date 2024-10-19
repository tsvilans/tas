using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH.Components
{
    public class Cmpt_JoinPaths : GH_Component
    {
        public Cmpt_JoinPaths()
          : base("Join Paths", "Path",
              "Join orientated paths together.",
              "tasMachine", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "P", "Paths to join.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_tasPath> path_tree = new GH_Structure<GH_tasPath>();

            DA.GetDataTree<IGH_Goo>(0, out GH_Structure<IGH_Goo> input_tree);

            foreach (var tree_path in input_tree.Paths)
            {
                path_tree.EnsurePath(tree_path);
                Path path = new Path();

                foreach (var goo in input_tree[tree_path])
                {
                    if (goo is GH_tasPath input_path)
                    {
                        path.Join(input_path.Value);
                    }
                }

                path_tree[tree_path].Add(new GH_tasPath(path));

            }

            /*
                foreach (var ipath in inputPaths)
                {
                    if (ipath is Path)
                    {
                        path.AddRange(ipath as Path);
                    }
                    else if (ipath is GH_tasPath)
                    {
                        path.AddRange((ipath as GH_tasPath).Value);
                    }
                }

                DA.SetData(0, new GH_tasPath(path));
            */

            DA.SetDataTree(0, path_tree);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CreateToolpath_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("65e38a47-8b71-4c15-bf1d-89935c870e73"); }
        }
    }
}