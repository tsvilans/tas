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
            m_end = tas.Core.Util.Modulus(end, 2) == 0;

        }

        public override void Compute()
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

            double w = m_glulam.Width();
            double h = m_glulam.Height();

            Curve[] crvs = new Curve[4];
            crvs[0] = new Line(new Point3d(-w / 2 - m_extension, m_length, h / 2 + m_extension), new Point3d(w / 2 + m_extension, m_length, h / 2 + m_extension)).ToNurbsCurve();
            crvs[1] = new Line(new Point3d(-w / 2 - m_extension, m_length, m_incline), new Point3d(w / 2 + m_extension, m_length, m_incline)).ToNurbsCurve();
            crvs[2] = new Line(new Point3d(-w / 2 - m_extension, 0, -m_incline), new Point3d(w / 2 + m_extension, 0, -m_incline)).ToNurbsCurve();
            crvs[3] = new Line(new Point3d(-w / 2 - m_extension, 0, -h / 2 - m_extension), new Point3d(w / 2 + m_extension, 0, -h / 2 - m_extension)).ToNurbsCurve();

            Brep brep = Brep.CreateFromLoft(crvs, Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

            brep.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));

            m_result = new List<Brep>();
            m_result.Add(brep);
        }
    }
}
