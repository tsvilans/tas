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

using System;
using System.Collections.Generic;
using System.Linq;

using Rhino.Geometry;

using tas.Core;

namespace tas.Extra
{
    /// <summary>
    /// Bare minimum information required for a free-form element with orientation
    /// frames. This can provide the basis for free-form glulams.
    /// </summary>
    [Serializable]
    public class FreeformElement
    {
        public static double ParameterTolerance = 1.0e-14;

        public Curve Guide;
        public List<GuidePoint> Frames;

        /// <summary>
        /// Construct FreeformElement from guide curve and list of frames.
        /// </summary>
        /// <param name="crv"></param>
        /// <param name="frames"></param>
        public FreeformElement(Curve crv, List<GuidePoint> frames = null)
        {
            if (frames == null)
                Frames = new List<GuidePoint>();
            else
                Frames = frames;

            Guide = crv;
        }

        /// <summary>
        /// Organize the GuideFrames in ascending order by Curve parameter, eliminating any overlaps
        /// between subsequent parameters.
        /// </summary>
        public void Organize()
        {
            // If there are no frames, or only one frame, there is nothing to do.
            if (Frames == null || Frames.Count < 1) return;

            // Reconstruction flag.
            bool reconstruct = false;

            // Sort in ascending order by curve parameter
            Frames.Sort(delegate (GuidePoint g1, GuidePoint g2) { return g1.T.CompareTo(g2.T); });

            // Check for overlaps...
            for (int i = 1; i < Frames.Count; ++i)
            {
                if (Math.Abs(Frames[i].T - Frames[i - 1].T) < ParameterTolerance)
                {
                    // If there are any overlaps, we need to fix them.
                    reconstruct = true;
                    break;
                }
            }

            // If there are overlapping parameters, we need to reconstruct the frame list
            // while omitting these overlaps.
            if (reconstruct)
            {
                bool[] flags = new bool[Frames.Count];
                for (int i = 1; i < Frames.Count; ++i)
                {
                    // Set flags for which elements to remove.
                    if (Math.Abs(Frames[i].T - Frames[i - 1].T) < ParameterTolerance)
                    {
                        flags[i] = true;
                    }
                }

                // Put elements to keep in a new list.
                List<GuidePoint> new_frames = new List<GuidePoint>();
                for (int i = 0; i < Frames.Count; ++i)
                    if (!flags[i])
                        new_frames.Add(Frames[i]);

                // Swap lists.
                Frames = new_frames;
            }
        }

        public Plane GetPlane(GuidePoint gp)
        {
            Point3d pt = Guide.PointAt(gp.T);
            Vector3d z_axis = Guide.TangentAt(gp.T);
            Vector3d x_axis = Vector3d.CrossProduct(z_axis, gp.Direction);
            Vector3d y_axis = Vector3d.CrossProduct(x_axis, z_axis);

            return new Plane(pt, x_axis, y_axis);
        }

        public Plane GetPlane(double t)
        {
            if (Frames == null || Frames.Count < 1) throw new Exception("No frames defined.");
            if (!Guide.Domain.IncludesParameter(t)) throw new Exception("T-value out of bounds.");

            if (t <= Frames.First().T) return GetPlane(new GuidePoint(Frames.First().Direction, t));
            else if (t >= Frames.Last().T) return GetPlane(new GuidePoint(Frames.Last().Direction, t));
            else
            {
                Vector3d v;
                double tt;
                for (int i = 1; i < Frames.Count; ++i)
                {
                    if (t <= Frames[i].T)
                    {
                        tt = (t - Frames[i - 1].T) / (Frames[i].T - Frames[i - 1].T);
                        v = tas.Core.Util.Slerp(Frames[i - 1].Direction, Frames[i].Direction,
                            tt);
                        return GetPlane(new GuidePoint(v, t));
                    }
                }
            }
            throw new Exception("Failed to get plane.");
        }
    }

    /// <summary>
    /// Convenience class for bundling a Plane / Frame with a curve parameter. This
    /// allows us to locate the frame on the curve.
    /// </summary>
    [Serializable]
    public struct GuidePoint
    {
        public GuidePoint(Vector3d dir, double param = 0.0)
        {
            m_direction = dir;
            m_t = param;
        }

        private Vector3d m_direction; // orientation of the Y-axis at that curve parameter
        private double m_t; // non-normalized parameter on Curve

        public Vector3d Direction { get { return m_direction; } set { m_direction = value; } }
        public double T { get { return m_t; } set { m_t = value; } }

    }

    /// <summary>
    /// An abstract representation of an endless stick of lumber (planed rectangular prism), 
    /// with information about the crown location, wood species, and other arbitrary data.
    /// </summary>
    [Serializable]
    public class Stick
    {
        Point2d m_crown;
        string m_species;
        string m_tags;

        /// <summary>
        /// Interface to the stick's crown center (center of log), relative to the center of the 
        /// stick's cross-section.
        /// </summary>
        public Point2d Crown { get { return m_crown; } set { m_crown = value; } }
        /// <summary>
        /// Interface to the wood species tag.
        /// </summary>
        public string Species { get { return m_species; } set { m_species = value; } }
        /// <summary>
        /// Interface to arbitrary string tag.
        /// </summary>
        public string Tags { get { return m_tags; } set { m_tags = value; } }

        public Stick(Point2d crown, string species = "SPF", string tags = "")
        {
            m_crown = crown;
            m_species = species;
            m_tags = tags;
        }

        public Stick(string species = "SPF", string tags = "")
        {
            m_crown = Point2d.Origin;
            m_species = species;
            m_tags = tags;
        }
    }

    /// <summary>
    /// Class to store data about a Glulam's internal structure. This keeps track of lamella counts, sizes, and 
    /// other composition details.
    /// </summary>
    [Serializable]
    public class GlulamData
    {
        // Private sizing data.
        int m_lam_count_x, m_lam_count_y;
        double m_lam_size_x, m_lam_size_y;

        /// <summary>
        /// Interface for LamellaCount in width (X-direction).
        /// </summary>
        public int LamellaCountX { get { return m_lam_count_x; } private set { m_lam_count_x = value; } }
        /// <summary>
        /// Interface for LamellaCount in height (Y-direction).
        /// </summary>
        public int LamellaCountY { get { return m_lam_count_y; } private set { m_lam_count_y = value; } }
        /// <summary>
        /// Interface for LamellaSize in width (X-direction).
        /// </summary>
        public double LamellaSizeX { get { return m_lam_size_x; } set { m_lam_size_x = value; } }
        /// <summary>
        /// Interface for LamellaSize in height (Y-direction).
        /// </summary>
        public double LamellaSizeY { get { return m_lam_size_y; } set { m_lam_size_y = value; } }

        /// <summary>
        /// Placeholder for deeper insight into the lamella composition of the glulam blank, allowing us
        /// to index each lamella and tag it with some arbitrary data.
        /// </summary>
        string[,] m_lam_tags;

        /// <summary>
        /// Guids for each lamella in the glulam.
        /// </summary>
        Stick[,] m_lam_ids;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="lam_count_x">Number of lamellas in X-direction.</param>
        /// <param name="lam_count_y">Number of lamellas in Y-direction.</param>
        /// <param name="lam_size_x">Size of each lamella in X-direction.</param>
        /// <param name="lam_size_y">Size of each lamella in Y-direction.</param>
        public GlulamData(int lam_count_x = 4, int lam_count_y = 4, double lam_size_x = 20.0, double lam_size_y = 20.0)
        {
            
            m_lam_count_x = lam_count_x;
            m_lam_count_y = lam_count_y;
            m_lam_size_x = lam_size_x;
            m_lam_size_y = lam_size_y;

            m_lam_tags = new string[m_lam_count_x, m_lam_count_y];
            m_lam_ids = new Stick[m_lam_count_x, m_lam_count_y];
        }

        /// <summary>
        /// Set string tag attached to lamella at (x, y).
        /// </summary>
        /// <param name="x">X-index.</param>
        /// <param name="y">Y-index.</param>
        /// <param name="tag"></param>
        public void LamellaTag(int x, int y, string tag)
        {
            if (x < 0 || x > m_lam_tags.GetLength(0) || y < 0 || y > m_lam_tags.GetLength(1))
                throw new IndexOutOfRangeException("Lamella index out of range.");

            m_lam_tags[x, y] = tag;
        }

        /// <summary>
        /// Get string tag attached to lamella at (x, y).
        /// </summary>
        /// <param name="x">X-index.</param>
        /// <param name="y">Y-index.</param>
        /// <returns></returns>
        public string LamellaTag(int x, int y)
        {
            if (x < 0 || x > m_lam_tags.GetLength(0) || y < 0 || y > m_lam_tags.GetLength(1))
                throw new IndexOutOfRangeException("Lamella index out of range.");

            return  m_lam_tags[x, y];
        }

        /// <summary>
        /// Set size of lamella matrix.
        /// </summary>
        /// <param name="x">Number of lamellas in X-direction.</param>
        /// <param name="y">Number of lamellas in Y-direction.</param>
        public void SetSize(int x, int y)
        {
            m_lam_count_x = x;
            m_lam_count_y = y;

            m_lam_tags = m_lam_tags.ResizeArray(x, y);
            m_lam_ids = m_lam_ids.ResizeArray(x, y);
        }

    }

    /// <summary>
    /// Base class for new Glulam object.
    /// </summary>
    [Serializable]
    public abstract class GlulamX
    {
        #region Static props
        /// <summary>
        /// Overall tolerance for Glulam operations.
        /// </summary>
        public static double Tolerance = 1.0e-10;

        #endregion

        public Guid ID { get; }
        public virtual Curve Guide { get; set; }
        GlulamData m_data;

        public GlulamData Data { get { return m_data; } set { m_data = value; } }

        /// <summary>
        /// Basic factory method for inferring the type of Glulam based on the
        /// input curve. This is the easiest way of generating glulams.
        /// </summary>
        /// <param name="crv">Input guide curve.</param>
        /// <param name="frames">Optional orientation frames.</param>
        /// <returns></returns>
        public static GlulamX Create(Curve crv, IEnumerable<Plane> frames = null)
        {
            if (crv.IsLinear())
                if (frames == null || !frames.Any())
                {
                    Plane pl;
                    crv.PerpendicularFrameAt(crv.Domain.Min, out pl);
                    return new StraightGlulam(crv, pl.YAxis);
                }
                else
                    return new StraightGlulam(crv, frames.First().YAxis);

            if (crv.IsPlanar(GlulamX.Tolerance))
                return new SingleCurvedGlulam(crv);

            return new FreeformGlulam(crv, frames);
        }

        /// <summary>
        /// Basic factory method for inferring the type of Glulam based on the
        /// input curve. This is the easiest way of generating glulams.
        /// </summary>
        /// <param name="crv">Input guide curve.</param>
        /// <param name="rays">Optional orientation rays.</param>
        /// <returns></returns>
        public static GlulamX Create(Curve crv, IEnumerable<Ray3d> rays = null)
        {
            if (crv.IsLinear())
                if (rays == null || !rays.Any())
                {
                    Plane pl;
                    crv.PerpendicularFrameAt(crv.Domain.Min, out pl);
                    return new StraightGlulam(crv, pl.YAxis);
                }
                else
                    return new StraightGlulam(crv, rays.First().Direction);

            if (crv.IsPlanar(GlulamX.Tolerance))
                return new SingleCurvedGlulam(crv);

            return new FreeformGlulam(crv, rays);
        }

        /// <summary>
        /// Get interpolated frame at curve parameter.
        /// </summary>
        /// <param name="t">Curve parameter to query.</param>
        /// <returns>Interpolated frame at curve parameter t.</returns>
        public abstract Plane GetFrame(double t);

        /// <summary>
        /// Get closest interpolated frame to point.
        /// </summary>
        /// <param name="p">Point to query.</param>
        /// <returns>Interpolated frame closest to point p.</returns>
        public Plane GetFrame(Point3d p)
        {
            double t;
            Guide.ClosestPoint(p, out t);
            return GetFrame(t);
        }

        /// <summary>
        /// Generate bounding mesh of glulam.
        /// </summary>
        /// <returns></returns>
        public abstract Mesh GenerateBoundingMesh(int samples = 20, double extra = 0);

        /// <summary>
        /// Generate bounding Brep of glulam.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public abstract Brep GenerateBoundingBrep(int samples = 20, double extra = 0);

        /// <summary>
        /// Generate centrelines of each lamella in glulam.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public abstract Curve[] GenerateLamellaCurves(int samples = 100);

        /// <summary>
        /// Generate an end-surface of a glulam.
        /// </summary>
        /// <param name="side">0 for surface at the start, 1 for surface at the end</param>
        /// <param name="offset">Offset of end-surface.</param>
        /// <param name="extra_width">Amount to widen end-surface by.</param>
        /// <param name="extra_height">Amount to heighten end-surface by.</param>
        /// <param name="flip">Flip orientation of end-surface.</param>
        /// <returns></returns>
        public Brep GenerateEndSurface(int side, double offset, double extra_width, double extra_height, bool flip = false)
        {
            side = Util.Modulus(side, 2);
            Plane end_plane = GetFrame(side == 0 ? Guide.Domain.Min : Guide.Domain.Max);

            if ((flip && side == 1) || (!flip && side == 0))
                end_plane = end_plane.FlipAroundYAxis();

            end_plane.Origin = end_plane.Origin + end_plane.ZAxis * offset;

            double hwidth = Data.LamellaSizeX * Data.LamellaCountX / 2 * extra_width;
            double hheight = Data.LamellaSizeY * Data.LamellaCountY / 2 * extra_height;

            return Brep.CreateFromCornerPoints(
                end_plane.Origin +  end_plane.XAxis * -hwidth +  end_plane.YAxis * -hheight,
                end_plane.Origin +  end_plane.XAxis * -hwidth +  end_plane.YAxis * hheight,
                end_plane.Origin +  end_plane.XAxis * hwidth +   end_plane.YAxis * hheight,
                end_plane.Origin +  end_plane.XAxis * hwidth +   end_plane.YAxis * -hheight,
                Tolerance);
        }

        /// <summary>
        /// Generate a ruled surface that follows the glulam guide and orientation.
        /// </summary>
        /// <param name="side">0 for a vertical (Y) surface, 1 for a horizontal (X) surface.</param>
        /// <param name="offset">Offset amount from glulam axis.</param>
        /// <param name="width">Width of surface.</param>
        /// <param name="extension">Amount to extend the length of the surface by.</param>
        /// <param name="flip">Flip orientation of surface.</param>
        /// <param name="samples">Number of samples for constructing surface.</param>
        /// <returns></returns>
        public Brep GenerateSideSurface(int side, double offset, double width, double extension = 0.0, bool flip = false, int samples = 100)
        {
            side = Util.Modulus(side, 2);
            double w2 = width / 2;

            Curve c = Guide.DuplicateCurve();
            if (extension > 0.0)
                c = c.Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);

            double[] t = c.DivideByCount(samples, true);
            Curve[] rules = new Curve[t.Length];

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetFrame(t[i]);
                if (side == 0)
                    rules[i] = new Line(p.Origin + p.XAxis * offset + p.YAxis * w2,
                        p.Origin + p.XAxis * offset - p.YAxis * w2).ToNurbsCurve();
                else
                    rules[i] = new Line(p.Origin + p.YAxis * offset + p.XAxis * w2,
                        p.Origin + p.YAxis * offset - p.XAxis * w2).ToNurbsCurve();

            }

            Brep[] loft = Brep.CreateFromLoft(rules, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            if (loft == null || loft.Length < 1) throw new Exception("Glulam::GetSideSurface::Loft failed!");

            Brep brep = loft[0];

            Point3d pt = brep.Faces[0].PointAt(brep.Faces[0].Domain(0).Mid, brep.Faces[0].Domain(1).Mid);
            Vector3d nor = brep.Faces[0].NormalAt(brep.Faces[0].Domain(0).Mid, brep.Faces[0].Domain(1).Mid);

            double ct;
            Guide.ClosestPoint(pt, out ct);
            Vector3d nor2 = Guide.PointAt(ct) - pt;
            nor2.Unitize();

            if (nor2 * nor < 0.0)
            {
                brep.Flip();
            }

            if (flip)
                brep.Flip();

            return brep;
        }

        public abstract Curve OffsetGuide(double x = 0, double y = 0, bool rebuild = false, int rebuild_samples = 100);

    }

    /// <summary>
    /// Class for a straight glulam object. Derived from Glulam.
    /// </summary>
    [Serializable]
    public class StraightGlulam : GlulamX
    {
        Line m_guide;
        Vector3d m_yaxis;

        public StraightGlulam(Curve crv, Vector3d yaxis)
        {
            if (!crv.IsLinear(GlulamX.Tolerance)) throw new Exception("StraightGlulam :: Input curve must be linear.");

            Polyline polyline;
            if (!crv.TryGetPolyline(out polyline)) throw new Exception("StraightGlulam :: Input curve failed.");

            if (polyline.SegmentCount == 1)
                m_guide = polyline.GetSegments()[0];
            else
                throw new Exception("StraightGlulam :: Input curve was polyline. Not sure what to do.");

            if (yaxis.IsParallelTo(m_guide.Direction) != 0)
                throw new Exception("StraightGlulam :: Y-axis vector is parallel to guide curve. This does not make sense.");

            Vector3d xaxis = Vector3d.CrossProduct(yaxis, m_guide.Direction);
            m_yaxis = Vector3d.CrossProduct(m_guide.Direction, xaxis);
            m_yaxis.Unitize();

        }

        public override Curve Guide { get => m_guide.ToNurbsCurve(); }

        public override Brep GenerateBoundingBrep(int samples = 0, double extra = 0)
        {
            double hwidth = Data.LamellaSizeX * Data.LamellaCountX / 2 + extra;
            double hheight = Data.LamellaSizeX * Data.LamellaCountX / 2 + extra;
            Box box = new Box(GetFrame(Guide.Domain.Min), 
                new Interval(-hwidth, hwidth), 
                new Interval(-hheight, hheight), 
                new Interval(0, Guide.GetLength()));

            return Brep.CreateFromBox(box);
        }

        public override Mesh GenerateBoundingMesh(int samples = 0, double extra = 0)
        {
            double x = Data.LamellaSizeX * Data.LamellaCountX;
            double y = Data.LamellaSizeY * Data.LamellaCountY;
            double z = m_guide.Length;
            double hx = x / 2;
            double hy = y / 2;

            Mesh mesh = new Mesh();

            mesh.Vertices.Add(new Point3d(-hx, -hy, 0));    //0
            mesh.Vertices.Add(new Point3d(-hx, hy, 0));     //1
            mesh.Vertices.Add(new Point3d(hx, hy, 0));      //2
            mesh.Vertices.Add(new Point3d(hx, -hy, 0));     //3

            mesh.Vertices.Add(new Point3d(-hx, -hy, z));    //4
            mesh.Vertices.Add(new Point3d(-hx, hy, z));     //5
            mesh.Vertices.Add(new Point3d(hx, hy, z));      //6
            mesh.Vertices.Add(new Point3d(hx, -hy, z));     //7

            mesh.Faces.AddFace(0, 4, 5, 1);     //0
            mesh.Faces.AddFace(1, 5, 6, 2);     //1
            mesh.Faces.AddFace(2, 6, 7, 3);     //2
            mesh.Faces.AddFace(3, 7, 4, 0);     //3

            mesh.Faces.AddFace(0, 1, 2, 3);     //4
            mesh.Faces.AddFace(4, 5, 6, 7);     //5

            Plane p = new Plane(m_guide.From, Vector3d.CrossProduct(m_guide.Direction, m_yaxis), m_yaxis);
            mesh.Transform(Transform.PlaneToPlane(Plane.WorldXY, p));

            return mesh;
        }

        public override Curve[] GenerateLamellaCurves(int samples = 100)
        {
            throw new NotImplementedException();
        }

        public override Plane GetFrame(double t) => 
            new Plane(m_guide.PointAt(t), Vector3d.CrossProduct(m_yaxis, m_guide.Direction), m_yaxis);

        public override Curve OffsetGuide(double x = 0, double y = 0, bool rebuild = false, int rebuild_samples = 100)
        {
            Vector3d xaxis = Vector3d.CrossProduct(m_yaxis, m_guide.Direction);
            return new Line(m_guide.From + xaxis * x + m_yaxis * y, m_guide.To + xaxis * x + m_yaxis * y).ToNurbsCurve();
        }
    }

    /// <summary>
    /// Class for a free-form glulam object, which can be double-curved
    /// and twisted. Derived from Glulam.
    /// </summary>
    [Serializable]
    public class FreeformGlulam : GlulamX
    {
        FreeformElement m_guide;

        public FreeformGlulam(Curve guide, IEnumerable<Plane> frames = null)
        {
            m_guide = new FreeformElement(guide);

            if (frames == null || !frames.Any())
            {
                Plane pl;
                guide.PerpendicularFrameAt(guide.Domain.Min, out pl);
                m_guide.Frames.Add(new GuidePoint(pl.YAxis, guide.Domain.Min));
            }
            else
            {
                Vector3d xaxis, zaxis, yaxis;
                double t;
                foreach (Plane pl in frames)
                { 
                    guide.ClosestPoint(pl.Origin, out t);
                    zaxis = guide.TangentAt(t);
                    xaxis = Vector3d.CrossProduct(pl.YAxis, zaxis);
                    yaxis = Vector3d.CrossProduct(zaxis, xaxis);
                    yaxis.Unitize();
                    m_guide.Frames.Add(new GuidePoint(yaxis, t));
                }
            }

            m_guide.Organize();
        }

        public FreeformGlulam(Curve guide, IEnumerable<Ray3d> frames = null)
        {
            m_guide = new FreeformElement(guide);

            if (frames == null || !frames.Any())
            {
                Plane pl;
                guide.PerpendicularFrameAt(guide.Domain.Min, out pl);
                m_guide.Frames.Add(new GuidePoint(pl.YAxis, guide.Domain.Min));
            }
            else
            {
                Vector3d xaxis, zaxis, yaxis;
                double t;
                foreach (Ray3d r in frames)
                {
                    guide.ClosestPoint(r.Position, out t);
                    zaxis = guide.TangentAt(t);
                    xaxis = Vector3d.CrossProduct(r.Direction, zaxis);
                    yaxis = Vector3d.CrossProduct(zaxis, xaxis);
                    yaxis.Unitize();
                    m_guide.Frames.Add(new GuidePoint(yaxis, t));
                }
            }

            m_guide.Organize();
        }

        public override Curve Guide { get => m_guide.Guide; }

        public override Brep GenerateBoundingBrep(int samples = 20, double extra = 0)
        {
            throw new NotImplementedException();
        }

        public override Mesh GenerateBoundingMesh(int samples = 30, double extra = 0)
        {
            throw new NotImplementedException();
        }

        public override Curve[] GenerateLamellaCurves(int samples = 100)
        {
            throw new NotImplementedException();
        }

        public override Plane GetFrame(double t)
        {
            return m_guide.GetPlane(t);
        }

        public override Curve OffsetGuide(double x = 0, double y = 0, bool rebuild = false, int rebuild_samples = 100)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class for a single-curved glulam object, which can only be curved
    /// in one plane (arc or planar spline). Derived from Glulam.
    /// </summary>
    [Serializable]
    public class SingleCurvedGlulam : GlulamX
    {
        Vector3d m_normal;
        Curve m_guide;

        public SingleCurvedGlulam(Curve crv)
        {
            if (!crv.IsPlanar(GlulamX.Tolerance))
                throw new Exception("SingleCurvedGlulam :: Input curve is not planar in specified tolerance.");

            m_guide = crv;

            Plane pl;
            if (!crv.TryGetPlane(out pl))
                throw new Exception("SingleCurvedGlulam :: Failed to get curve plane.");

            m_normal = pl.ZAxis;
        }

        public override Curve Guide { get => m_guide; }

        public override Brep GenerateBoundingBrep(int samples = 20, double extra = 0)
        {
            throw new NotImplementedException();
        }

        public override Mesh GenerateBoundingMesh(int samples = 30, double extra = 0)
        {
            throw new NotImplementedException();
        }

        public override Curve[] GenerateLamellaCurves(int samples = 100)
        {
            throw new NotImplementedException();
        }

        public override Plane GetFrame(double t)
        {
            Point3d pt = m_guide.PointAt(t);
            Vector3d z_axis = m_guide.TangentAt(t);
            Vector3d y_axis = Vector3d.CrossProduct(z_axis, m_normal);

            return new Plane(pt, m_normal, y_axis);
        }

        public override Curve OffsetGuide(double x = 0, double y = 0, bool rebuild = false, int rebuild_samples = 100)
        {
            throw new NotImplementedException();
        }
    }
}
