using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH.Components
{
    public class Cmpt_CreatePath : GH_Component
    {
        public Cmpt_CreatePath()
          : base("Create Path", "Path",
              "Create an orientated path.",
              "tasMachine", UiNames.PathSection)
        {
        }
        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_CreatePath;
        public override Guid ComponentGuid => new Guid("8b3cbf53-f05d-4fdd-b582-913330497f72");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "List of planes to convert to path.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Plane> plane_tree = null;
            GH_Structure<GH_tasPath> path_tree = new GH_Structure<GH_tasPath>();

            DA.GetDataTree(0, out plane_tree);

            foreach (var tree_path in plane_tree.Paths)
            {
                path_tree.EnsurePath(tree_path);

                if (plane_tree[tree_path].Count > 1)
                {
                    path_tree[tree_path].Add(
                        new GH_tasPath(
                            new Path(
                                plane_tree[tree_path].Select(x => x.Value))
                            )
                        );
                }
            }

            DA.SetDataTree(0, path_tree);
        }
    }
}