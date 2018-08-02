using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core.Types;

namespace tas.Lam.GH
{

    public class GlulamParameter : GH_PersistentParam<GH_Glulam>
    {
        public GlulamParameter() : base("Glulam parameter", "Glulam", "This is a glulam.", "tasTools", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasTools_icons_FreeformGlulam_24x24;
        public override System.Guid ComponentGuid => new Guid("{9ae1662a-a114-4780-9dbe-c56c32998c95}");
        protected override GH_GetterResult Prompt_Singular(ref GH_Glulam value)
        {
            value = new GH_Glulam();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_Glulam> values)
        {
            values = new List<GH_Glulam>();
            return GH_GetterResult.success;
        }

    }

    public class GlulamAssemblyParameter : GH_PersistentParam<GH_Assembly>
    {
        public GlulamAssemblyParameter() : base("Assembly parameter", "Assembly", "This is a glulam assembly.", "tasTools", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasTools_icons_FreeformGlulam_24x24;
        public override System.Guid ComponentGuid => new Guid("{de07b39c-5dca-438f-9b1a-73380442ec21}");
        protected override GH_GetterResult Prompt_Singular(ref GH_Assembly value)
        {
            value = new GH_Assembly();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_Assembly> values)
        {
            values = new List<GH_Assembly>();
            return GH_GetterResult.success;
        }
    }

    public class GlulamWorkpieceParameter : GH_PersistentParam<GH_GlulamWorkpiece>
    {
        public GlulamWorkpieceParameter() : base("GlulamWorkpiece parameter", "GlulamWorkpiece", "This is a glulam workpiece.", "tasTools", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasTools_icons_FreeformGlulam_24x24;
        public override System.Guid ComponentGuid => new Guid("{9b760327-3e87-4439-9250-bcaf283ff5c4}");
        protected override GH_GetterResult Prompt_Singular(ref GH_GlulamWorkpiece value)
        {
            value = new GH_GlulamWorkpiece();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_GlulamWorkpiece> values)
        {
            values = new List<GH_GlulamWorkpiece>();
            return GH_GetterResult.success;
        }
    }
}