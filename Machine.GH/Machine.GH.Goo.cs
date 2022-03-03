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
                //return new BoundingBox(Value.Paths.SelectMany(x => x.Select(y => y.Plane.Origin)));
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

    public class GH_tasPath : GH_Goo<Path>, IGH_PreviewData
    {
        public GH_tasPath() { this.Value = null; }
        public GH_tasPath(GH_tasPath goo) { this.Value = new Path(goo.Value); }
        public GH_tasPath(Path native) { this.Value = new Path(native); }
        public override IGH_Goo Duplicate() => new GH_tasPath(this);
        public override bool IsValid => true;
        public override string TypeName => "Path";
        public override string TypeDescription => "Polyline with planes";
        public override string ToString() => "Path"; //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public static IEnumerable<GH_tasPath> MakeGoo(List<Path> Paths)
        {
            return Paths.Select(x => new GH_tasPath(x));
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetInt32("Count", Value.Count);
            for (int i = 0; i < Value.Count; ++i)
            {
                writer.SetPlane("v", i,
                    new GH_IO.Types.GH_Plane(
                        Value[i].OriginX,
                        Value[i].OriginY,
                        Value[i].OriginZ,
                        Value[i].XAxis.X,
                        Value[i].XAxis.Y,
                        Value[i].XAxis.Z,
                        Value[i].YAxis.X,
                        Value[i].YAxis.Y,
                        Value[i].YAxis.Z
                    )
                    );
            }
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            List<Plane> planes = new List<Plane>();
            int N = reader.GetInt32("Count");
            for (int i = 0; i < N; ++i)
            {
                GH_IO.Types.GH_Plane gp = reader.GetPlane("v", i);
                planes.Add(
                    new Plane(
                        new Point3d(gp.Origin.x, gp.Origin.y, gp.Origin.z),
                        new Vector3d(gp.XAxis.x, gp.XAxis.y, gp.XAxis.z),
                        new Vector3d(gp.YAxis.x, gp.YAxis.y, gp.YAxis.z)
                        )
                    );
            }
            Value = new Path(planes);
            return base.Read(reader);
        }

        public override bool CastFrom(object source)
        {
            if (source is GH_tasPath)
            {
                Value = new Path((source as GH_tasPath).Value);
                return true;
            }
            else if (source is Path)
            {
                Value = new Path(source as Path);
                return true;
            }
            else if (source is GH_Curve)
            {
                Polyline poly;
                Curve c = (source as GH_Curve).Value;
                if (c.IsPolyline())
                {
                    c.TryGetPolyline(out poly);
                }
                else
                {
                    PolylineCurve polyc = (source as Curve).ToPolyline(0, 0, 0.1, 1.0, 0, 0.2, 0.1, 0, true);
                    if (!polyc.TryGetPolyline(out poly))
                        return false;
                }

                List<Plane> planes = new List<Plane>();
                foreach (Point3d p in poly)
                {
                    planes.Add(new Plane(p, Vector3d.ZAxis));
                }

                Value = new Path(planes);
                return true;
            }
            else if (source is List<GH_Plane>)
            {
                List<Plane> planes = (source as List<GH_Plane>).Select(x => x.Value).ToList();
                Value = new Path(planes);
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                object mesh = new GH_Curve(Value.PolylineCurve());

                target = (Q)mesh;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(IEnumerable<GH_Plane>)))
            {
                List<GH_Plane> cl = new List<GH_Plane>();
                foreach (Plane p in Value)
                {
                    cl.Add(new GH_Plane(p));
                }

                object o = cl;
                target = (Q)o;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        public BoundingBox ClippingBox => Value.PolylineCurve().GetBoundingBox(true);

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            args.Pipeline.DrawPolyline(new Polyline(Value.Select(x => x.Origin)), args.Material.Diffuse);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPolyline(new Polyline(Value.Select(x => x.Origin)), args.Color);
        }
    }

    public class tasPathParameter : GH_PersistentParam<GH_tasPath>
    {
        public tasPathParameter() : base("Path parameter", "Path", "This is a polyline with planes.", "tasMachine", "Path") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        //protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_oriented_polyline_component_24x24;
        public override System.Guid ComponentGuid => new Guid("{eba2ae1c-5c0c-4553-9fae-411e0905ce23}");
        protected override GH_GetterResult Prompt_Singular(ref GH_tasPath value)
        {
            value = new GH_tasPath();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_tasPath> values)
        {
            values = new List<GH_tasPath>();
            return GH_GetterResult.success;
        }
    }

}