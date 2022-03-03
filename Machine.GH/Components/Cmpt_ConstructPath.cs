using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH
{
    public class Cmpt_CreatePath : GH_Component
    {
        public Cmpt_CreatePath()
          : base("Create Path", "Path",
              "Create an orientated path.",
              "tasMachine", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "List of planes to convert to path.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Plane> plane_tree = null;
            GH_Structure<GH_tasPath> path_tree = null;

            DA.GetDataTree(0, out plane_tree);

            foreach (var path in plane_tree.Paths)
            {
                path_tree.EnsurePath(path);

                if (plane_tree[path].Count > 1)
                {
                    path_tree[path].Add(
                        new GH_tasPath(
                            new Path(
                                plane_tree[path].Select(x => x.Value))
                            )
                        );
                }
            }

            DA.SetDataTree(0, path_tree);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CreateToolpath_24x24;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8b3cbf53-f05d-4fdd-b582-913330497f72"); }
        }
    }
}