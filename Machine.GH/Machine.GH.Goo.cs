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
using System.IO;

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

    public class GH_Toolpath : GH_GeometricGoo<Toolpath>, IGH_PreviewData
    {

        public GH_Toolpath() 
        { 
            Value = null; 
        }

        public GH_Toolpath(GH_Toolpath goo) 
        { 
            Value = new Toolpath(goo.Value);
            CreatePreviewData(goo.Value);
        }

        public GH_Toolpath(Toolpath native) 
        { 
            Value = new Toolpath(native);
            CreatePreviewData(native);
        }

        private void CreatePreviewData(Toolpath toolpath)
        {
            var rapidLines = new List<Line>();
            var feedLines = new List<Line>();
            var plungeLines = new List<Line>();
            var unknownLines = new List<Line>();

            for (int i = 0; i < toolpath.Paths.Count; ++i)
            {
                for (int j = 1; j < toolpath.Paths[i].Count; ++j)
                {
                    var wp = toolpath.Paths[i][j];
                    var line = new Line(
                        toolpath.Paths[i][j-1].Plane.Origin,
                        wp.Plane.Origin);

                    if (wp.IsRapid()) rapidLines.Add(line);
                    else if (wp.IsFeed()) feedLines.Add(line);
                    else if (wp.IsPlunge()) plungeLines.Add(line);
                    else unknownLines.Add(line);
                }
            }

            PreviewFeeds = feedLines.ToArray();
            PreviewRapids = rapidLines.ToArray();
            PreviewPlunges = plungeLines.ToArray();
            PreviewUnknown = unknownLines.ToArray();
        }

        Line[] PreviewRapids = null, PreviewFeeds = null, PreviewPlunges = null, PreviewUnknown = null;

        public override IGH_Goo Duplicate() => new GH_Toolpath(this);
        public override bool IsValid => true;
        public override string TypeName => "ToolpathGoo";
        public override string TypeDescription => "ToolpathGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is Toolpath toolpath)
            {
                Value = new Toolpath(toolpath);
                return true;
            }

            if (source is GH_Toolpath gh_toolpath)
            {
                Value = new Toolpath(gh_toolpath.Value);
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Toolpath)))
            {
                object toolpath = Value;
                target = (Q)toolpath;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                var points = new List<Point3d>();
                for (int i = 0; i < Value.Paths.Count; ++i)
                {
                    points.AddRange(Value.Paths[i].Select(x => x.Plane.Origin));
                }
                object curve = new GH_Curve(new Polyline(points).ToNurbsCurve());

                target = (Q)curve;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(IEnumerable<GH_Plane>)))
            {
                var planes = new List<GH_Plane>();
                for (int i = 0; i < Value.Paths.Count; ++i)
                {
                    for (int j = 0; j < Value.Paths[i].Count; ++j)
                    {
                        planes.AddRange(Value.Paths[i].Select(x => new GH_Plane(x.Plane)));
                    }
                }

                object o = planes;
                target = (Q)o;

                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return Boundingbox;
            }
        }

        public override BoundingBox Boundingbox => new BoundingBox(Value.Paths.SelectMany(x => x.Select(y => y.Plane.Origin)));

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (PreviewFeeds != null) { args.Pipeline.DrawLines(PreviewFeeds, Color.LightBlue); }
            if (PreviewRapids != null) { args.Pipeline.DrawLines(PreviewRapids, Color.Red); }
            if (PreviewPlunges != null) { args.Pipeline.DrawLines(PreviewPlunges, Color.LimeGreen); }
            if (PreviewUnknown != null) { args.Pipeline.DrawLines(PreviewUnknown, Color.Purple); }
        }

        public override bool Write(GH_IWriter writer)
        {
            //byte[] centrelineBytes = GH_Convert.CommonObjectToByteArray(Value.Centreline);
            //writer.SetByteArray("centreline", 0, centrelineBytes);
            return base.Write(writer);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_Toolpath(Value.Duplicate());
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var bb = ClippingBox;
            bb.Transform(xform);
            return bb;
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            var transformed = Value.Duplicate();
            transformed.Transform(xform);

            return new GH_Toolpath(transformed);
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            throw new NotImplementedException();
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

    public class GH_tasPath : GH_GeometricGoo<Path>, IGH_PreviewData
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

        public static IEnumerable<GH_tasPath> MakeGoo(IEnumerable<Path> Paths) => Paths.Select(x => new GH_tasPath(x));

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
                var planes = new List<GH_Plane>();
                foreach (Plane p in Value)
                {
                    planes.Add(new GH_Plane(p));
                }

                object o = planes;
                target = (Q)o;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        public BoundingBox ClippingBox => Value.PolylineCurve().GetBoundingBox(true);

        public override BoundingBox Boundingbox => ClippingBox;

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            args.Pipeline.DrawPolyline(new Polyline(Value.Select(x => x.Origin)), args.Material.Diffuse);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPolyline(new Polyline(Value.Select(x => x.Origin)), args.Color);
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_tasPath(Value.Duplicate());
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            var bb = ClippingBox;
            bb.Transform(xform);
            return bb;
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            var path = Value.Duplicate();
            path.Transform(xform);

            return new GH_tasPath(path);
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            throw new NotImplementedException();
        }
    }

    public class ToolpathParameter : GH_PersistentParam<GH_Toolpath>, IGH_PreviewObject
    {
        public ToolpathParameter() : base("Toolpath parameter", "Toolpath", "This is a toolpath.", "tasMachine", UiNames.ToolpathSection) { }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_ToolpathParameter;
        public override System.Guid ComponentGuid => new Guid("DA2E3425-9FD4-43FD-8BD8-A0619EEC16CD");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private BoundingBox BoundingBox = BoundingBox.Unset;

        public bool Hidden { get; set; }

        public bool IsPreviewCapable => VolatileDataCount > 0;

        public BoundingBox ClippingBox => BoundingBox;

        protected override GH_GetterResult Prompt_Singular(ref GH_Toolpath value)
        {
            value = new GH_Toolpath();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_Toolpath> values)
        {
            values = new List<GH_Toolpath>();
            return GH_GetterResult.success;
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);
        }

        protected override void OnVolatileDataCollected()
        {
            BoundingBox = BoundingBox.Empty;
            foreach (GH_Toolpath toolpath in VolatileData.AllData(true))
            {
                BoundingBox.Union(toolpath.Boundingbox);
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }
    }

    public class PathParameter : GH_PersistentParam<GH_tasPath>, IGH_PreviewObject
    {
        public PathParameter() : base("Path parameter", "Path", "This is a polyline with planes.", "tasMachine", UiNames.PathSection) { }
        //protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_oriented_polyline_component_24x24;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_PathParameter;
        public override System.Guid ComponentGuid => new Guid("{eba2ae1c-5c0c-4553-9fae-411e0905ce23}");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private BoundingBox BoundingBox = BoundingBox.Unset;

        public bool Hidden { get; set ; }

        public bool IsPreviewCapable => VolatileDataCount > 0;

        public BoundingBox ClippingBox => BoundingBox;

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

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            Preview_DrawWires(args);                
        }

        protected override void OnVolatileDataCollected()
        {
            BoundingBox = BoundingBox.Empty;
            foreach (GH_tasPath path in VolatileData.AllData(true))
            {
                foreach(var plane in path.Value)
                {
                    BoundingBox.Union(plane.Origin);
                }
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
        }
    }

}