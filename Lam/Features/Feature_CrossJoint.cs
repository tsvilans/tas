using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace tas.Lam.Features
{
    /// <summary>
    /// (obsolete) Cross-joint between two Glulam members, a la Swatch building.
    /// Tentatively superseded by CrossJoint2
    /// </summary>
    public class CrossJoint : FeatureX
    {
        Glulam m_glulamA, m_glulamB;
        double m_offset1, m_offset2;
        double offset_center;

        public CrossJoint(Glulam glulamA, Glulam glulamB, double offset1, double offset2)
        {
            m_glulamA = glulamA;
            m_glulamB = glulamB;
            m_offset1 = offset1;
            m_offset2 = offset2;
            offset_center = 20;
        }

        public override bool Compute()
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double widthA = m_glulamA.Width();
            double heightA = m_glulamA.Height();
            double widthB = m_glulamB.Width();
            double heightB = m_glulamB.Height();

            m_result = new List<Brep>();

            //
            // Find orientation relationship between glulams
            //

            Point3d ptA, ptB;
            m_glulamA.Centreline.ClosestPoints(m_glulamB.Centreline, out ptA, out ptB);

            Plane plA = m_glulamA.GetPlane(ptA);
            Plane plB = m_glulamB.GetPlane(ptB);

            int sign = 1;
            bool flip = false;

            if (plA.XAxis * plB.XAxis > 0)
            {
                flip = !flip;
                sign *= -1;
            }
            m_result = new List<Brep>();

            //
            // Make center surface
            //

            Brep[] arrbrpCenterSides = new Brep[]
              {
                    m_glulamA.GetSideSurface(0, (widthA / 2 - offset_center) * sign, heightB, 5, flip),
                    m_glulamA.GetSideSurface(0, -(widthA / 2 - offset_center) * sign, heightB, 5, flip)
              };

            Brep brpCenterFlat = m_glulamB.GetSideSurface(1, 0, widthB - offset_center, 5, flip);

            Curve[] arrcrvCenterSides = new Curve[4];

            Curve[] xCurves;
            Point3d[] xPoints;
            Rhino.Geometry.Intersect.Intersection.BrepBrep(arrbrpCenterSides[0], brpCenterFlat, tolerance, out xCurves, out xPoints);

            if (xCurves.Length > 0)
                arrcrvCenterSides[0] = xCurves[0];
            else
                return false;
                //throw new Exception("Failed to intersect: 0");

            Rhino.Geometry.Intersect.Intersection.BrepBrep(arrbrpCenterSides[1], brpCenterFlat, tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvCenterSides[1] = xCurves[0];
            else
                return false;
                //throw new Exception("Failed to intersect: 1");

            arrcrvCenterSides[2] = new Line(arrcrvCenterSides[0].PointAtStart, arrcrvCenterSides[1].PointAtStart).ToNurbsCurve();
            arrcrvCenterSides[3] = new Line(arrcrvCenterSides[0].PointAtEnd, arrcrvCenterSides[1].PointAtEnd).ToNurbsCurve();

            Brep brpCenterBrep = Brep.CreateEdgeSurface(arrcrvCenterSides);

            //
            // GlulamA Top
            //

            Brep brpGlulamATop = m_glulamA.GetSideSurface(1, -(heightA / 2 + m_offset1), widthA + m_offset2, 5, false);

            //
            // GlulamA Sides
            //

            Brep[] arrbrpGlulamASides = new Brep[2];
            arrbrpGlulamASides[0] = m_glulamA.GetSideSurface(0, (widthA / 2 + m_offset1) * sign, heightA * 2+ m_offset2, 5, flip);
            arrbrpGlulamASides[1] = m_glulamA.GetSideSurface(0, -(widthA / 2 + m_offset1) * sign, heightA * 2+ m_offset2, 5, flip);

            //
            // GlulamB Bottom
            //

            Brep brpGlulamBBtm = m_glulamB.GetSideSurface(1, heightB / 2 + m_offset1, widthB + m_offset2, 5, false);

            //
            // GlulamB Sides
            //

            Brep[] arrbrpGlulamBSides = new Brep[2];
            arrbrpGlulamBSides[0] = m_glulamB.GetSideSurface(0, (widthB / 2 + m_offset1) * sign, heightB * 2 + m_offset2, 5, flip);
            arrbrpGlulamBSides[1] = m_glulamB.GetSideSurface(0, -(widthB / 2 + m_offset1) * sign, heightB * 2 + m_offset2, 5, flip);

            //
            // Intersect GlulamA Top with GlulamB Sides
            //

            //m_result.Add(brpGlulamATop);
            //m_result.AddRange(arrbrpGlulamBSides);
            //return;

            Curve[] arrcrvATopBSides = new Curve[2];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamATop, arrbrpGlulamBSides[0], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvATopBSides[0] = xCurves[0];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamATop, arrbrpGlulamBSides[1], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvATopBSides[1] = xCurves[0];

            //
            // Intersect GlulamB Bottom with GlulamA Sides
            //
            Curve[] arrcrvBBtmASides = new Curve[2];

            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamBBtm, arrbrpGlulamASides[0], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvBBtmASides[0] = xCurves[0];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamBBtm, arrbrpGlulamASides[1], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvBBtmASides[1] = xCurves[0];

            //
            // Loft GlulamA Tops with Center
            //

            if (arrcrvCenterSides[3].TangentAtStart * arrcrvATopBSides[0].TangentAtStart < 0.0)
                arrcrvATopBSides[0].Reverse();

            Brep[] arrbrpTopCenterLoft1 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[3], arrcrvATopBSides[0] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            if (arrcrvCenterSides[2].TangentAtStart * arrcrvATopBSides[1].TangentAtStart < 0.0)
                arrcrvATopBSides[1].Reverse();

            Brep[] arrbrpTopCenterLoft2 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[2], arrcrvATopBSides[1] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            //
            // Loft GlulamB Bottoms with Center
            //

            if (arrcrvCenterSides[0].TangentAtStart * arrcrvBBtmASides[0].TangentAtStart < 0.0)
                arrcrvBBtmASides[0].Reverse();

            Brep[] arrbrpBtmCenterLoft1 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[0], arrcrvBBtmASides[0] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            if (arrcrvCenterSides[1].TangentAtStart * arrcrvBBtmASides[1].TangentAtStart < 0.0)
                arrcrvBBtmASides[1].Reverse();

            Brep[] arrbrpBtmCenterLoft2 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[1], arrcrvBBtmASides[1] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            //
            // Make webs
            //

            Brep web1 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[0].PointAtStart,
              arrcrvATopBSides[1].PointAtStart,
              arrcrvBBtmASides[0].PointAtStart,
              tolerance
              );

            Brep web2 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[0].PointAtEnd,
              arrcrvATopBSides[0].PointAtStart,
              arrcrvBBtmASides[0].PointAtEnd,
              tolerance
              );

            Brep web3 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[1].PointAtEnd,
              arrcrvATopBSides[0].PointAtEnd,
              arrcrvBBtmASides[1].PointAtEnd,
              tolerance
              );

            Brep web4 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[1].PointAtStart,
              arrcrvATopBSides[1].PointAtEnd,
              arrcrvBBtmASides[1].PointAtStart,
              tolerance
              );

            //
            // Populate the result list.
            //

            m_result.Add(brpCenterBrep);

            //m_result.Add(brpGlulamATop);
            //m_result.Add(brpGlulamBTop);
            m_result.AddRange(arrbrpTopCenterLoft1);
            m_result.AddRange(arrbrpTopCenterLoft2);
            m_result.AddRange(arrbrpBtmCenterLoft1);
            m_result.AddRange(arrbrpBtmCenterLoft2);

            m_result.Add(web1);
            m_result.Add(web2);
            m_result.Add(web3);
            m_result.Add(web4);
            //m_result.AddRange(arrbrpGlulamBSides);  

            return true;
        }
    }

    /// <summary>
    /// Tentative replacement for CrossJoint with improved orientation and geometry creation.
    /// </summary>
    public class CrossJoint2 : tas.Lam.FeatureX
    {
        // improved by Tom, Aug. 13, 2018
        Glulam m_glulamA, m_glulamB;
        double m_offset1, m_offset2;
        public double offset_center;
        double m_extension;

        public List<object> debug = new List<object>();

        public CrossJoint2(Glulam glulamA, Glulam glulamB, double offset1 = 10.0, double offset2 = 10.0, double extension = 5.0)
        {
            m_glulamA = glulamA;
            m_glulamB = glulamB;
            m_offset1 = offset1;
            m_offset2 = offset2;
            m_extension = extension;
            offset_center = 10;
        }

        public override bool Compute()
        {
            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            double widthA = m_glulamA.Width();
            double heightA = m_glulamA.Height();
            double widthB = m_glulamB.Width();
            double heightB = m_glulamB.Height();
            double drill_depth = 200.0;

            bool flip = false;

            m_result = new List<Brep>();

            //
            // Find orientation relationship between glulams
            //

            Point3d ptA, ptB;
            m_glulamA.Centreline.ClosestPoints(m_glulamB.Centreline, out ptA, out ptB);

            Point3d CP = (ptA + ptB) / 2;
            debug.Add(CP);
            Plane cPlane;

            double cDist = ptA.DistanceTo(ptB) / 2;

            Plane plA = m_glulamA.GetPlane(ptA);
            Plane plB = m_glulamB.GetPlane(ptB);

            double ofc = offset_center;
            double ofc2 = offset_center / 2;

            Vector3d vAB = ptB - ptA;
            Vector3d vBA = ptA - ptB;

            if (vAB.IsZero || vBA.IsZero)
            {
                vAB = plA.YAxis;
                vBA = -vAB;
            }
            else
            {
                vAB.Unitize();
                vBA.Unitize();
            }

            int
              yAFlip = vAB * plA.YAxis < 0 ? 1 : -1,
              yBFlip = vBA * plB.YAxis < 0 ? 1 : -1,
              xAFlip = plA.XAxis * plB.ZAxis < 0 ? 1 : -1,
              xBFlip = plB.XAxis * plA.ZAxis < 0 ? 1 : -1;

            m_result = new List<Brep>();

            //
            // Make center surface
            //

            Brep[] arrbrpCenterSides = new Brep[]
              {
        m_glulamA.GetSideSurface(0, (widthA / 2 - ofc2) * xAFlip, heightB * 3, m_extension, flip),
        m_glulamA.GetSideSurface(0, -(widthA / 2 - ofc2) * xAFlip, heightB * 3, m_extension, flip)
              };

            //debug.AddRange(arrbrpCenterSides);

            Brep brpCenterFlat = m_glulamB.GetSideSurface(1, cDist * yAFlip, widthB - ofc, m_extension, flip);

            //debug.Add(brpCenterFlat);

            Curve[] arrcrvCenterSides = new Curve[4];

            Curve[] xCurves;
            Point3d[] xPoints;
            Rhino.Geometry.Intersect.Intersection.BrepBrep(arrbrpCenterSides[0], brpCenterFlat, tolerance, out xCurves, out xPoints);

            if (xCurves.Length > 0)
                arrcrvCenterSides[0] = xCurves[0];
            else
            {
                m_result.Add(brpCenterFlat);
                m_result.AddRange(arrbrpCenterSides);
                return false;
                //throw new Exception("Failed to intersect: 0");
            }

            Rhino.Geometry.Intersect.Intersection.BrepBrep(arrbrpCenterSides[1], brpCenterFlat, tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvCenterSides[1] = xCurves[0];
            else
            {
                m_result.Add(brpCenterFlat);
                m_result.AddRange(arrbrpCenterSides);
                return false;
                //throw new Exception("Failed to intersect: 1");
            }

            Point3d[] cPoints = new Point3d[4];
            cPoints[0] = arrcrvCenterSides[0].PointAtStart;
            cPoints[1] = arrcrvCenterSides[0].PointAtEnd;
            cPoints[2] = arrcrvCenterSides[1].PointAtStart;
            cPoints[3] = arrcrvCenterSides[1].PointAtEnd;

            Plane.FitPlaneToPoints(cPoints, out cPlane);
            cPlane.Origin = CP;

            for (int i = 0; i < 4; ++i)
                cPoints[i] = cPlane.ClosestPoint(cPoints[i]);

            //arrcrvCenterSides[2] = new Line(arrcrvCenterSides[0].PointAtStart, arrcrvCenterSides[1].PointAtStart).ToNurbsCurve();
            //arrcrvCenterSides[3] = new Line(arrcrvCenterSides[0].PointAtEnd, arrcrvCenterSides[1].PointAtEnd).ToNurbsCurve();

            arrcrvCenterSides[0] = new Line(cPoints[0], cPoints[1]).ToNurbsCurve();
            arrcrvCenterSides[1] = new Line(cPoints[2], cPoints[3]).ToNurbsCurve();
            arrcrvCenterSides[2] = new Line(cPoints[0], cPoints[2]).ToNurbsCurve();
            arrcrvCenterSides[3] = new Line(cPoints[1], cPoints[3]).ToNurbsCurve();

            Brep brpCenterBrep = Brep.CreateEdgeSurface(arrcrvCenterSides);

            //
            // GlulamA Top
            //

            Brep brpGlulamATop = m_glulamA.GetSideSurface(1, (heightA / 2 + m_offset1) * -yAFlip, widthA + m_offset2, m_extension, false);

            debug.Add(brpGlulamATop);

            //
            // GlulamA Sides
            //

            Brep[] arrbrpGlulamASides = new Brep[2];
            arrbrpGlulamASides[0] = m_glulamA.GetSideSurface(0, (widthA / 2 + m_offset1) * xAFlip, heightA * 2 + m_offset2, m_extension, flip);
            arrbrpGlulamASides[1] = m_glulamA.GetSideSurface(0, -(widthA / 2 + m_offset1) * xAFlip, heightA * 2 + m_offset2, m_extension, flip);

            debug.AddRange(arrbrpGlulamASides);
            //
            // GlulamB Bottom
            //

            Brep brpGlulamBBtm = m_glulamB.GetSideSurface(1, (heightB / 2 + m_offset1) * -yBFlip, widthB + m_offset2, m_extension, false);
            debug.Add(brpGlulamBBtm);

            //
            // GlulamB Sides
            //

            Brep[] arrbrpGlulamBSides = new Brep[2];
            arrbrpGlulamBSides[0] = m_glulamB.GetSideSurface(0, (widthB / 2 + m_offset1) * xAFlip, heightB * 2 + m_offset2, m_extension, flip);
            arrbrpGlulamBSides[1] = m_glulamB.GetSideSurface(0, -(widthB / 2 + m_offset1) * xAFlip, heightB * 2 + m_offset2, m_extension, flip);

            debug.AddRange(arrbrpGlulamBSides);

            //
            // Intersect GlulamA Top with GlulamB Sides
            //

            //m_result.Add(brpGlulamATop);
            //m_result.AddRange(arrbrpGlulamBSides);
            //return;

            Curve[] arrcrvATopBSides = new Curve[2];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamATop, arrbrpGlulamBSides[0], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvATopBSides[0] = xCurves[0];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamATop, arrbrpGlulamBSides[1], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvATopBSides[1] = xCurves[0];

            if (arrcrvATopBSides[0] == null || arrcrvATopBSides[1] == null) return false;

            //
            // Intersect GlulamB Bottom with GlulamA Sides
            //
            Curve[] arrcrvBBtmASides = new Curve[2];

            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamBBtm, arrbrpGlulamASides[0], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvBBtmASides[0] = xCurves[0];
            Rhino.Geometry.Intersect.Intersection.BrepBrep(brpGlulamBBtm, arrbrpGlulamASides[1], tolerance, out xCurves, out xPoints);
            if (xCurves.Length > 0)
                arrcrvBBtmASides[1] = xCurves[0];

            if (arrcrvBBtmASides[0] == null || arrcrvBBtmASides[1] == null) return false;

            //
            // Loft GlulamA Tops with Center
            //

            if (arrcrvCenterSides[3].TangentAtStart * arrcrvATopBSides[0].TangentAtStart < 0.0)
                arrcrvATopBSides[0].Reverse();

            Brep[] arrbrpTopCenterLoft1 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[3], arrcrvATopBSides[0] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            if (arrcrvCenterSides[2].TangentAtStart * arrcrvATopBSides[1].TangentAtStart < 0.0)
                arrcrvATopBSides[1].Reverse();

            Brep[] arrbrpTopCenterLoft2 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[2], arrcrvATopBSides[1] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            //
            // Loft GlulamB Bottoms with Center
            //

            if (arrcrvCenterSides[0].TangentAtStart * arrcrvBBtmASides[0].TangentAtStart < 0.0)
                arrcrvBBtmASides[0].Reverse();

            Brep[] arrbrpBtmCenterLoft1 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[0], arrcrvBBtmASides[0] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            if (arrcrvCenterSides[1].TangentAtStart * arrcrvBBtmASides[1].TangentAtStart < 0.0)
                arrcrvBBtmASides[1].Reverse();

            Brep[] arrbrpBtmCenterLoft2 =
              Brep.CreateFromLoft(
              new Curve[] { arrcrvCenterSides[1], arrcrvBBtmASides[1] },
              Point3d.Unset, Point3d.Unset,
              LoftType.Straight, false);

            //
            // Make webs
            //

            Brep web1 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[0].PointAtStart,
              arrcrvATopBSides[1].PointAtStart,
              arrcrvBBtmASides[0].PointAtStart,
              tolerance
              );

            Brep web2 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[0].PointAtEnd,
              arrcrvATopBSides[0].PointAtStart,
              arrcrvBBtmASides[0].PointAtEnd,
              tolerance
              );

            Brep web3 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[1].PointAtEnd,
              arrcrvATopBSides[0].PointAtEnd,
              arrcrvBBtmASides[1].PointAtEnd,
              tolerance
              );

            Brep web4 = Brep.CreateFromCornerPoints(
              arrcrvCenterSides[1].PointAtStart,
              arrcrvATopBSides[1].PointAtEnd,
              arrcrvBBtmASides[1].PointAtStart,
              tolerance
              );

            //
            // Populate the result list.
            //

            m_result.Add(brpCenterBrep);

            //m_result.Add(brpGlulamATop);
            //m_result.Add(brpGlulamBTop);
            m_result.AddRange(arrbrpTopCenterLoft1);
            m_result.AddRange(arrbrpTopCenterLoft2);
            m_result.AddRange(arrbrpBtmCenterLoft1);
            m_result.AddRange(arrbrpBtmCenterLoft2);

            m_result.Add(web1);
            m_result.Add(web2);
            m_result.Add(web3);
            m_result.Add(web4);

            var temp = Brep.JoinBreps(m_result, 0.001);
            if (temp.Length > 0)
            {
                m_result.Clear();
                m_result.AddRange(temp);
            }

            m_result.Add(Brep.CreateFromCylinder(
              new Cylinder(
              new Circle(
              new Plane(cPlane.Origin - cPlane.ZAxis * drill_depth / 2,
              cPlane.XAxis, cPlane.YAxis)
              , 6.0),
              drill_depth), false, false));

            return true;
        }
    }

}
