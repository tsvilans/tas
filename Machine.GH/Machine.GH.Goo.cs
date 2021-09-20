/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2018 Tom Svilans
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
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Drawing;
using GH_IO.Serialization;

namespace tas.Machine.GH
{
    public class GH_MachineTool : GH_Goo<MachineTool>
    {
        public GH_MachineTool() { this.Value = null; }
        public GH_MachineTool(GH_MachineTool goo) { this.Value = goo.Value; }
        public GH_MachineTool(MachineTool native) { this.Value = native; }
        public override IGH_Goo Duplicate() => new GH_MachineTool(this);
        public override bool IsValid => true;
        public override string TypeName => "MachineToolGoo";
        public override string TypeDescription => "MachineToolGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is MachineTool)
            {
                Value = source as MachineTool;
                return true;
            }
            else if (source is GH_MachineTool)
            {
                Value = (source as GH_MachineTool).Value;
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(MachineTool)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }


        public override bool Write(GH_IWriter writer)
        {
            //byte[] centrelineBytes = GH_Convert.CommonObjectToByteArray(Value.Centreline);
            //writer.SetByteArray("centreline", 0, centrelineBytes);

            writer.SetString("name", Value.Name);
            writer.SetInt32("shape", (int)Value.Shape);
            writer.SetInt32("maxfeed", Value.FeedRate);
            writer.SetInt32("maxspeed", Value.SpindleSpeed);
            writer.SetInt32("plunge", Value.PlungeRate);
            writer.SetDouble("diameter", Value.Diameter);
            writer.SetDouble("length", Value.Length);
            writer.SetInt32("number", Value.Number);
            writer.SetInt32("offset", Value.OffsetNumber);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            MachineTool mt = new MachineTool();
            mt.Name = reader.GetString("name");
            mt.FeedRate = reader.GetInt32("maxfeed");
            mt.SpindleSpeed = reader.GetInt32("maxspeed");
            mt.PlungeRate = reader.GetInt32("plunge");
            mt.Diameter = reader.GetDouble("diameter");
            mt.Length = reader.GetDouble("length");
            mt.Number = reader.GetInt32("number");
            mt.OffsetNumber = reader.GetInt32("offset");
            int shape = 0;
            if (reader.TryGetInt32("shape", ref shape))
                mt.Shape = (ToolShape)shape;
            return base.Read(reader);
        }
    }

    public class GH_Toolpath : GH_Goo<Toolpath>, IGH_PreviewData
    {
        public GH_Toolpath() { this.Value = null; }
        public GH_Toolpath(GH_Toolpath goo) { this.Value = new Toolpath(goo.Value); }
        public GH_Toolpath(Toolpath native) { this.Value = new Toolpath(native); }
        public override IGH_Goo Duplicate() => new GH_Toolpath(this);
        public override bool IsValid => true;
        public override string TypeName => "ToolpathGoo";
        public override string TypeDescription => "ToolpathGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is Toolpath)
            {
                Value = new Toolpath(source as Toolpath);
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Toolpath)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return new BoundingBox(Value.Paths.SelectMany(x => x.Select(y => y.Plane.Origin)));
            }
        }

    public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            //for (int i = 0; i < Value.Paths.Count; ++i)
            //    args.Pipeline.DrawPolyline(Value.Paths[i].Select(x => x.Plane.Origin), Color.Black);
        }

        public override bool Write(GH_IWriter writer)
        {
            //byte[] centrelineBytes = GH_Convert.CommonObjectToByteArray(Value.Centreline);
            //writer.SetByteArray("centreline", 0, centrelineBytes);
            return base.Write(writer);
        }
    }

    public class GH_Feature : GH_Goo<Feature>
    {
        public GH_Feature() { this.Value = null; }
        public GH_Feature(GH_Feature goo) { this.Value = goo.Value.Duplicate(); }
        public GH_Feature(Feature native) { this.Value = native.Duplicate(); }
        public override IGH_Goo Duplicate() => new GH_Feature(this);
        public override bool IsValid => true;
        public override string TypeName => "FeatureGoo";
        public override string TypeDescription => "FeatureGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is Feature)
            {
                Value = (source as Feature).Duplicate();
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Feature)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }
            else if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
            {
                object blank = new GH_Brep(Value.ToBrep());

                target = (Q)blank;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                object cl = new GH_Curve(Value.ToNurbsCurve());
                target = (Q)cl;
                return true;
            }
            return base.CastTo<Q>(ref target);
        }
    }
}