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

namespace tas.Core.GH
{
    public class GH_PPolyline : GH_Goo<PPolyline>, IGH_PreviewData
    {
        public GH_PPolyline() { this.Value = null; }
        public GH_PPolyline(GH_PPolyline goo) { this.Value = new PPolyline(goo.Value); }
        public GH_PPolyline(PPolyline native) { this.Value = new PPolyline(native); }
        public override IGH_Goo Duplicate() => new GH_PPolyline(this);
        public override bool IsValid => true;
        public override string TypeName => "OrientedPolyline";
        public override string TypeDescription => "OrientedPolyline";
        public override string ToString() => "PPolyline"; //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public static IEnumerable<GH_PPolyline> MakeGoo(List<PPolyline> OrientedPolylines)
        {
            return OrientedPolylines.Select(x => new GH_PPolyline(x));
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
            Value = new PPolyline(planes);
            return base.Read(reader);
        }

        public override bool CastFrom(object source)
        {
            if (source is GH_PPolyline)
            {
                Value = new PPolyline((source as GH_PPolyline).Value);
                return true;
            }
            else if (source is PPolyline)
            {
                Value = new PPolyline(source as PPolyline);
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

                Value = new PPolyline(planes);
                return true;
            }
            else if (source is List<GH_Plane>)
            {
                List<Plane> planes = (source as List<GH_Plane>).Select(x => x.Value).ToList();
                Value = new PPolyline(planes);
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
            args.Pipeline.DrawPolyline((Polyline)Value, args.Material.Diffuse);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            args.Pipeline.DrawPolyline((Polyline)Value, args.Color);
        }
    }

    public class OrientedPolylineParameter : GH_PersistentParam<GH_PPolyline>
    {
        public OrientedPolylineParameter() : base("Oriented Polyline parameter", "OPolyline", "This is an oriented polyline.", "tasTools", "Parameters") { }
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_oriented_polyline_component_24x24;
        public override System.Guid ComponentGuid => new Guid("{99a9cf30-a28b-475b-8d95-79440ccf631e}");
        protected override GH_GetterResult Prompt_Singular(ref GH_PPolyline value)
        {
            value = new GH_PPolyline();
            return GH_GetterResult.success;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_PPolyline> values)
        {
            values = new List<GH_PPolyline>();
            return GH_GetterResult.success;
        }
    }

}
