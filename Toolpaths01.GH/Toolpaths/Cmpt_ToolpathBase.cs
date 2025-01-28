using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Windows.Forms;

using tas.Core.GH;
using tas.Machine.Toolpaths;

using GH_IO.Serialization;

namespace tas.Machine.GH.Components
{
    public abstract class ToolpathBase_Component : GH_Component
    {
        protected MachineTool Tool = new MachineTool();
        protected Plane Workplane = Plane.WorldXY;

        public ToolpathBase_Component(string name, string nickname, string description, string category, string subcategory)
          : base(name, nickname, description, category, subcategory)
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane for this machining strategy.", GH_ParamAccess.item, Plane.WorldXY);
            int res = pManager.AddGenericParameter("MachineTool", "MT", "Machine tool to use for this strategy.", GH_ParamAccess.item);
            pManager[res].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Paths", "P", "Paths describing the machining strategy.", GH_ParamAccess.list);
        }
    }
}