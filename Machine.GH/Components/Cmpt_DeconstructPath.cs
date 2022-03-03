using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace tas.Machine.GH
{
    public class Cmpt_DeconstructPath : GH_Component
    {
        public Cmpt_DeconstructPath()
          : base("Deconstruct Path", "DePath",
              "Convert path to planes.",
              "tasMachine", "Path")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Path", "P", "Path to deconstruct.", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Planes", "P", "List of planes.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Plane> plane_tree = null;
            GH_Structure<GH_tasPath> path_tree = null;

            DA.GetDataTree<GH_tasPath>(0, out path_tree);

            foreach (var path in path_tree.Paths)
            {
                for (int i = 0; i < path_tree[path].Count; ++i)
                {
                    var npath = path.AppendElement(i);
                    plane_tree.EnsurePath(npath);
                    plane_tree[npath].AddRange(path_tree[path][i].Value.ToList().Select(x => new GH_Plane(x)));
                }
            }

            DA.SetDataTree(0, plane_tree);
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
            get { return new Guid("36abf236-a2af-46d4-9f24-3985007469b5"); }
        }
    }
}