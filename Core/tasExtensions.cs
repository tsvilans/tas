/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017-2018 Tom Svilans
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

using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Globalization;

using tas.Core.Util;

namespace tas.Core
{
    public static class PolylineExtensionMethods
    {
        /// <summary>
        /// Calculate the normal of a Polyline according to its direction / winding direction.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>Normal of Polyline.</returns>
        public static Vector3d CalculateNormal(this Polyline p)
        {
            Vector3d normal = Vector3d.Zero;
            for (int i = 0; i < p.Count - 1; ++i)
            {
                normal += Vector3d.CrossProduct(new Vector3d(p[i]), new Vector3d(p[i + 1]));
            }
            if (!p.IsClosed)
            {
                normal += Vector3d.CrossProduct(new Vector3d(p.Last), new Vector3d(p.First));
            }

            normal.Unitize();

            return normal;
        }

        public static Polyline CreateRamp(this Polyline poly, Plane pl, double height, double length)//, ref string debug)
        {
            poly.Reverse();
            int N = poly.Count;
            if (poly.IsClosed)
                N--;
            double th = 0.0;
            double td = 0.0;
            int i = 0;
            int next = 1;
            Vector3d dv;
            List<Point3d> rpts = new List<Point3d>();

            while (th < length)
            {
                // distance between i and next
                dv = poly[next] - poly[i];
                td = dv.Length;
                if (th + td >= length)
                {
                    double t = ((th + td) - length) / td; // get t value for lerp
                    rpts.Add(poly[i]);
                    rpts.Add(Util.Interpolation.Lerp(poly[i], poly[next], 1.0 - t));

                    break;
                }
                th += td;
                rpts.Add(poly[i]);
                i = (i + 1) % N;
                next = (i + 1) % N;
            }

            List<double> el = new List<double>();
            for (int j = 0; j < rpts.Count; ++j)
            {
                int ind = (j + 1) % rpts.Count;
                double d = rpts[j].DistanceTo(rpts[ind]);
                el.Add(d);
            }
            double L = el.Sum();

            double LL = 0.0;
            for (int j = 0; j < rpts.Count - 1; ++j)
            {
                LL = el.Take(j + 1).Sum();
                double z = (LL / L) * height;
                rpts[j + 1] = new Point3d(rpts[j + 1] + pl.ZAxis * z);
            }
            rpts.Reverse();

            return new Polyline(rpts);
        }

        /// <summary>
        /// Closes open polyline. Returns false if polyline is already closed.
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static bool Close(this Polyline poly)
        {
            if (!poly.IsClosed)
            {
                poly.Add(poly.First);
                return true;
            }
            return false;

        }

        /// <summary>
        /// Slices up polyline with a plane.
        /// </summary>
        /// <param name="poly">Input polyline.</param>
        /// <param name="p">Slicing plane.</param>
        /// <param name="clip">Remove segments above plane.</param>
        /// <param name="close">Close clipped segments.</param>
        /// <returns></returns>
        public static List<Polyline> PlaneSlice(this Polyline poly, Plane p, bool clip = false, bool close = false)
        {
            List<bool> SliceSides = new List<bool>();
            List<Tuple<int, int>> XSeg = new List<Tuple<int, int>>();

            if (poly.Count < 2) return new List<Polyline>();

            bool CurrentSide = (p.Normal * new Vector3d(poly[0] - p.Origin) > 0.0);
            bool Temp = CurrentSide;

            int N = poly.Count;
            //if (poly.IsClosed) --N;
            for (int i = 0; i < N - 1; ++i)
            {
                //Temp = (p.Normal * new Vector3d(poly[i + 1] - poly[i]) > 0.0);
                Temp = (p.Normal * new Vector3d(poly[i + 1] - p.Origin) > 0.0);
                if (Temp != CurrentSide)
                {
                    XSeg.Add(new Tuple<int, int>(i, i + 1));
                    SliceSides.Add(Temp);
                }
                CurrentSide = Temp;
            }

            SliceSides.Add(!Temp);

            if (XSeg.Count < 1)
                return new List<Polyline>() { poly };

            // Slice polyline into pieces
            List<Polyline> Slices = new List<Polyline>();

            int index = 0;
            Point3d index_point = Point3d.Unset;

            List<Point3d> pts;
            for (int i = 0; i < XSeg.Count; ++i)
            {
                // Extract active range from original polyline
                pts = poly.GetRange(index, XSeg[i].Item2 - index).ToList();
                if (index_point.IsValid) pts[0] = index_point;
                index_point = Intersection.PlaneLineIntersection(new Line(poly[XSeg[i].Item1], poly[XSeg[i].Item2]), p);
                pts.Add(index_point);

                Slices.Add(new Polyline(pts));

                // Update current index
                index = XSeg[i].Item1;
            }

            // Handle last segment, including joining if original polyline is closed
            if (poly.IsClosed)
            {
                pts = poly.GetRange(index, poly.Count - index - 1).ToList();
                if (index_point.IsValid) pts[0] = index_point;
                if (Slices.Count > 0)
                    Slices.First().InsertRange(0, pts);
                else
                    Slices.Add(new Polyline(pts));
            }
            else
            {
                pts = poly.GetRange(index, poly.Count - index).ToList();
                if (index_point.IsValid) pts[0] = index_point;
                Slices.Add(new Polyline(pts));
            }

            List<Polyline> KeepSlices = new List<Polyline>();

            for (int i = 0; i < Slices.Count; ++i)
            {
                Polyline temp = Slices[i];
                // If Close option is enabled
                if (close)// && i > 0)
                    temp.Close();

                // If Clip option is enabled
                if (!clip || SliceSides[i])
                    KeepSlices.Add(temp);
            }

            return KeepSlices;
        }

        /// <summary>
        /// Calculate frames along Polyline based on surrounding vertices. 
        /// </summary>
        /// <param name="poly"></param>
        /// <returns>List of Planes objects.</returns>
        public static List<Plane> CalculateNormals(this Polyline poly)
        {
            List<Vector3d> normals = new List<Vector3d>();
            List<Vector3d> sums = new List<Vector3d>();

            normals.Add(new Vector3d());
            sums.Add(new Vector3d());

            List<Plane> xplanes = new List<Plane>();

            Vector3d aab, bab, sum, normal;
            Point3d a, b, ab;

            for (int i = 1; i < poly.Count - 1; ++i)
            {
                a = poly[i - 1];
                b = poly[i + 1];
                ab = poly[i];


                aab = new Vector3d(ab - a);
                aab.Unitize();

                bab = new Vector3d(ab - b);
                bab.Unitize();

                sum = new Vector3d(aab + bab);

                normal = Vector3d.CrossProduct(aab, bab);
                if (Vector3d.Multiply(normal, normals[normals.Count - 1]) < 0.0)
                {
                    normal.Reverse();
                    sum.Reverse();
                }

                normals.Add(normal);
                xplanes.Add(new Plane(ab, sum, normal));
            }

            normals.Add(normals[normals.Count - 1]);
            normals[0] = normals[1];

            return xplanes;
        }

        /// <summary>
        /// Test polyline to see if it is clockwise around a guide vector.
        /// </summary>
        /// <param name="pl">Polyline to test.</param>
        /// <param name="vec">Guide vector for establishing directionality.</param>
        /// <returns>True if polyline is CW around vector, false if not.</returns>
        public static bool IsClockwise(this Polyline pl, Vector3d vec)
        {
            double temp = 0.0;

            for (int i = 0; i < pl.SegmentCount - 1; ++i)
            {
                var v1 = pl.SegmentAt(i).Direction;
                var v2 = pl.SegmentAt(i + 1).Direction;

                var cross = Vector3d.CrossProduct(v1, v2);
                temp += cross * vec;
            }

            return temp > 0;
        }

        /// <summary>
        /// Deprecated.
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="tolerance"></param>
        /// <param name="seam"></param>
        /// <returns></returns>
        public static Polyline SimplifyPolyline(this Polyline poly, double tolerance = 1e-12, bool seam = false)
        {
            Polyline new_poly = poly.Duplicate();
            new_poly.MergeColinearSegments(tolerance, seam);

            return new_poly;
            /*
            List<Point3d> pts = new List<Point3d>();
            pts.Add(poly[0]);
            for (int i = 1; i < poly.Count - 1; ++i)
            {
                if (!tas.Core.Util.Misc.ArePointsCollinear(poly[i - 1], poly[i], poly[i + 1], tolerance))
                {
                    pts.Add(poly[i]);
                }
            }
            pts.Add(poly[poly.Count - 1]);
            return new Polyline(pts);
            */
        }

    }

    public static class Vector3dExtensionMethods
    {
        /// <summary>
        /// Project vector onto plane.
        /// </summary>
        /// <param name="v">Vector to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected vector.</returns>
        public static Vector3d ProjectToPlane(this Vector3d v, Plane p)
        {
            return new Vector3d(v - (p.ZAxis * Vector3d.Multiply(p.ZAxis, v)));

            //double dot = Vector3d.Multiply(p.ZAxis, v);
            //Vector3d v2 = p.ZAxis * dot;
            //return new Vector3d(v - v2);
        }

        /// <summary>
        /// Transforms 1st-rank tensor from coordinate system CS1 to CS2.
        /// </summary>
        /// <param name="v">Input vector in coordinate system CS1.</param>
        /// <param name="CS1">Original coordinate system (3 vectors).</param>
        /// <param name="CS2">Transformed coordinate system (3 vectors).</param>
        /// <returns></returns>
        public static Vector3d TensorTransformation(this Vector3d v, Vector3d[] CS1, Vector3d[] CS2)
        {
            if (CS1.Length != 3 || CS2.Length != 3) return v;

            Matrix angles = new Matrix(3, 3);

            angles[0, 0] = CS1[0] * CS2[0];
            angles[0, 1] = CS1[1] * CS2[0];
            angles[0, 2] = CS1[2] * CS2[0];

            angles[1, 0] = CS1[0] * CS2[1];
            angles[1, 1] = CS1[1] * CS2[1];
            angles[1, 2] = CS1[2] * CS2[1];

            angles[2, 0] = CS1[0] * CS2[2];
            angles[2, 1] = CS1[1] * CS2[2];
            angles[2, 2] = CS1[2] * CS2[2];

            Vector3d vv = new Vector3d
            {
                X = v.X * angles[0, 0] + v.Y * angles[0, 1] + v.Z * angles[0, 2],
                Y = v.X * angles[1, 0] + v.Y * angles[1, 1] + v.Z * angles[1, 2],
                Z = v.X * angles[2, 0] + v.Y * angles[2, 1] + v.Z * angles[2, 2]
            };

            return vv;
        }

        /// <summary>
        /// Return unitized vector. Convenience method.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3d Unitized(this Vector3d v)
        {
            Vector3d vv = new Vector3d(v);
            vv.Unitize();
            return vv;
        }

    }

    public static class MeshExtensionMethods
    {
        /// <summary>
        /// Returns Mesh texture coordinates as a base64-encoded string of space-separated values.
        /// </summary>
        /// <param name="m"></param>
        /// <returns>Base64 string</returns>
        public static string TextureCoordinatesToBase64(this Mesh m)
        {
            if (m.TextureCoordinates.Count != m.Vertices.Count) throw new Exception("Mesh does not have texture coordinates!");
            int N = m.TextureCoordinates.Count;
            double[] uvs = new double[N * 2];

            System.Text.StringBuilder uv_string = new System.Text.StringBuilder();

            for (int i = 0; i < N; ++i)
            {
                // uv_string.Append(string.Format("{0} {1} ", m.TextureCoordinates[i].X, m.TextureCoordinates[i].Y));
                uv_string.Append(m.TextureCoordinates[i].X.ToString(CultureInfo.InvariantCulture.NumberFormat));
                uv_string.Append(" ");
                uv_string.Append(m.TextureCoordinates[i].Y.ToString(CultureInfo.InvariantCulture.NumberFormat));
                uv_string.Append(" ");

                uvs[i * 2] = m.TextureCoordinates[i].X;
                uvs[i * 2 + 1] = m.TextureCoordinates[i].Y;
            }

            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(uv_string.ToString()));
        }

        /// <summary>
        /// Parses base64-encoded string of space-separated values as texture coordinates.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="coords">Base64 string</param>
        public static void Base64ToTextureCoordinates(this Mesh m, string coords)
        {
            byte[] bytes = System.Convert.FromBase64String(coords);
            string coord_string = System.Text.Encoding.UTF8.GetString(bytes);
            string[] chunks = coord_string.Split(null);

            if (chunks.Length * 2 != m.Vertices.Count)
                throw new Exception("Texture coordinates don't match vertex count!");

            m.TextureCoordinates.Clear();

            for (int i = 0; i < chunks.Length; i += 2)
            {
                m.TextureCoordinates.Add(new Point2f(
                    float.Parse(chunks[i], CultureInfo.InvariantCulture.NumberFormat),
                    float.Parse(chunks[i + 1], CultureInfo.InvariantCulture.NumberFormat)
                    ));
            }
        }

        /// <summary>
        /// Returns a submesh defined by a list of faces of an existing mesh.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="face_indices">Faces to include in new mesh.</param>
        /// <returns></returns>
        public static Mesh ExtractSubMesh(this Mesh m, List<int> face_indices)
        {
            Mesh mesh = m.DuplicateMesh();
            int N = face_indices.Count;
            MeshFace[] new_faces = new MeshFace[N];

            for (int i = 0; i < N; ++i)
                new_faces[i] = m.Faces[face_indices[i]];

            mesh.Faces.Clear();
            mesh.Faces.AddFaces(new_faces);

            mesh.Compact();

            return mesh;
        }

        public static Mesh Amalgamate(this Mesh[] meshes)
        {
            Mesh m = new Mesh();
            if (meshes != null && meshes.Length > 0)
            {
                if (meshes.Length == 1)
                    return meshes.First();

                foreach (Mesh mesh in meshes)
                {
                    m.Append(mesh);
                }
            }
            m.Compact();
            return m;
        }

        public static Mesh FitToAxes(this Mesh m, Plane p, out Polyline convex_hull, ref Plane transform)
        {
            List<Point3d> pts = m.Vertices.Select(x => new Point3d(x)).ToList();

            convex_hull = pts.GetConvexHull(p, false);

            convex_hull.DeleteShortSegments(0.01);

            double angle = 0;
            int index = 0;
            double area = 0;
            double w, h;
            double temp = double.MaxValue;
            BoundingBox bb;

            for (int i = 0; i < convex_hull.SegmentCount; ++i)
            {
                Line seg = convex_hull.SegmentAt(i);
                Polyline ppoly = new Polyline(convex_hull);
                Vector3d dir = seg.Direction;
                dir.Unitize();
                //double temp_angle = Vector3d.VectorAngle(Vector3d.YAxis, dir);
                double temp_angle = Math.Atan2(dir.Y, dir.X);

                ppoly.Transform(Transform.Rotation(temp_angle, Vector3d.ZAxis, ppoly.CenterPoint()));
                bb = ppoly.BoundingBox;

                w = bb.Max.X - bb.Min.X;
                h = bb.Max.Y - bb.Min.Y;

                if (w < h) temp_angle += Math.PI / 2;
                area = w * h;

                if (Math.Abs(area - temp) < 0.01 && temp_angle < angle)
                {
                    temp = area;
                    index = i;
                    angle = temp_angle;
                }
                else if (area < temp)
                {
                    temp = area;
                    index = i;
                    angle = temp_angle;
                }
            }

            angle = -angle;

            convex_hull.Transform(Transform.Rotation(angle, Vector3d.ZAxis, convex_hull.CenterPoint()));
            Transform xform = Transform.Rotation(angle, p.ZAxis, p.Origin);
            m.Transform(xform);
            transform.Transform(xform);

            return m;
        }

        /// <summary>
        /// Generate pointcloud with N points on mesh.
        /// </summary>
        /// <param name="m">Mesh to generate pointcloud on.</param>
        /// <param name="N">Number of points to generate.</param>
        /// <returns></returns>
        public static PointCloud PointCloud(this Mesh m, int N)
        {
            Mesh M = m.DuplicateMesh();
            m.Faces.ConvertQuadsToTriangles();
            AreaMassProperties amp = AreaMassProperties.Compute(M);

            List<int> NumPerFace = new List<int>();
            List<Point3d> Points = new List<Point3d>();
            List<Vector3d> Normals = new List<Vector3d>();

            Random rnd = new Random();
            Point3d test;

            for (int i = 0; i < M.Faces.Count; ++i)
            {
                Point3d A, B, C;
                A = M.Vertices[M.Faces[i].A];
                B = M.Vertices[M.Faces[i].B];
                C = M.Vertices[M.Faces[i].C];

                test = new Point3d((A.X + B.X + C.X) / 3, (A.Y + B.Y + C.Y) / 3, (A.Z + B.Z + C.Z) / 3);
                M.ClosestPoint(test, out _, out Vector3d Normal, 0.0);

                if (Normal == null) continue;

                double area = Triangle.Area(A, B, C);
                //Vector3d Normal = Util.TriangleNormal(A, B, C);

                int npf = (int)(N * area / amp.Area);
                NumPerFace.Add(npf);

                for (int j = 0; j < npf; ++j)
                {
                    Points.Add(Triangle.PointOnTriangle(A, B, C, rnd.NextDouble(), rnd.NextDouble()));
                    Normals.Add(Normal);
                }
            }

            PointCloud pc = new PointCloud(Points);
            for (int i = 0; i < pc.Count; ++i)
            {
                pc[i].Normal = Normals[i];
            }
            return pc;
        }
        /// <summary>
        /// Slices a mesh with a plane and caps the resulting planar hole.
        /// </summary>
        /// <param name="m">Mesh to slice.</param>
        /// <param name="p">Slicing plane.</param>
        /// <param name="side">Side of plane to keep.</param>
        /// <returns>Sliced and capped mesh.</returns>
        public static Mesh CutAndCap(this Mesh m, Plane p, bool side, double Tolerance = 0.01)
        {
            int s = side ? 0 : 1;
            Mesh[] splits = m.Split(p);
            if (splits == null || splits.Length < 1) return m;
            Mesh cut = splits[s];

            Polyline[] Slices = Rhino.Geometry.Intersect.Intersection.MeshPlane(m, p);
            if (Slices == null || Slices.Length < 1) return null;

            foreach (Polyline pl in Slices)
            {
                if (pl == null) continue;

                MeshFace[] MF = pl.TriangulateClosedPolyline();
                int N = cut.TopologyVertices.Count;

                List<int> Remap = new List<int>();
                for (int i = 0; i < pl.Count - 1; ++i)
                    for (int j = 0; j < cut.Vertices.Count; ++j)
                        if (pl[i].DistanceTo(cut.Vertices[j]) < Tolerance)
                        {
                            Remap.Add(j);
                            break;
                        }
                Remap.Add(Remap.First());

                List<MeshFace> NewMF = new List<MeshFace>();
                for (int i = 0; i < MF.Length; ++i)
                {
                    MeshFace mf;
                    if (MF[i].IsQuad)
                        mf = new MeshFace(
                          Remap[MF[i].A],
                          Remap[MF[i].B],
                          Remap[MF[i].C],
                          Remap[MF[i].D]);
                    else
                        mf = new MeshFace(
                          Remap[MF[i].A],
                          Remap[MF[i].B],
                          Remap[MF[i].C]);
                    NewMF.Add(mf);
                }
                cut.Faces.AddFaces(NewMF);
            }

            cut.UnifyNormals();
            cut.Normals.ComputeNormals();

            cut.Weld(3.14);
            cut.Vertices.CombineIdentical(true, true);

            return cut;
        }

        public static int[] CheckFibreCuttingAngle(this Mesh m, double angle = 0.0872665)
        {
            //Mesh mm = MapToCurveSpace(m);
            m.FaceNormals.ComputeFaceNormals();

            List<int> fi = new List<int>();

            for (int i = 0; i < m.Faces.Count; ++i)
            {
                double dot = Math.Abs(m.FaceNormals[i] * Vector3d.ZAxis);
                if (dot > Math.Sin(angle))
                {
                    fi.Add(i);
                }
            }

            return fi.ToArray();
        }

        public static Mesh FitToAxes(this Mesh m, Plane p)
        {
            Plane pp = Plane.WorldXY;
            return m.FitToAxes(p, out _, ref pp);
        }
    }

    public static class Point3dExtensionMethods
    {
        /// <summary>
        /// Project point onto plane.
        /// </summary>
        /// <param name="p">Point to project.</param>
        /// <param name="pl">Plane to project onto.</param>
        /// <returns>Projected point.</returns>
        public static Point3d ProjectToPlane(this Point3d pt, Plane p)
        {
            Vector3d op = new Vector3d(pt - p.Origin);
            double dot = Vector3d.Multiply(p.ZAxis, op);
            Vector3d v = p.ZAxis * dot;
            return new Point3d(pt - v);
        }

        /// <summary>
        /// Tests if points are coincident within a specified 
        /// tolerance.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="Tolerance"></param>
        /// <returns></returns>
        public static bool IsCoincident(this Point3d p1, Point3d p2, double Tolerance = 0.000000001)
        {
            return p1.DistanceTo(p2) < Tolerance;
        }

        /// <summary>
        /// Get 2d convex hull projected onto a plane. Uses the Jarvis march algorithm, as described on Wikipedia.
        /// </summary>
        /// <param name="pts">Points to hull.</param>
        /// <param name="plane">Plane to project points onto.</param>
        /// <returns></returns>
        public static Polyline GetConvexHull(this List<Point3d> pts, Plane plane, bool transformed = true)
        {
            if (pts.Count < 1) return null;
            //Transform xform = Transform.PlaneToPlane(plane, Plane.WorldXY);
            //List<Point3d> pts_xformed = new List<Point3d>();
            for (int i = 0; i < pts.Count; ++i)
            {
                plane.RemapToPlaneSpace(pts[i], out Point3d m_temp);
                pts[i] = m_temp;
                //Point3d p = pts[i];
                //p.Transform(xform);
                //p.Z = 0;
                //pts_xformed.Add(m_temp);
            }
            //pts = pts_xformed;
            pts = pts.OrderBy(x => x.X).ToList();

            Point3d poh = pts[0];
            Point3d ep = pts[0];
            List<Point3d> chpts = new List<Point3d>();
            int index = 0;

            do
            {
                chpts.Add(poh);
                ep = pts[index];
                for (int j = 0; j < pts.Count; ++j)
                {
                    if (ep == poh || (ep.X - chpts[index].X) * (pts[j].Y - chpts[index].Y) - (ep.Y - chpts[index].Y) * (pts[j].X - chpts[index].X) < 0)
                        ep = pts[j];
                }
                index++;
                poh = ep;
            }
            while (ep != pts[0] && index < pts.Count);

            Polyline poly = new Polyline(chpts);
            poly.Add(poly.First());

            if (transformed)
                poly.Transform(Transform.PlaneToPlane(Plane.WorldXY, plane));
            return poly;
        }
    }

    public static class PlaneExtensionMethods
    {
        /// <summary>
        /// Flip Plane around its X-axis (flip Y-axis).
        /// </summary>
        /// <param name="P"></param>
        public static Plane FlipAroundXAxis(this Plane P)
        {
            return new Plane(P.Origin, P.XAxis, -P.YAxis);
        }

        /// <summary>
        /// Flip Plane around its Y-axis (flip X-axis).
        /// </summary>
        /// <param name="P"></param>
        public static Plane FlipAroundYAxis(this Plane P)
        {
            return new Plane(P.Origin, -P.XAxis, P.YAxis);
        }

        public static Transform ProjectAlongVector(this Plane Pln, Vector3d V)
        {
            Transform oblique = new Transform(1);
            double[] eq = Pln.GetPlaneEquation();
            double a, b, c, d, dx, dy, dz, D;
            a = eq[0];
            b = eq[1];
            c = eq[2];
            d = eq[3];
            dx = V.X;
            dy = V.Y;
            dz = V.Z;
            D = a * dx + b * dy + c * dz;
            oblique.M00 = 1 - a * dx / D;
            oblique.M01 = -a * dy / D;
            oblique.M02 = -a * dz / D;
            oblique.M03 = 0;
            oblique.M10 = -b * dx / D;
            oblique.M11 = 1 - b * dy / D;
            oblique.M12 = -b * dz / D;
            oblique.M13 = 0;
            oblique.M20 = -c * dx / D;
            oblique.M21 = -c * dy / D;
            oblique.M22 = 1 - c * dz / D;
            oblique.M23 = 0;
            oblique.M30 = -d * dx / D;
            oblique.M31 = -d * dy / D;
            oblique.M32 = -d * dz / D;
            oblique.M33 = 1;
            oblique = oblique.Transpose();
            return oblique;
        }
    }

    public static class CurveExtensionMethods
    {
        /// <summary>
        /// Change curvature of arc-curve while keeping lengths and continuity.
        /// </summary>
        /// <param name="pc">PolyCurve to over-/under-bend.</param>
        /// <param name="t">Bending factor. 1.0 is no change. 0.5 is relaxing the curvature by half. 1.5 is overbending by half.</param>
        /// <returns>New over-/under-bent PolyCurve.</returns>
        public static PolyCurve OverBend(this PolyCurve pc, double t)
        {
            PolyCurve pc2 = new PolyCurve();
            List<Arc> Arcs = new List<Arc>();
            for (int i = 0; i < pc.SegmentCount; ++i)
            {
                if (pc.SegmentCurve(i).IsArc())
                {
                    Arc a;
                    if (!pc.SegmentCurve(i).TryGetArc(out a))
                        break;
                    Arcs.Add(a);
                }
            }

            if (Arcs.Count < 2) return pc;

            Transform X = Transform.Identity;

            for (int i = 0; i < Arcs.Count; ++i)
            {
                Arc ArcOld = Arcs[i];
                ArcOld.Transform(X);

                double OAngle = ArcOld.Angle;
                double NAngle = OAngle * t;
                double dRadius = (ArcOld.Radius * ArcOld.Angle) / NAngle;

                Plane p = ArcOld.Plane;
                if (ArcOld.AngleDomain.Min < 0.0)
                    p.Transform(Transform.Rotation(-ArcOld.Angle, p.ZAxis, p.Origin));

                if (NAngle < 0.0) NAngle *= -1;

                Arc a = new Arc(
                  p,
                  p.Origin - p.XAxis * (dRadius - ArcOld.Radius),
                  dRadius,
                  NAngle);

                // get transform between old end and new end

                Plane p_new = new Plane(p);
                p.Origin = ArcOld.EndPoint;
                p_new.Origin = a.EndPoint;
                p_new.Transform(Transform.Rotation(NAngle - OAngle, p_new.ZAxis, p_new.Origin));


                Transform XX = Transform.PlaneToPlane(p, p_new);
                //X = XX;
                X = Transform.Multiply(XX, X);

                pc2.Append(a);
            }
            return pc2;
        }

        /// <summary>
        /// Gets a plane that is aligned to the curve start and end points, with the Z-axis point in the direction of most curvature.
        /// Kind of like a best-fit for a bounding box.
        /// </summary>
        /// <param name="c">Input curve.</param>
        /// <param name="Samples">Number of samples for calculating the Z-axis.</param>
        /// <returns>Decent-fit plane.</returns>
        public static Plane GetAlignedPlane(this Curve c, int Samples, out double Mag)
        {
            if (c.IsLinear())
            {
                Plane p;
                //c.PerpendicularFrameAt(c.Domain.Min, out p);
                c.FrameAt(c.Domain.Min, out p);
                Mag = 0;
                return p;
                //return new Plane(p.Origin, p.XAxis, p.ZAxis);
            }

            Point3d start = c.PointAtStart;
            Point3d end = c.PointAtEnd;

            Vector3d YAxis = end - start;
            YAxis.Unitize();

            Plane SortingPlane = new Plane((end + start) / 2, YAxis);

            Point3d[] DivPts;
            c.DivideByCount(Samples, false, out DivPts);

            Vector3d ZAxis = new Vector3d();

            foreach (Point3d p in DivPts)
            {
                ZAxis += (p.ProjectToPlane(SortingPlane) - SortingPlane.Origin);
            }

            Mag = ZAxis.Length / Samples;
            YAxis.Unitize();

            Vector3d XAxis = Vector3d.CrossProduct(ZAxis, YAxis);

            return new Plane(start, XAxis, YAxis);
        }

        /// <summary>
        /// Gets a plane that is aligned to the curve start and end points, with the Z-axis point in the direction of most curvature.
        /// Kind of like a best-fit for a bounding box.
        /// </summary>
        /// <param name="c">Input curve.</param>
        /// <param name="Samples">Number of samples for calculating the Z-axis.</param>
        /// <returns>Decent-fit plane.</returns>
        public static Plane GetAlignedPlane(this Curve c, int Samples)
        {
            double Mag;
            return c.GetAlignedPlane(Samples, out Mag);
        }

        /// <summary>
        /// Checks to see if the curve is roughly aligned with a vector. This uses the start and 
        /// end points to create a vector to check with, so it is useless for closed curves
        /// or ones with excessive curvature.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="t">Parameter at 'bottom' of the curve, at the end which is the furthest 
        /// from the vector.</param>
        /// <param name="v">Vector to compare to. If null, the Z-axis will be used.</param>
        /// <param name="limit">Dot product limit to test if it is aligned.</param>
        /// <returns>Whether or not the curve is 'aligned' with the vector.</returns>
        public static bool IsAlignedWithVector(this Curve c, out double t, Vector3d? v, double limit = 0.5)
        {
            if (v == null) v = Vector3d.ZAxis;
            t = 0.0;
            double m;
            Plane p = c.GetAlignedPlane(5, out m);
            if (p.YAxis * v > limit)
            {
                t = c.Domain.Min;
                return true;
            }
            else if (-p.YAxis * v > limit)
            {
                t = c.Domain.Max;
                return true;
            }
            return false;
        }

        public static Mesh MapToCurveSpace(this Curve curve, Mesh m)
        {
            Mesh mesh;
            if (curve.IsLinear())
            {
                mesh = m.DuplicateMesh();
                Plane p;
                curve.PerpendicularFrameAt(curve.Domain.Min, out p);

                p.Origin = curve.PointAtStart;

                mesh.Transform(Rhino.Geometry.Transform.PlaneToPlane(p, Plane.WorldXY));
            }
            else if (curve.IsPlanar())
            {
                Plane cp, cpp = Plane.Unset;
                if (!curve.TryGetPlane(out cp))
                    throw new Exception("SingleCurvedGlulam: Centreline is not planar!");
                double t, l;

                mesh = new Mesh();

                for (int i = 0; i < m.Vertices.Count; ++i)
                {
                    Point3d p = new Point3d(m.Vertices[i]);
                    curve.ClosestPoint(p, out t);
                    l = curve.GetLength(new Interval(curve.Domain.Min, t));
                    Vector3d xaxis = Vector3d.CrossProduct(cp.ZAxis, curve.TangentAt(t));
                    cpp = new Plane(curve.PointAt(t), xaxis, cp.ZAxis);
                    p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cpp, Plane.WorldXY));
                    p.Z = l;

                    mesh.Vertices.Add(p);
                }
                mesh.Faces.AddFaces(m.Faces);

            }
            else
            {
                Plane cp;
                double t, l;
                Point3d m_point1, m_point2;
                mesh = new Mesh();

                List<Point3d> verts = new List<Point3d>();
                //object m_lock = new object();

                //Parallel.For(0, m.Vertices.Count, i =>
                for (int i = 0; i < m.Vertices.Count; ++i)
                {
                    m_point1 = m.Vertices[i];
                    curve.ClosestPoint(m_point1, out t);
                    l = curve.GetLength(new Interval(curve.Domain.Min, t));
                    curve.PerpendicularFrameAt(t, out cp);
                    cp.RemapToPlaneSpace(m_point1, out m_point2);
                    m_point2.Z = l;

                    //    lock (m_lock)
                    //    {
                    verts.Add(m_point2);
                    //    }
                }
                //});

                mesh.Vertices.AddVertices(verts);
                mesh.Faces.AddFaces(m.Faces);
            }

            mesh.FaceNormals.ComputeFaceNormals();
            return mesh;
        }

        public static Curve Relax(this Curve c, double factor, bool make_single_curved = false, Plane? projection_plane = null, bool rebuild = false)
        {
            NurbsCurve NC = c.ToNurbsCurve();

            List<Point3d> CtrlPts = NC.Points.Select(x => x.Location).ToList();
            Line BestFit;
            Line.TryFitLineToPoints(CtrlPts, out BestFit);
            Plane plane = Plane.Unset;


            if (make_single_curved)
            {
                if (projection_plane.HasValue)
                {
                    plane = projection_plane.Value;
                    plane.Origin = NC.PointAt(NC.Domain.Mid);
                }

                else
                {
                    Plane.FitPlaneToPoints(CtrlPts, out plane);
                }
            }

            for (int i = 0; i < CtrlPts.Count; ++i)
            {
                Vector3d v = BestFit.ClosestPoint(CtrlPts[i], false) - CtrlPts[i];
                //NC.Points[i].Location += v * 0.2;
                Point3d pt = NC.Points[i].Location + v * factor;

                if (make_single_curved)
                    pt = pt.ProjectToPlane(plane);

                NC.Points[i] = new ControlPoint(pt,
                  NC.Points[i].Weight);
            }

            if (rebuild)
                NC = NC.Rebuild(NC.Points.Count, NC.Degree, false);

            NC.Domain = new Interval(0, NC.GetLength());

            return NC;
        }

    }

    public static class BrepExtensionMethods
    {
        public static Brep BrepSlice(this Brep b, Brep slicer, bool flip = false)
        {
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            /*
            Curve[] iCrvs;
            Point3d[] iPts;
            if (!Rhino.Geometry.Intersect.Intersection.BrepBrep(b, slicer, tol, out iCrvs, out iPts))
                return new Brep();
            */
            //if (b.IsSolid && b.SolidOrientation !=)

            if (slicer.IsSolid)
            {
                Brep[] boolean = Brep.CreateBooleanDifference(b, slicer, tol);

                if (boolean == null || boolean.Length < 1) return b;
                return boolean[0];
            }

            if (b.SolidOrientation != BrepSolidOrientation.Outward)
                b.Faces.Flip(true);

            slicer.Faces.StandardizeFaceSurfaces();
            if (slicer.Faces[0].OrientationIsReversed) flip = !flip;

            Brep[] sliced = b.Split(slicer, tol);

            if (sliced == null || sliced.Length < 1)
                return b;

            Brep[] cap = slicer.Trim(b, tol);

            Brep brep = null;

            if (flip)
                brep = sliced[1];
            else brep = sliced[0];

            if (cap != null && cap.Length > 0)
            {
                Brep[] joined = Brep.JoinBreps(cap, tol);
                brep.Join(joined[0], tol, true);
            }

            return brep;
        }

        public static Brep[] BrepSlice(this Brep b, Brep[] slicers, bool flip = false)
        {
            //Brep input = b;
            //Brep res = b;

            //for (int i = 0; i < slicers.Length; ++i)
            //{
            //    input = res;
            //    res = input.BrepSlice(slicers[i]);
            //}

            //return res;


            Brep[] cut = new Brep[] { b };
            List<Brep> temp;

            for (int i = 0; i < slicers.Length; ++i)
            {
                if (flip)
                    slicers[i].Flip();

                temp = new List<Brep>();
                int N = cut.Length;
                for (int j = 0; j < N; ++j)
                {
                    //Brep[] breps = cut[j].Split(slicers[i], Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    Brep[] breps = cut[j].Trim(slicers[i], Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    Brep[] caps = slicers[i].Split(cut[j], Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    caps = Brep.JoinBreps(caps, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

                    Curve[] int_crvs;
                    Point3d[] int_pts;
                    Rhino.Geometry.Intersect.Intersection.BrepBrep(slicers[i], cut[j], 0.01, out int_crvs, out int_pts);

                    if (breps == null || breps.Length < 1)
                        continue;

                    /*
                    for (int k = 0; k < breps.Length; ++k)
                    {
                        //Point3d v = breps[k].Vertices.First().Location;
                        //BoundingBox bb = breps[k].GetBoundingBox(true);
                        //Point3d v = bb.Center;

                        var edge = breps[k].Edges.First();
                        double dmid = edge.Domain.Mid;
                        Point3d pt = edge.PointAt(dmid);
                        BrepFace face = breps[k].Faces[edge.AdjacentFaces().First()];
                        double u, v;
                        face.ClosestPoint(pt, out u, out v);
                        Vector3d offset_vec = Vector3d.CrossProduct(edge.TangentAt(dmid), face.NormalAt(u, v));
                        pt = new Point3d(pt + offset_vec);

                        Point3d cp;
                        ComponentIndex ci;
                        double s, t;
                        Vector3d n;

                        slicers[i].ClosestPoint(pt, out cp, out ci, out s, out t, 0.0, out n);

                        if (n * new Vector3d(pt - cp) < 0)
                            temp.Add(breps[k]);
                        /*
                        if (!flip && n * new Vector3d(v - cp) < 0)
                            temp.Add(breps[k]);
                        else if (flip && n * new Vector3d(v - cp) > 0)
                            temp.Add(breps[k]);
                        
                    }*/

                    for (int k = 0; k < caps.Length; ++k)
                    {
                        Curve loop = null;
                        Curve[] naked = caps[k].DuplicateNakedEdgeCurves(true, false);
                        Curve[] naked_joined = Curve.JoinCurves(naked);
                        if (naked_joined.Length > 0)
                            loop = naked_joined.First();
                        else
                            continue;
                        //return new Brep[] { caps[k] };
                        //throw new Exception("Failed to get outer loop...");

                        for (int l = 0; l < int_crvs.Length; ++l)
                        {
                            double maxDistance, maxDistanceParamA, maxDistanceParamB;
                            double minDistance, minDistanceParamA, minDistanceParamB;

                            if (!Curve.GetDistancesBetweenCurves(loop, int_crvs[l], 0.01,
                              out maxDistance, out maxDistanceParamA, out maxDistanceParamB,
                              out minDistance, out minDistanceParamA, out minDistanceParamB))
                                continue;

                            if (maxDistance < 1.0)
                            {
                                temp.Add(caps[k]);
                                break;
                            }
                        }
                    }

                    cut = Brep.JoinBreps(temp, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    for (int k = 0; k < cut.Length; ++k)
                    {
                        cut[k].Standardize();
                        cut[k].Compact();
                    }
                    if (cut == null) break;
                }
            }
            /*
            var options = new Rhino.FileIO.SerializationOptions();
            options.RhinoVersion = 5;
            var context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.All, options);
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter(null, context);
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            formatter.Serialize(ms, cut);
            byte[] bytes = ms.ToArray();
            */
            return cut;
        }

        public static void PlaneSlice(this Brep brep, Plane sliceplane, ref List<Curve> curves)
        {
            //brep.Faces.StandardizeFaceSurfaces();
            //BrepSolidOrientation bso = brep.SolidOrientation;

            Curve[] xCrvs;
            Point3d[] xPts;
            bool[] inside;
            Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, sliceplane, 0.001, out xCrvs, out xPts);
            if (xCrvs == null)
                return;

            xCrvs = Rhino.Geometry.Curve.JoinCurves(xCrvs);
            curves = new List<Curve>();
            inside = new bool[xCrvs.Length];
            for (int i = 0; i < xCrvs.Length; ++i)
            {
                //int sign = 1;
                Point3d tpt = xCrvs[i].PointAtNormalizedLength(0.5);
                Vector3d n = brep.ClosestNormal(tpt);
                n = n.ProjectToPlane(sliceplane);
                tpt.Transform(Transform.Translation(n * 0.001));
                PointContainment pcon = xCrvs[i].Contains(tpt, sliceplane, 0.0001);
                if (pcon == PointContainment.Inside)
                {
                    inside[i] = true;
                    //sign = -1;
                }
                else
                {
                    inside[i] = false;
                    //sign = 1;
                }

                curves.Add(xCrvs[i]);
                Curve[] offsets = xCrvs[i].Offset(tpt, sliceplane.ZAxis, 3.0, 0.0001, CurveOffsetCornerStyle.Sharp);
                //Curve[] offsets = xCrvs[i].Offset(sliceplane, 3.0 * sign, 0.001, CurveOffsetCornerStyle.Sharp);
                curves.AddRange(offsets);
            }

            return;
        }


        public static BrepFace BrepFaceFromPoint(this Brep brep, Point3d testPoint)
        {
            BrepFace closest_face = null;
            if (null != brep && testPoint.IsValid)
            {
                double closest_dist = Rhino.RhinoMath.UnsetValue;
                foreach (BrepFace face in brep.Faces)
                {
                    double u, v;
                    if (face.ClosestPoint(testPoint, out u, out v))
                    {
                        Point3d face_point = face.PointAt(u, v);
                        double face_dist = face_point.DistanceTo(testPoint);
                        if (!Rhino.RhinoMath.IsValidDouble(closest_dist) || face_dist < closest_dist)
                        {
                            closest_dist = face_dist;
                            closest_face = face;
                        }
                    }
                }
            }
            return closest_face;
        }

        public static Vector3d ClosestNormal(this Brep brep, Point3d testPoint)
        {
            double u, v;
            BrepFace bf = BrepFaceFromPoint(brep, testPoint);
            bf.ClosestPoint(testPoint, out u, out v);
            return bf.NormalAt(u, v);
        }
    }

    public static class TransformExtensionMethods
    {
        public static Transform FromDoubleArray(double[] arr)
        {
            var transform = new Transform();
            transform.M00 = arr[0];
            transform.M01 = arr[1];
            transform.M02 = arr[2];
            transform.M03 = arr[3];

            transform.M10 = arr[4];
            transform.M11 = arr[5];
            transform.M12 = arr[6];
            transform.M13 = arr[7];

            transform.M20 = arr[8];
            transform.M21 = arr[9];
            transform.M22 = arr[10];
            transform.M23 = arr[11];

            transform.M30 = arr[12];
            transform.M31 = arr[13];
            transform.M32 = arr[14];
            transform.M33 = arr[15];

            return transform;
        }

        public static Transform FromFloatArray(float[] arr)
        {
            var transform = new Transform();
            transform.M00 = arr[0];
            transform.M01 = arr[1];
            transform.M02 = arr[2];
            transform.M03 = arr[3];

            transform.M10 = arr[4];
            transform.M11 = arr[5];
            transform.M12 = arr[6];
            transform.M13 = arr[7];

            transform.M20 = arr[8];
            transform.M21 = arr[9];
            transform.M22 = arr[10];
            transform.M23 = arr[11];

            transform.M30 = arr[12];
            transform.M31 = arr[13];
            transform.M32 = arr[14];
            transform.M33 = arr[15];

            return transform;
        }
    }

    public static class MiscExtensionMethods
    {
        /// <summary>
        /// Modulus which works with negative numbers.
        /// </summary>
        /// <param name="x">Input value.</param>
        /// <param name="m">Domain value.</param>
        /// <returns></returns>
        public static int Modulus(this int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        /// <summary>
        /// Resize 2D array. From: https://stackoverflow.com/a/9059866
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original">Original array to resize.</param>
        /// <param name="x">New width.</param>
        /// <param name="y">New height.</param>
        /// <returns></returns>
        public static T[,] ResizeArray<T>(this T[,] original, int x, int y)
        {
            T[,] newArray = new T[x, y];
            int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
            int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

            for (int i = 0; i < minY; ++i)
                Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

            return newArray;
        }

        /// <summary>
        /// Checks to see if one list contains the other.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static bool ContainsPattern<T>(this List<T> list, List<T> pattern)
        {
            for (int i = 0; i < list.Count - (pattern.Count - 1); i++)
            {
                var match = true;
                for (int j = 0; j < pattern.Count; j++)
                    if (!EqualityComparer<T>.Default.Equals(list[i + j], pattern[j]))
                    {
                        match = false;
                        break;
                    }
                if (match) return true;
            }
            return false;
        }

    }
}
