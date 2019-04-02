using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using tas.Core.Types;
/*
namespace GooIoTest
{
    /// <summary>
    /// A simple immutable data type with no functionality.
    /// </summary>
    public sealed class Foo
    {
        public Foo(int integer, string text)
        {
            Integer = integer;
            Text = text;
        }

        public int Integer { get; }
        public string Text { get; }

        public override string ToString()
        {
            return string.Format("[{0}] \"{1}\"", Integer, Text);
        }
    }

    /// <summary>
    /// An IGH_Goo wrapper around Foo.
    /// </summary>
    public sealed class FooGoo : GH_Goo<Foo>
    {
        #region constructors
        public FooGoo()
         : this(null)
        { }
        public FooGoo(Foo foo)
        {
            Value = foo;
        }

        public override IGH_Goo Duplicate()
        {
            // It's okay to share the same Foo instance since Foo is immutable.
            return new FooGoo(Value);
        }
        #endregion

        #region properties
        public override string ToString()
        {
            if (Value == null) return "No foo";
            return Value.ToString();
        }

        public override string TypeName => "Foo";
        public override string TypeDescription => "Pointless foo data";
        public override bool IsValid
        {
            get
            {
                if (Value == null) return false;
                if (Value.Integer < 0) return false;
                if (Value.Text == null) return false;
                return true;
            }
        }
        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) return "No data";
                if (Value.Integer < 0) return "Negative integer data";
                if (Value.Text == null) return "No text data";
                return string.Empty;
            }
        }

        public override bool CastFrom(object source)
        {
            if (source == null) return false;
            if (source is int integer)
            {
                Value = new Foo(integer, string.Empty);
                return true;
            }
            if (source is GH_Integer ghInteger)
            {
                Value = new Foo(ghInteger.Value, string.Empty);
                return true;
            }
            if (source is string text)
            {
                Value = new Foo(0, text);
                return true;
            }
            if (source is GH_String ghText)
            {
                Value = new Foo(0, ghText.Value);
                return true;
            }
            return false;
        }
        public override bool CastTo<TQ>(ref TQ target)
        {
            if (Value == null)
                return false;

            if (typeof(TQ) == typeof(int))
            {
                target = (TQ)(object)Value.Integer;
                return true;
            }
            if (typeof(TQ) == typeof(GH_Integer))
            {
                target = (TQ)(object)new GH_Integer(Value.Integer);
                return true;
            }

            if (typeof(TQ) == typeof(double))
            {
                target = (TQ)(object)Value.Integer;
                return true;
            }
            if (typeof(TQ) == typeof(GH_Number))
            {
                target = (TQ)(object)new GH_Number(Value.Integer);
                return true;
            }

            if (typeof(TQ) == typeof(string))
            {
                target = (TQ)(object)Value.Text;
                return true;
            }
            if (typeof(TQ) == typeof(GH_String))
            {
                target = (TQ)(object)new GH_String(Value.Text);
                return true;
            }

            return false;
        }
        #endregion

        #region (de)serialisation
        private const string IoIntegerKey = "Integer";
        private const string IoTextKey = "Text";
        public override bool Write(GH_IWriter writer)
        {
            if (Value != null)
            {
                writer.SetInt32(IoIntegerKey, Value.Integer);
                if (Value.Text != null)
                    writer.SetString(IoTextKey, Value.Text);
            }
            return true;
        }
        public override bool Read(GH_IReader reader)
        {
            if (!reader.ItemExists(IoIntegerKey))
            {
                Value = null;
                return true;
            }

            int integer = reader.GetInt32(IoIntegerKey);
            string text = null;

            if (reader.ItemExists(IoTextKey))
                text = reader.GetString(IoTextKey);

            Value = new Foo(integer, text);

            return true;
        }
        #endregion
    }

    public sealed class FooParameter : GH_PersistentParam<FooGoo>
    {
        public FooParameter()
        : this("Foo", "Foo", "A collection of Foo data", "tasTools", "ShitBalls")
        { }
        public FooParameter(GH_InstanceDescription tag) : base(tag) { }
        public FooParameter(string name, string nickname, string description, string category, string subcategory)
          : base(name, nickname, description, category, subcategory) { }

        public override Guid ComponentGuid => new Guid("{606C9679-C36C-48B1-A547-22B68EE8A0A1}");
        protected override GH_GetterResult Prompt_Singular(ref FooGoo value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<FooGoo> values)
        {
            return GH_GetterResult.cancel;
        }
    }
}
*/
namespace tas.Lam.GH
{

    public class GlulamParameter : GH_PersistentParam<GH_Glulam>
    {
        public GlulamParameter() : this("Glulam parameter", "Glulam", "This is a glulam.", "tasLam", "Parameters") { }
        public GlulamParameter(string name, string nickname, string description, string category, string subcategory) 
            : base(name, nickname, description, category, subcategory) { }
        public GlulamParameter(GH_InstanceDescription tag) : base(tag) { }

        //public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override System.Guid ComponentGuid => new Guid("{9ae1662a-a114-4780-9dbe-c56c32998c95}");
        protected override GH_GetterResult Prompt_Singular(ref GH_Glulam value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_Glulam> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override Bitmap Icon => Properties.Resources.tasTools_icons_FreeformGlulam_24x24;

    }

    public class GlulamDataParamater : GH_PersistentParam<GH_GlulamData>
    {
        public GlulamDataParamater() : this("GlulamData parameter", "GlulamData", "This is a glulam.", "tasTools", "Parameters") { }
        public GlulamDataParamater(string name, string nickname, string description, string category, string subcategory)
            : base(name, nickname, description, category, subcategory) { }
        public GlulamDataParamater(GH_InstanceDescription tag) : base(tag) { }

        //public override GH_Exposure Exposure => GH_Exposure.secondary;
        //protected override System.Drawing.Bitmap Icon => Properties.Resources.tasTools_icons_FreeformGlulam_24x24;

        public override System.Guid ComponentGuid => new Guid("{e05cc9e7-6f2e-4341-80f2-af6921699c9b}");
        protected override GH_GetterResult Prompt_Singular(ref GH_GlulamData value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_GlulamData> values)
        {
            return GH_GetterResult.cancel;
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