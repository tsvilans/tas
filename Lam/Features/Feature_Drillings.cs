using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
namespace tas.Lam.Features
{
    /// <summary>
    /// Single-sided counter-sunk drilling feature. The workplane is on the surface of the workpiece.
    /// The drill geometry points down along the Z-axis.
    /// </summary>
    public class CounterSunkDrill : FeatureX
    {
        double m_cs_radius = 8.0;
        double m_cs_depth = 4.0;
        double m_drill_radius = 6.0;
        double m_drill_depth = 100.0;

        Plane m_workplane;

        public CounterSunkDrill(Plane plane, double drill_radius, double drill_depth, double countersink_radius, double countersink_depth)
        {
            m_workplane = plane;
            m_cs_radius = countersink_radius;
            m_cs_depth = countersink_depth;
            m_drill_radius = drill_radius;
            m_drill_depth = drill_depth;

        }

        public override bool Compute()
        {
            Vector3d z = m_workplane.ZAxis;
            double clearance = 10.0;

            Plane wp = m_workplane;

            m_result = new List<Brep>();

            Circle[] lofts = new Circle[4];
            wp.Origin = m_workplane.Origin + z * clearance;
            lofts[0] = new Circle(wp, m_cs_radius);

            wp.Origin = m_workplane.Origin - z * m_cs_depth;
            lofts[1] = new Circle(wp, m_cs_radius);

            wp.Origin = m_workplane.Origin - z * m_cs_depth;
            lofts[2] = new Circle(wp, m_drill_radius);

            wp.Origin = m_workplane.Origin - z * m_drill_depth;
            lofts[3] = new Circle(wp, m_drill_radius);

            Brep[] res = Brep.CreateFromLoft(lofts.Select(x => x.ToNurbsCurve()), Point3d.Unset, Point3d.Unset, LoftType.Straight, false);

            if (res.Length < 1) return false;

            Brep cap = Brep.CreateTrimmedPlane(lofts[3].Plane, lofts[3].ToNurbsCurve());
            Brep joined = res[0];
            joined.Join(cap, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, true);

            m_result.Add(joined);

            return true;

            /*
            m_result.Add(Brep.CreateFromCylinder(
              new Cylinder(
              new Circle(
              new Plane(plane.Origin - plane.ZAxis * drill_depth / 2
              + plane.YAxis * m_length / 3.6,
              plane.XAxis, plane.YAxis),
              12.0), drill_depth),
              false, false));
            */
        }
    }

    /// <summary>
    /// Double-sided counter-sunk drilling feature. The workplane is in the center of the workpiece.
    /// The drill geometry goes outwards from the workplane.
    /// </summary>
    public class DoubleSidedCounterSunkDrill : FeatureX
    {
        double m_cs_radius = 8.0;
        double m_cs_depth = 4.0;
        double m_drill_radius = 6.0;
        double m_drill_depth = 100.0;

        Plane m_workplane;

        public DoubleSidedCounterSunkDrill(Plane plane, double drill_radius, double drill_depth, double countersink_radius, double countersink_depth)
        {
            m_workplane = plane;
            m_cs_radius = countersink_radius;
            m_cs_depth = countersink_depth;
            m_drill_radius = drill_radius;
            m_drill_depth = drill_depth;

        }

        public override bool Compute()
        {
            Vector3d z = m_workplane.ZAxis;
            double clearance = 10.0;
            double hdrill_depth = m_drill_depth / 2;

            Plane wp = m_workplane;

            m_result = new List<Brep>();

            Circle[] lofts = new Circle[6];
            wp.Origin = m_workplane.Origin + z * (clearance + hdrill_depth);
            lofts[0] = new Circle(wp, m_cs_radius);

            wp.Origin = m_workplane.Origin + z * (hdrill_depth - m_cs_depth);
            lofts[1] = new Circle(wp, m_cs_radius);

            wp.Origin = m_workplane.Origin + z * (hdrill_depth - m_cs_depth);
            lofts[2] = new Circle(wp, m_drill_radius);

            wp.Origin = m_workplane.Origin - z * (hdrill_depth - m_cs_depth);
            lofts[3] = new Circle(wp, m_drill_radius);

            wp.Origin = m_workplane.Origin - z * (hdrill_depth - m_cs_depth);
            lofts[4] = new Circle(wp, m_cs_radius);

            wp.Origin = m_workplane.Origin - z * (clearance + hdrill_depth);
            lofts[5] = new Circle(wp, m_cs_radius);

            Brep[] res = Brep.CreateFromLoft(lofts.Select(x => x.ToNurbsCurve()), Point3d.Unset, Point3d.Unset, LoftType.Straight, false);

            if (res.Length < 1) return false;

            m_result.AddRange(res);

            return true;
        }
    }
}
