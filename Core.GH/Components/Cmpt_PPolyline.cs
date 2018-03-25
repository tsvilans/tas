using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using tas.Core.Types;


namespace tas.Core.GH
{
    public class PPolyline_Component : GH_Component
    {
        public PPolyline_Component()
          : base("PPolyline", "PPoly",
              "PlanePolyline, a polyline with planes as vertices.",
              "tasTools", "Test")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "Can be a list of Planes, a Curve / Polyline, or a list of Points.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Orientation", "Ori", "Optional plane that will orient the input points if no planes are found.", GH_ParamAccess.list);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PPolyline", "PPoly", "Polyline with planes as vertices.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> obj_refs = new List<object>();
            List<Plane> planes = new List<Plane>();
            List<Plane> orientations = new List<Plane>();

            bool single = true;

            if (!DA.GetDataList(0, obj_refs)) return;
            if (!DA.GetDataList(1, orientations)) orientations = new List<Plane> { Plane.WorldXY };

            if (orientations.Count >= obj_refs.Count) single = false;

            List<GH_PPolyline> gh_op = new List<GH_PPolyline>();

            for (int i = 0; i < obj_refs.Count; ++i)
            {
                if (obj_refs[i] is GH_Plane)
                    planes.Add((obj_refs[i] as GH_Plane).Value);
                else if (obj_refs[i] is GH_Point)
                {
                    if (single)
                        planes.Add(new Plane((obj_refs[i] as GH_Point).Value, orientations[0].XAxis, orientations[0].YAxis));
                    else
                        planes.Add(new Plane((obj_refs[i] as GH_Point).Value, orientations[i].XAxis, orientations[i].YAxis));
                }
                else if (obj_refs[i] is GH_Curve)
                {
                    Curve c = (obj_refs[i] as GH_Curve).Value;
                    if (c.IsPolyline())
                    {
                        Polyline p;
                        c.TryGetPolyline(out p);
                        PPolyline op = new PPolyline(p, orientations[0]);
                        gh_op.Add(new GH_PPolyline(op));
                    }
                    else
                    {
                        var pc = c.ToPolyline(0, 0, 0.1, 1.0, 0, 0, 1.0, 0, true);
                        Polyline p;
                        pc.TryGetPolyline(out p);
                        PPolyline op = new PPolyline(p, orientations[0]);
                        gh_op.Add(new GH_PPolyline(op));
                    }
                }
            }

            if (planes.Count > 0)
                gh_op.Add(new GH_PPolyline(new Types.PPolyline(planes)));

            if (gh_op.Count > 0)
                DA.SetDataList(0, gh_op);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.icon_oriented_polyline_component_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{160669f3-96dd-4247-bcb9-fca30391f508}"); }
        }
    }
}
