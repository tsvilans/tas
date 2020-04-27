using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

using tas.Core;

namespace tas.Lam.Features
{
    public class EndLapJoint : FeatureX
    {
        Glulam m_glulam;

        double m_extension;
        double m_incline;
        double m_length;
        bool m_end;



        public EndLapJoint(Glulam glulam, int end, double length, double incline, double extension)
        {
            m_glulam = glulam;
            m_length = length;
            m_incline = incline;
            m_extension = extension;
            m_end = end.Modulus(2) == 0;

        }

        public override bool Compute()
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;


            double t;
            if (m_end)
                t = m_glulam.Centreline.Domain.Max - 5.0;
            else
                t = m_glulam.Centreline.Domain.Min + 5.0;

            Plane plane = m_glulam.GetPlane(t);

            if (m_end)
                plane = plane.FlipAroundYAxis();

            plane = new Plane(plane.Origin, -plane.XAxis, plane.ZAxis);

            double w = m_glulam.Width;
            double h = m_glulam.Height;

            Curve[] crvs = new Curve[4];
            crvs[0] = new Line(new Point3d(-w / 2 - m_extension, m_length, h / 2 + m_extension), new Point3d(w / 2 + m_extension, m_length, h / 2 + m_extension)).ToNurbsCurve();
            crvs[1] = new Line(new Point3d(-w / 2 - m_extension, m_length, m_incline), new Point3d(w / 2 + m_extension, m_length, m_incline)).ToNurbsCurve();
            crvs[2] = new Line(new Point3d(-w / 2 - m_extension, 0, -m_incline), new Point3d(w / 2 + m_extension, 0, -m_incline)).ToNurbsCurve();
            crvs[3] = new Line(new Point3d(-w / 2 - m_extension, 0, -h / 2 - m_extension), new Point3d(w / 2 + m_extension, 0, -h / 2 - m_extension)).ToNurbsCurve();

            Brep brep = Brep.CreateFromLoft(crvs, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

            brep.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));

            m_result = new List<Brep>();
            m_result.Add(brep);

            return true;
        }
    }

    /// <summary>
    /// Tentative replacement of EndLapJoint. Improved geometry creation and drilling geometry.
    /// </summary>
    public class EndLapJoint2 : FeatureX
    {
        Glulam m_glulam1;
        Glulam m_glulam2;

        public Glulam Glulam1 { get { return m_glulam1; } }
        public Glulam Glulam2 { get { return m_glulam2; } }

        double m_extension;
        double m_incline;
        double m_length;
        double m_t1, m_t2;
        double m_drill_depth;

        public double Extension { get { return m_extension; } set { m_extension = value; } }
        public double Incline { get { return m_incline; } set { m_incline = value; } }
        public double Length { get { return m_length; } set { m_length = value; } }
        public double T1 { get { return m_t1; } set { m_t1 = value; } }
        public double T2 { get { return m_t2; } set { m_t2 = value; } }

        public double DrillDepth { get { return m_drill_depth; } set { m_drill_depth = value; } }

        public EndLapJoint2(Glulam glulam1, Glulam glulam2, double t1, double t2, double length, double incline, double extension = 5.0, double drill_depth = 200.0)
        {
            m_glulam1 = glulam1;
            m_glulam2 = glulam2;
            m_t1 = t1;
            m_t2 = t2;

            m_length = length;
            m_incline = incline;
            m_extension = extension;
            m_drill_depth = drill_depth;
        }

        public override bool Compute()
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            Plane p1 = m_glulam1.GetPlane(m_t1);
            Plane p2 = m_glulam2.GetPlane(m_t2);

            Plane p0 = tas.Core.Util.Interpolation.InterpolatePlanes2(p1, p2, 0.5);

            Plane plane = new Plane(p0.Origin, p0.XAxis, p0.ZAxis);

            double w = Math.Max(m_glulam1.Width, m_glulam2.Width);
            double h = Math.Max(m_glulam1.Height, m_glulam2.Height);

            Curve[] crvs = new Curve[4];
            crvs[0] = new Line(new Point3d(-w / 2 - m_extension, m_length / 2, h / 2 + m_extension),
              new Point3d(w / 2 + m_extension, m_length / 2, h / 2 + m_extension)).ToNurbsCurve();
            crvs[1] = new Line(new Point3d(-w / 2 - m_extension, m_length / 2, m_incline),
              new Point3d(w / 2 + m_extension, m_length / 2, m_incline)).ToNurbsCurve();
            crvs[2] = new Line(new Point3d(-w / 2 - m_extension, -m_length / 2, -m_incline),
              new Point3d(w / 2 + m_extension, -m_length / 2, -m_incline)).ToNurbsCurve();
            crvs[3] = new Line(new Point3d(-w / 2 - m_extension, -m_length / 2, -h / 2 - m_extension),
              new Point3d(w / 2 + m_extension, -m_length / 2, -h / 2 - m_extension)).ToNurbsCurve();

            Brep brep = Brep.CreateFromLoft(crvs, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

            brep.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));

            m_result = new List<Brep>();
            m_result.Add(brep);

            var drill0 = new DoubleSidedCounterSunkDrill(
                new Plane(
                plane.Origin
                  + plane.YAxis * -m_length / 3.6,
                  plane.XAxis, plane.YAxis),
                5.0, DrillDepth, 10.0, 6.0);

            drill0.Compute();

            m_result.AddRange(drill0.GetCuttingGeometry());

            var drill1 = new DoubleSidedCounterSunkDrill(
                new Plane(plane.Origin
              + plane.YAxis * m_length / 3.6,
              plane.XAxis, plane.YAxis),
                5.0, DrillDepth, 10.0, 6.0);

            drill1.Compute();

            m_result.AddRange(drill1.GetCuttingGeometry());

            /*
            m_result.Add(Brep.CreateFromCylinder(
              new Cylinder(
              new Circle(
              new Plane(plane.Origin - plane.ZAxis * drill_depth / 2
              + plane.YAxis * -m_length / 3.6,
              plane.XAxis, plane.YAxis),
              12.0), drill_depth),
              false, false));

            m_result.Add(Brep.CreateFromCylinder(
              new Cylinder(
              new Circle(
              new Plane(plane.Origin - plane.ZAxis * drill_depth / 2
              + plane.YAxis * m_length / 3.6,
              plane.XAxis, plane.YAxis),
              12.0), drill_depth),
              false, false));
            */

            return true;
        }
    }
}
