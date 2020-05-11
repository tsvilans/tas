using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Geometry;

namespace tas.Abaqus
{
    public class Discretizer
    {
        public static Contour ToContour(Polyline poly, Plane plane = default)
        {
            if (plane.IsValid)
                poly.Transform(Transform.PlaneToPlane(plane, Plane.WorldXY));

            Contour cnt = new Contour(poly.Select(x => new Vertex(x.X, x.Y)));

            return cnt;
        }

        public static Polyline FromContour(Contour cnt, Plane plane = default)
        {
            Polyline poly = new Polyline(cnt.Points.Select(x => new Point3d(x.X, x.Y, 0)));

            if (plane.IsValid)
                poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));

            return poly;
        }

        public static Mesh ToRhinoMesh(IMesh imesh)
        {
            Mesh rmesh = new Mesh();
            foreach (var vert in imesh.Vertices)
            {
                rmesh.Vertices.Add(vert.X, vert.Y, 0);
            }

            foreach (var tri in imesh.Triangles)
            {
                rmesh.Faces.AddFace(tri.GetVertexID(0), tri.GetVertexID(1), tri.GetVertexID(2));
            }

            return rmesh;

        }

        public static Mesh Triangulate(Polyline poly, Plane plane, double min_angle = 0.436332, double max_angle = Math.PI, double max_area = double.MaxValue)
        {
            min_angle = Rhino.RhinoMath.ToDegrees(min_angle);
            max_angle = Rhino.RhinoMath.ToDegrees(max_angle);

            var options = new ConstraintOptions() { ConformingDelaunay = true };
            var quality = new QualityOptions() { MinimumAngle = min_angle, MaximumAngle = max_angle, MaximumArea = max_area, VariableArea = true };

            Contour cnt = ToContour(poly, plane);

            Polygon pgon = new Polygon();
            pgon.Add(cnt);

            var tmesh = pgon.Triangulate(options, quality);

            return ToRhinoMesh(tmesh);
        }
        public static Mesh Triangulate(Polyline poly, List<Point3d> points, Plane plane, double min_angle = 0.436332, double max_angle = Math.PI, double max_area = double.MaxValue)
        {
            min_angle = Rhino.RhinoMath.ToDegrees(min_angle);
            max_angle = Rhino.RhinoMath.ToDegrees(max_angle);

            var options = new ConstraintOptions() { ConformingDelaunay = true };
            var quality = new QualityOptions() { MinimumAngle = min_angle, MaximumAngle = max_angle, MaximumArea = max_area, VariableArea = true };

            Contour cnt = ToContour(poly, plane);

            Polygon pgon = new Polygon();
            pgon.Add(cnt);

            foreach(Point3d pt in points)
            {
                pgon.Add(new Vertex(pt.X, pt.Y));
            }

            var tmesh = pgon.Triangulate(options, quality);

            return ToRhinoMesh(tmesh);
        }


    }
}
