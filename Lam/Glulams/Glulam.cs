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
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using tas.Core;
using tas.Core.Util;

using Rhino.Collections;

namespace tas.Lam
{
    public abstract class Glulam
    {
        public static double RadiusMultiplier = 200.0; // This is the Eurocode 5 formula: lamella thickness cannot exceed 1/200th of the curvature radius.
        public static int CurvatureSamples = 100; // Number of samples to samples curvature at.
        public static double RadiusTolerance = 0.00001; // For curvature calculations: curvature radius and lamella thickness cannot exceed this


        #region Static variables and methods
        protected static double Tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        protected static double OverlapTolerance = 1.0 * Rhino.RhinoMath.UnitScale(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem, Rhino.UnitSystem.Millimeters);
        protected static double AngleTolerance = Math.Cos(2);


        /// <summary>
        /// Glulam factory methods.
        /// </summary>
        /// <param name="curve">Input curve.</param>
        /// <param name="planes">Input orientation planes.</param>
        /// <param name="data">Input glulam data.</param>
        /// <returns>New glulam.</returns>
        static public Glulam CreateGlulam(Curve curve, Plane[] planes = null, GlulamData data = null)
        {
            if (data == null) data = GlulamData.FromCurveLimits(curve);


            Glulam glulam;
            if (planes == null || planes.Length < 1)
            // if there are no planes defined, create defaults
            {
                Plane p;
                if (curve.IsLinear(Tolerance))
                {
                    curve.PerpendicularFrameAt(curve.Domain.Min, out p);
                    glulam = new StraightGlulam(curve, new Plane[] { p });
                }
                else if (curve.IsPlanar(Tolerance))
                {
                    curve.TryGetPlane(out p, Tolerance);
                    glulam = new SingleCurvedGlulam(curve, new Plane[]
                    {
                        new Plane(
                            curve.PointAtStart,
                            p.ZAxis,
                            Vector3d.CrossProduct(
                                curve.TangentAtStart, p.ZAxis
                                )
                            ),
                        new Plane(
                            curve.PointAtEnd,
                            p.ZAxis,
                            Vector3d.CrossProduct(
                                curve.TangentAtEnd, p.ZAxis 
                                )
                            )

                    });
                }
                else
                {
                    Plane start, end;
                    curve.PerpendicularFrameAt(curve.Domain.Min, out start);
                    curve.PerpendicularFrameAt(curve.Domain.Max, out end);
                    glulam = new DoubleCurvedGlulam(curve, new Plane[] { start, end });
                }
            }
            else // if there are planes defined
            {
                if (curve.IsLinear(Tolerance))
                {
                    if (planes.Length == 1)
                        glulam = new StraightGlulam(curve, planes);
                    else
                    {
                        glulam = new StraightGlulam(curve, planes, true);
                        // glulam = new StraightGlulamWithTwist(curve, planes);
                        Console.WriteLine("Not implemented...");
                    }
                }
                else if (curve.IsPlanar(Tolerance))
                {
                    Plane crv_plane;
                    curve.TryGetPlane(out crv_plane);

                    /*
                     * Are all the planes perpendicular to the curve normal?
                     *    Yes: basic SC Glulam
                     * Are all the planes consistently aligned from the curve normal?
                     *    Yes: SC Glulam with rotated cross-section
                     * SC Glulam with twisting
                     */

                    bool HasTwist = false;

                    foreach (Plane p in planes)
                    {
                        if (Math.Abs(p.XAxis * crv_plane.ZAxis) > Tolerance)
                        {
                            HasTwist = true;
                        }
                    }
                    if (HasTwist)
                        glulam = new DoubleCurvedGlulam(curve, planes);
                    else
                    {

                        Plane first = new Plane(curve.PointAtStart, crv_plane.ZAxis, Vector3d.CrossProduct(curve.TangentAtStart, crv_plane.ZAxis));
                        Plane last = new Plane(curve.PointAtEnd, crv_plane.ZAxis, Vector3d.CrossProduct(curve.TangentAtEnd, crv_plane.ZAxis));
                        glulam = new SingleCurvedGlulam(curve, new Plane[] { first, last });
                    }
                }
                else
                {
                    Plane temp;
                    double t;
                    bool Twisted = false;
                    curve.PerpendicularFrameAt(curve.Domain.Min, out temp);

                    double Angle = Vector3d.VectorAngle(planes[0].YAxis, temp.YAxis);

                    for (int i = 0; i < planes.Length; ++i)
                    {
                        curve.ClosestPoint(planes[i].Origin, out t);
                        curve.PerpendicularFrameAt(t, out temp);

                        if (Math.Abs(Vector3d.VectorAngle(planes[0].YAxis, temp.YAxis) - Angle) > AngleTolerance)
                        {
                            // Twisting Glulam
                            Twisted = true;
                            break;
                        }
                    }
                    /*
                     * Are all the planes consistently aligned from some plane?
                     *    Yes: DC Glulam with constant cross-section
                     * Are all the planes at a consistent angle from the perpendicular frame of the curve?
                     *    Yes: DC Glulam with minimal twisting
                     * DC Glulam with twisting
                     */

                    if (Twisted)
                        // TODO: differentiate between DC Glulam with minimal twist, and DC Glulam with twist
                        glulam = new DoubleCurvedGlulam(curve, planes);
                    else
                        glulam = new DoubleCurvedGlulam(curve, planes);
                }
            }

            glulam.ValidateFrames();

            if (glulam is DoubleCurvedGlulam)
            {
                if (data.NumHeight < 2)
                {
                    data.NumHeight = 2;
                    data.LamHeight /= 2;
                }

                if (data.NumWidth < 2)
                {
                    data.NumWidth = 2;
                    data.LamWidth /= 2;
                }
            }
            else if (glulam is SingleCurvedGlulam)
            {
                if (data.NumHeight < 2)
                {
                    data.NumHeight = 2;
                    data.LamHeight /= 2;
                }
            }

            glulam.Data = data;

            return glulam;
        }

        /// <summary>
        /// Create glulam with frames that are aligned with a Brep. The input curve does not
        /// necessarily have to lie on the Brep.
        /// </summary>
        /// <param name="curve">Input centreline of the glulam.</param>
        /// <param name="brep">Brep to align the glulam orientation to.</param>
        /// <param name="num_samples">Number of orientation frames to use for alignment.</param>
        /// <returns>New Glulam oriented to the brep.</returns>
        static public Glulam CreateGlulamNormalToSurface(Curve curve, Brep brep, int num_samples = 20, GlulamData data = null)
        {
            Plane[] frames = tas.Core.Util.Misc.FramesNormalToSurface(curve, brep, num_samples);
            return Glulam.CreateGlulam(curve, frames, data);
        }

        /// <summary>
        /// Create glulam with frames that are aligned with a Surface. The input curve does not
        /// necessarily have to lie on the Surface.
        /// </summary>
        /// <param name="curve">Input centreline of the Glulam.</param>
        /// <param name="srf">Surface to align the Glulam orientation to.</param>
        /// <param name="num_samples">Number of orientation frames to use for alignment.</param>
        /// <returns>New Glulam oriented to the Surface.</returns>
        static public Glulam CreateGlulamNormalToSurface_old(Curve curve, Brep brep, int num_samples = 20, GlulamData data = null)
        {
            double[] t = curve.DivideByCount(num_samples, true);
            List<Plane> planes = new List<Plane>();
            double u, v;
            Vector3d xaxis, yaxis, zaxis;
            ComponentIndex ci;
            Point3d pt;

            for (int i = 0; i < t.Length; ++i)
            {
                brep.ClosestPoint(curve.PointAt(t[i]), out pt, out ci, out u, out v, 0, out yaxis);
                zaxis = curve.TangentAt(t[i]);
                xaxis = Vector3d.CrossProduct(zaxis, yaxis);

                planes.Add(new Plane(curve.PointAt(t[i]), xaxis, yaxis));
            }
            return Glulam.CreateGlulam(curve, planes.ToArray(), data);
        }

        /// <summary>
        /// Create a Glulam from arbitrary geometry and a guide curve. The curve describes the fibre direction of the Glulam. This will
        /// create a Glulam which completely envelops the input geometry and whose centreline is offset from the input guide curve, to 
        /// preserve the desired fibre orientation. 
        /// </summary>
        /// <param name="curve">Guide curve to direct the Glulam.</param>
        /// <param name="beam">Beam geometry as Mesh.</param>
        /// <param name="extra">Extra material tolerance to leave on Glulam (the width and height of the Glulam will be 
        /// increased by this much).</param>
        /// <returns>A new Glulam which envelops the input beam geometry, plus an extra tolerance as defined above.</returns>
        static public Glulam CreateGlulamFromBeamGeometry(Curve curve, Mesh beam, out double true_width, out double true_height, out double true_length, double extra = 0.0)
        {
            double t, l;
            Plane cp = Plane.Unset;
            Plane cpp;
            Polyline ch;
            Mesh m = new Mesh();

            List<Plane> frames = new List<Plane>();
            double[] tt = curve.DivideByCount(20, true);

            if (curve.IsLinear())
            {
                m = beam.DuplicateMesh();
                curve.PerpendicularFrameAt(curve.Domain.Min, out cp);
                m.Transform(Rhino.Geometry.Transform.PlaneToPlane(cp, Plane.WorldXY));
                m.Faces.Clear();

                Plane twist = Plane.WorldXY;
                m = m.FitToAxes(Plane.WorldXY, out ch, ref twist);

                double angle = Vector3d.VectorAngle(Vector3d.XAxis, twist.XAxis);
                int sign = Math.Sign(twist.YAxis * Vector3d.XAxis);

                cp.Transform(Rhino.Geometry.Transform.Rotation(angle * sign, cp.ZAxis, cp.Origin));
                frames.Add(cp);
            }
            else if (curve.TryGetPlane(out cpp, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance))
            {
                for (int i = 0; i < tt.Length; ++i)
                {
                    Vector3d xaxis = Vector3d.CrossProduct(cpp.ZAxis, curve.TangentAt(tt[i]));
                    cp = new Plane(curve.PointAt(tt[i]), xaxis, cpp.ZAxis);
                    frames.Add(cp);
                }
                for (int i = 0; i < beam.Vertices.Count; ++i)
                {
                    Point3d p = new Point3d(beam.Vertices[i]);
                    curve.ClosestPoint(p, out t);
                    l = curve.GetLength(new Interval(curve.Domain.Min, t));
                    Vector3d xaxis = Vector3d.CrossProduct(cpp.ZAxis, curve.TangentAt(t));
                    cp = new Plane(curve.PointAt(t), xaxis, cpp.ZAxis);
                    p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cp, Plane.WorldXY));
                    p.Z = l;
                    m.Vertices.Add(p);
                }
            }
            else
            {
                for (int i = 0; i < beam.Vertices.Count; ++i)
                {
                    Point3d p = new Point3d(beam.Vertices[i]);
                    curve.ClosestPoint(p, out t);
                    l = curve.GetLength(new Interval(curve.Domain.Min, t));
                    curve.PerpendicularFrameAt(t, out cp);
                    p.Transform(Rhino.Geometry.Transform.PlaneToPlane(cp, Plane.WorldXY));
                    p.Z = l;

                    m.Vertices.Add(p);
                }

                Plane twist = Plane.WorldXY;
                m = m.FitToAxes(Plane.WorldXY, out ch, ref twist);
                double angle = Vector3d.VectorAngle(Vector3d.XAxis, twist.XAxis);
                int sign = Math.Sign(twist.YAxis * Vector3d.XAxis);

                for (int i = 0; i < tt.Length; ++i)
                {
                    curve.PerpendicularFrameAt(tt[i], out cp);
                    cp.Transform(Rhino.Geometry.Transform.Rotation(angle * sign, cp.ZAxis, cp.Origin));
                    frames.Add(cp);
                }
            }

            m.Faces.AddFaces(beam.Faces);
            m.FaceNormals.ComputeFaceNormals();

            BoundingBox bb = m.GetBoundingBox(true);

            double offsetX = bb.Center.X;
            double offsetY = bb.Center.Y;

            Brep bb2 = bb.ToBrep();
            bb2.Transform(Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, cp));

            true_width = bb.Max.X - bb.Min.X + extra;
            true_height = bb.Max.Y - bb.Min.Y + extra;

            // Now we create the glulam...

            //tasTools.Lam.Glulam glulam = tasTools.Lam.Glulam.CreateGlulam(curve, frames.ToArray());
            Beam temp_beam = new Beam(curve, frames.ToArray());
            temp_beam.Samples = (int)(curve.GetLength() / GlulamData.DefaultSampleDistance);
            Curve new_curve = temp_beam.CreateOffsetCurve(offsetX, offsetY, true);
            new_curve = new_curve.Extend(CurveEnd.Both, 5.0 + extra, CurveExtensionStyle.Smooth);

            GlulamData data = GlulamData.FromCurveLimits(new_curve, frames.ToArray());

            if (new_curve.IsPlanar())
            {
                data.NumHeight = 1;
                data.LamHeight = Math.Ceiling(true_height);
                data.NumWidth = (int)(Math.Ceiling(true_width / data.LamWidth));

            }
            else if (new_curve.IsLinear())
            {
                data.NumHeight = 1;
                data.NumWidth = 1;
                data.LamHeight = Math.Ceiling(true_height);
                data.LamWidth = Math.Ceiling(true_width);
            }
            else
            {
                data.NumHeight = (int)(Math.Ceiling(true_height / data.LamHeight));
                data.NumWidth = (int)(Math.Ceiling(true_width / data.LamWidth));
            }


            Glulam glulam = tas.Lam.Glulam.CreateGlulam(new_curve, frames.ToArray(), data);
            /*
            tt = glulam.Centreline.DivideByCount(100, true);
            double maxK = 0.0;

            int index = 0;
            Vector3d kvec = Vector3d.Unset;
            Vector3d temp;

            for (int i = 0; i < tt.Length; ++i)
            {
                temp = glulam.Centreline.CurvatureAt(tt[i]);
                if (temp.Length > maxK)
                {
                    index = i;
                    kvec = temp;
                    maxK = temp.Length;
                }
            }
            Plane frame = glulam.GetPlane(tt[index]);

            double min_lam_width = 1.0;
            double min_lam_height = 1.0;
            double max_lam_width = (double)((int)Math.Ceiling(true_width / 10.0) * 10.0);
            double max_lam_height = (double)((int)Math.Ceiling(true_height / 10.0) * 10.0);

            double lam_width = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.XAxis) * 200), max_lam_width));
            double lam_height = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.YAxis) * 200), max_lam_height));

            if (lam_width == 0.0) lam_width = max_lam_width;
            if (lam_height == 0.0) lam_height = max_lam_height;

            glulam.Data.LamHeight = lam_height;
            glulam.Data.LamWidth = lam_width;
            glulam.Data.NumHeight = (int)(Math.Ceiling(true_height / lam_height));
            glulam.Data.NumWidth = (int)(Math.Ceiling(true_width / lam_width));

            //double new_lam_height, new_lam_width;

            if (glulam.Data.NumHeight * glulam.Data.LamHeight - true_height > 20.0)
                glulam.Data.LamHeight = Math.Ceiling((true_height + 10.0) / glulam.Data.NumHeight);
            if (glulam.Data.NumWidth * glulam.Data.LamWidth - true_width > 20.0)
                glulam.Data.LamWidth = Math.Ceiling((true_width + 10.0) / glulam.Data.NumWidth);
            */
            true_length = new_curve.GetLength();
            
            return glulam;
        }

        static public Glulam CreateGlulamFromBeamGeometry2(Curve curve, Mesh beam, out double true_width, out double true_height, out double true_length, double extra = 0.0)
        {

            Mesh mm = curve.MapToCurveSpace(beam);

            BoundingBox bb = mm.GetBoundingBox(true);

            double x = bb.Center.X;
            double y = bb.Center.Y;

            double tmin, tmax;
            curve.LengthParameter(bb.Min.Z, out tmin);
            curve.LengthParameter(bb.Max.Z, out tmax);

            Plane twist = Plane.WorldXY;
            Polyline ch;
            mm = mm.FitToAxes(Plane.WorldXY, out ch, ref twist);

            bb = mm.GetBoundingBox(true);
            double dx = bb.Max.X - bb.Min.X;
            double dy = bb.Max.Y - bb.Min.Y;

            Plane cp;
            curve.PerpendicularFrameAt(tmin, out cp);


            double angle = Vector3d.VectorAngle(Vector3d.XAxis, twist.XAxis);
            int sign = Math.Sign(twist.YAxis * Vector3d.XAxis);


            Curve[] segments = curve.Split(new double[] { tmin, tmax });
            if (segments.Length == 3)
                curve = segments[1];
            else
                curve = segments[0];

            Beam b = new Beam(curve, new Plane[] { cp });

            //curve = b.CreateOffsetCurve(-x, -y);
            curve = b.CreateOffsetCurve(x, y);
            curve = curve.Extend(CurveEnd.Both, extra, CurveExtensionStyle.Smooth);

            cp.Transform(Rhino.Geometry.Transform.Rotation(angle * sign, cp.ZAxis, cp.Origin));

            GlulamData data = GlulamData.FromCurveLimits(curve, dx + extra * 2, dy + extra * 2, new Plane[] { cp });

            true_length = curve.GetLength();
            true_width = dx + extra * 2;
            true_height = dy + extra * 2;

            return Glulam.CreateGlulam(curve, new Plane[] { cp }, data);
        }

        static public Glulam CreateGlulamFromBeamGeometry(Curve curve, Mesh beam, double extra = 0.0)
        {
            double w, h, l;
            return CreateGlulamFromBeamGeometry2(curve, beam, out w, out h, out l, extra);
        }

        static public Brep GetGlulamBisector(Glulam g1, Glulam g2, double extension = 50.0, bool normalized = false)
        {
            Glulam[] g = new Glulam[2] { g1, g2 };

            int shorter = g[0].Centreline.GetLength() > g[1].Centreline.GetLength() ? 1 : 0;
            int longer = 1 - shorter;

            double length = g[shorter].Centreline.GetLength();
            double[] t = g[shorter].Centreline.DivideByCount(10, true);
            double t2;

            List<Point3d>[] edge_pts = new List<Point3d>[2];
            for (int i = 0; i < 2; ++i)
                edge_pts[i] = new List<Point3d>();

            double tl;
            for (int i = 0; i < t.Length; ++i)
            {
                tl = g[shorter].Centreline.GetLength(new Interval(g[shorter].Centreline.Domain.Min, t[i]));

                g[longer].Centreline.LengthParameter(tl, out t2);
                //g[longer].Centreline.ClosestPoint(g[shorter].Centreline.PointAt(t[i]), out t2);
                Plane p = Interpolation.InterpolatePlanes2(g[shorter].GetPlane(t[i]), g[longer].GetPlane(t2), 0.5);

                edge_pts[0].Add(p.Origin + p.YAxis * extension);
                edge_pts[1].Add(p.Origin - p.YAxis * extension);
            }

            Curve[] crvs = new Curve[4];
            for (int i = 0; i < 2; ++i)
            {
                crvs[i] = Curve.CreateControlPointCurve(edge_pts[i], 2);
                crvs[i] = crvs[i].Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);
                //crvs[i] = crvs[i].Rebuild(10, 3, true);
            }

            crvs[2] = (new Line(crvs[0].PointAtStart, crvs[1].PointAtStart)).ToNurbsCurve();
            crvs[3] = (new Line(crvs[0].PointAtEnd, crvs[1].PointAtEnd)).ToNurbsCurve();

            Brep brep = Brep.CreateEdgeSurface(crvs);
            //Brep[] brep = Brep.CreateFromLoft(crvs, crvs[0].PointAtStart, crvs[0].PointAtStart,
            //  LoftType.Straight, false);

            return brep;
        }

        #endregion

        protected Guid ID;
        /*protected*/public List<Tuple<double, Plane>> Frames;

        public Curve Centreline { get; protected set;}
        public GlulamData Data;

        // Protected
        protected Point3d[] m_section_corners = null; // Cached section corners

        protected Glulam()
        {
            ID = Guid.NewGuid();
        }

        public abstract void CalculateLamellaSizes(double width, double height);

        public List<Point3d> DiscretizeCentreline(bool adaptive = true)
        {
            if (adaptive)
            {
                var pCurve = Centreline.ToPolyline(Glulam.Tolerance, Glulam.AngleTolerance, 0.0, 0.0);
                return pCurve.ToPolyline().ToList();
            }

            var tt = Centreline.DivideByCount(Data.Samples, true);
            return tt.Select(x => Centreline.PointAt(x)).ToList();
        }

        public virtual Mesh GetBoundingMesh(double offset = 0.0, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            return new Mesh();
        }

        public virtual Brep GetBoundingBrep(double offset = 0.0)
        {
            return new Brep();
        }

        public virtual List<Curve> GetLamellaCurves()
        {
            return new List<Curve>();
        }

        public virtual List<Mesh> GetLamellaMeshes()
        {
            return new List<Mesh>();
        }

        public virtual List<Brep> GetLamellaBreps()
        {
            return new List<Brep>();
        }

        public virtual double GetMaxCurvature(ref double width, ref double height)
        {
            return 0.0;
        }

        public Dictionary<string, object> GetProperties()
        {
            Dictionary<string, object> props = new Dictionary<string, object>();

            props.Add("id", ID);
            props.Add("centreline", Centreline);
            props.Add("width", Width());
            props.Add("height", Height());
            props.Add("length", Centreline.GetLength());
            props.Add("lamella_width", Data.LamWidth);
            props.Add("lamella_height", Data.LamHeight);
            props.Add("lamella_count_width", Data.NumWidth);
            props.Add("lamella_count_height", Data.NumHeight);
            props.Add("volume", GetVolume());
            props.Add("samples", Data.Samples);
            props.Add("frames", GetAllPlanes());

            double max_kw = 0.0, max_kh = 0.0;
            props.Add("max_curvature", GetMaxCurvature(ref max_kw, ref max_kh));
            props.Add("max_curvature_width", max_kw);
            props.Add("max_curvature_height", max_kh);
            props.Add("type", ToString());
            props.Add("type_id", (int)Type());
            
            return props;
        }

        public abstract void GenerateCrossSectionPlanes(int N, double extension, out Plane[] planes, out double[] t, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR);

        public ArchivableDictionary GetArchivableDictionary()
        {
            ArchivableDictionary ad = new ArchivableDictionary();

            ad.Set("id", ID);
            ad.Set("centreline", Centreline);
            ad.Set("width", Width());
            ad.Set("height", Height());
            ad.Set("length", Centreline.GetLength());
            ad.Set("lamella_width", Data.LamWidth);
            ad.Set("lamella_height", Data.LamHeight);
            ad.Set("lamella_count_width", Data.NumWidth);
            ad.Set("lamella_count_height", Data.NumHeight);
            ad.Set("volume", GetVolume());
            ad.Set("samples", Data.Samples);

            var planes = GetAllPlanes();
            ArchivableDictionary pd = new ArchivableDictionary();

            for (int i = 0; i < planes.Length; ++i)
            {
                pd.Set(string.Format("Frame_{0}", i), planes[i]);
            }
            ad.Set("frames", pd);

            double max_kw = 0.0, max_kh = 0.0;
            ad.Set("max_curvature", GetMaxCurvature(ref max_kw, ref max_kh));
            ad.Set("max_curvature_width", max_kw);
            ad.Set("max_curvature_height", max_kh);
            ad.Set("type", ToString());
            ad.Set("type_id", (int)Type());

            return ad;
        }

        public virtual double GetVolume(bool accurate = false)
        {
            if (accurate)
            {
                Rhino.Geometry.VolumeMassProperties vmp = VolumeMassProperties.Compute(GetBoundingBrep());
                return vmp.Volume;
            }
            return Centreline.GetLength() * Width() * Height();
        }

        public virtual GlulamType Type()
        {
            return GlulamType.Straight;
        }

        public virtual Plane GetPlane(double t)
        {
            return Plane.Unset;
        }

        public Plane[] GetAllPlanes()
        {
            return Frames.Select(x => x.Item2).ToArray();
        }

        public void SortFrames()
        {
            Frames.Sort((x, y) => x.Item1.CompareTo(y.Item1));
        }

        public void ValidateFrames()
        {
            Frames = Frames.Where(x => x.Item2.IsValid).ToList();
        }

        public Plane GetPlane(Point3d p)
        {
            double t;
            Centreline.ClosestPoint(p, out t);
            return GetPlane(t);
        }

        public virtual void Transform(Transform x)
        {
            return;
        }

        public override bool Equals(object obj)
        {
            if (obj is Glulam && (obj as Glulam).ID == ID)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return "Glulam";
        }

        /// <summary>
        /// Reduce twisting of glulam frames. This relaxes twisting between consecutive frames.
        /// </summary>
        /// <param name="factor">Factor to relax by.</param>
        /// <param name="start_with_first">True to start at the centreline start, false to start at centreline end.</param>
        public abstract void ReduceTwist(double factor, bool start_with_first = true);

        /// <summary>
        /// Reverse direction of glulam.
        /// </summary>
        public void Reverse()
        {
            Point3d[] temp_points = Frames.Select(x => x.Item2.Origin).ToArray();
            Centreline.Reverse();

            double t;
            for (int i = 0; i < Frames.Count; ++i)
            {
                Centreline.ClosestPoint(temp_points[i], out t);
                Plane p = Frames[i].Item2.FlipAroundYAxis();
                Frames[i] = new Tuple<double, Plane>(t, p);
            }

            Frames.Reverse();
        }

        /// <summary>
        /// Checks the glulam to see if its lamella sizes are appropriate for its curvature.
        /// </summary>
        /// <returns>True if glulam is within curvature limits.</returns>
        public bool InKLimits()
        {
            double t;
            return InKLimits(out t);
        }

        /// <summary>
        /// Checks the glulam to see if its lamella sizes are appropriate for its curvature.
        /// </summary>
        /// <param name="param">Parameter with maximum curvature.</param>
        /// <returns>True if glulam is within curvature limits.</returns>
        public bool InKLimits(out double param)
        {
            double[] t = Centreline.DivideByCount(CurvatureSamples, false);
            double max_k = 0.0;
            int index = 0;
            double temp;
            for (int i = 0; i < t.Length; ++i)
            {
                temp = Centreline.CurvatureAt(t[i]).Length;
                if (temp > max_k)
                {
                    max_k = temp;
                    index = i;
                }
            }

            param = t[index];

            double ratio = (1 / max_k) / RadiusMultiplier;
            if (Math.Abs(ratio - Data.LamHeight) > RadiusTolerance || Math.Abs(ratio - Data.LamWidth) > RadiusTolerance)
                return false;
            return true;
        }

        /// <summary>
        /// Get total width of glulam.
        /// </summary>
        /// <returns>Total width of glulam.</returns>
        public double Width()
        {
            return Data.LamWidth * Data.NumWidth;
        }

        /// <summary>
        /// Get total height of glulam.
        /// </summary>
        /// <returns>Total height of glulam.</returns>
        public double Height()
        {
            return Data.LamHeight * Data.NumHeight;
        }

        /// <summary>
        /// Checks the glulam to see if its lamella sizes are appropriate for its curvature.
        /// </summary>
        /// <param name="width">True if the lamella width is OK.</param>
        /// <param name="height">True if the lamella height is OK.</param>
        /// <returns>True if both dimensions are OK.</returns>
        public virtual bool InKLimitsComponent(out bool width, out bool height)
        {
            width = height = false;
            double[] t = Centreline.DivideByCount(CurvatureSamples, false);
            double max_kw = 0.0, max_kh = 0.0;
            Plane temp;
            Vector3d k;
            for (int i = 0; i < t.Length; ++i)
            {
                temp = GetPlane(t[i]);

                k = Centreline.CurvatureAt(t[i]);
                max_kw = Math.Max(max_kw, Math.Abs(k * temp.XAxis));
                max_kh = Math.Max(max_kh, Math.Abs(k * temp.YAxis));
            }

            double rw = (1 / max_kw) / RadiusMultiplier;
            double rh = (1 / max_kh) / RadiusMultiplier;

            if (rw - Data.LamWidth > -RadiusTolerance || double.IsInfinity(1 / max_kw))
                width = true;
            if (rh - Data.LamHeight > -RadiusTolerance || double.IsInfinity(1 / max_kh))
                height = true;

            return width && height;
        }

        /// <summary>
        /// Cleans up frame list to get rid of frames that share the same parameter.
        /// </summary>
        public void RemoveDuplicateFrames()
        {
            if (Frames.Count < 2) return;
            List<Tuple<double, Plane>> NewFrames = new List<Tuple<double, Plane>>();
            bool[] Flags = new bool[Frames.Count];

            for (int i = 0; i < Frames.Count - 1; ++i)
            {
                if (Flags[i]) continue;
                for (int j = i + 1; j < Frames.Count; ++j)
                {
                    if (Flags[j]) continue;
                    if (Math.Abs(Frames[i].Item1 - Frames[j].Item1) < 0.001)
                        Flags[j] = true;
                }
            }

            for (int i = 0; i < Flags.Length; ++i)
            {
                if (!Flags[i]) NewFrames.Add(Frames[i]);
            }

            Frames = NewFrames;
        }

        /// <summary>
        /// Join a glulam onto another one. Returns null if join is not possible.
        /// </summary>
        /// <param name="glulam"></param>
        /// <returns></returns>
        public Glulam Join(Glulam glulam)
        {
            Rhino.Geometry.Intersect.CurveIntersections ci;
            ci = Rhino.Geometry.Intersect.Intersection.CurveCurve(Centreline, glulam.Centreline, Tolerance, OverlapTolerance);
            if (ci.Count != 1) return null;
            if (ci[0].IsOverlap) return null;
            if (Math.Abs(Centreline.TangentAt(ci[0].ParameterA) * glulam.Centreline.TangentAt(ci[0].ParameterB)) < AngleTolerance) return null;

            Curve[] NewCentreline = Curve.JoinCurves(new Curve[] { Centreline, glulam.Centreline });
            if (NewCentreline.Length != 1) return null;

            List<Plane> NewFrames = new List<Plane>();
            NewFrames.AddRange(Frames.Select(x => x.Item2));
            NewFrames.AddRange(glulam.Frames.Select(x => x.Item2));

            Glulam new_glulam = CreateGlulam(NewCentreline[0], NewFrames.ToArray());

            new_glulam.RemoveDuplicateFrames();

            new_glulam.Data = Data.Duplicate();
            new_glulam.Data.Samples = Data.Samples + glulam.Data.Samples;

            return new_glulam;
        }

        /// <summary>
        /// Make sure frames are correctly oriented on the curve (Z-axis tangent to the curve).
        /// </summary>
        public void RecalculateFrames()
        {
            Point3d pt_on_crv;
            Vector3d tan, xaxis, yaxis;

            for (int i = 0; i < Frames.Count; ++i)
            {
                pt_on_crv = Centreline.PointAt(Frames[i].Item1);
                tan = Centreline.TangentAt(Frames[i].Item1);
                xaxis = Vector3d.CrossProduct(Frames[i].Item2.YAxis, tan);
                yaxis = Vector3d.CrossProduct(tan, xaxis);
                Frames[i] = new Tuple<double, Plane>(Frames[i].Item1, new Plane(pt_on_crv, xaxis, yaxis));
            }
        }

        /// <summary>
        /// Get the glulam frames (planes) around curve parameter t. Useful for interpolation or getting intermediate orientations.
        /// </summary>
        /// <param name="t">Curve parameter to evaluate.</param>
        /// <returns>Tuple consisting of the frame before, the frame after, and the normalized parameter between them where t is.</returns>
        public virtual Tuple<Plane, Plane, double> FramesAround(double t)
        {
            return new Tuple<Plane, Plane, double>(Plane.Unset, Plane.Unset, 0.0);
        }

        /// <summary>
        /// Duplicate glulam data.
        /// </summary>
        /// <returns></returns>
        public Glulam Duplicate()
        {
            Curve c = Centreline.Duplicate() as Curve;
            Plane[] NewPlanes = Frames.Select(x => x.Item2).ToArray();
            GlulamData data = Data;

            return CreateGlulam(c, NewPlanes, data);
        }

        public bool Extend(CurveEnd end, double length)
        {
            Curve c = Centreline.Extend(end, length, CurveExtensionStyle.Smooth);
            Centreline = c;

            return true;
        }

        /// <summary>
        /// Split glulam into two at parameter t.
        /// </summary>
        /// <param name="t">Curve parameter to split glulam at.</param>
        /// <returns>List of new glulams.</returns>
        public List<Glulam> Split(double t)
        {
            if (!Centreline.Domain.IncludesParameter(t)) return null;

            double percentage = (t - Centreline.Domain.Min) / (Centreline.Domain.Max - Centreline.Domain.Min);

            Plane split_plane = GetPlane(t);
            Curve[] split_curves = Centreline.Split(t);
            if (split_curves == null || split_curves.Length != 2) return null;

            List<Tuple<double, Plane>> Frames1 = Frames.Where(x => x.Item1 < t).ToList();
            List<Tuple<double, Plane>> Frames2 = Frames.Where(x => x.Item1 >= t).ToList();

            GlulamData Data1 = Data.Duplicate();
            Data1.Samples = (int)(Data.Samples * percentage);

            Glulam Blank1 = CreateGlulam(split_curves[0], new Plane[] { split_plane }, Data1);
            Blank1.Frames.AddRange(Frames1);
            Blank1.SortFrames();
            Blank1.RecalculateFrames();

            GlulamData Data2 = Data.Duplicate();
            Data2.Samples = (int)(Data.Samples * (1 - percentage));

            Glulam Blank2 = CreateGlulam(split_curves[1], new Plane[] { split_plane }, Data2);
            Blank2.Frames.AddRange(Frames2);
            Blank2.SortFrames();
            Blank2.RecalculateFrames();

            List<Glulam> blanks = new List<Glulam>() { Blank1, Blank2 };
            return blanks;
        }

        public Glulam Extract(Interval domain, double overlap)
        {
            //domain = new Interval(
            //    Math.Max(domain.Min, Centreline.Domain.Min),
            //    Math.Min(domain.Max, Centreline.Domain.Max));

            double l1 = Centreline.GetLength(new Interval(Centreline.Domain.Min, domain.Min));
            double l2 = Centreline.GetLength(new Interval(Centreline.Domain.Min, domain.Max));
            double t1, t2;
            Centreline.LengthParameter(l1 - overlap, out t1);
            Centreline.LengthParameter(l2 + overlap, out t2);

            domain = new Interval(
                Math.Max(domain.Min, Centreline.Domain.Min),
                Math.Min(domain.Max, Centreline.Domain.Max));

            double length = Centreline.GetLength(domain);
            double percentage = length / Centreline.GetLength();

            GlulamData data = Data.Duplicate();
            data.Samples = Math.Max(2, (int)(data.Samples * percentage));

            List<Tuple<double, Plane>> NewFrames = Frames.Where(x => domain.IncludesParameter(x.Item1)).ToList();
            NewFrames.Insert(0, new Tuple<double, Plane>(domain.Min, this.GetPlane(domain.Min)));
            NewFrames.Add(new Tuple<double, Plane>(domain.Max, this.GetPlane(domain.Max)));

            Glulam glulam = CreateGlulam(Centreline.Trim(domain), NewFrames.Select(x => x.Item2).ToArray(), data);

            return glulam;
        }

        /// <summary>
        /// Split glulam into two at parameter t, with an overlap of a certain length.
        /// </summary>
        /// <param name="t">Curve parameter to split glulam at.</param>
        /// <param name="overlap">Amount of overlap.</param>
        /// <returns>List of new glulams.</returns>
        public List<Glulam> Split(double t, double overlap)
        {
            if (overlap < Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) return Split(t);

            if (!Centreline.Domain.IncludesParameter(t)) return null;
            double split_length = Centreline.GetLength(new Interval(Centreline.Domain.Min, t));

            double t1;
            double t2;

            if (!Centreline.LengthParameter(split_length + (overlap / 2), out t1)) return null;
            if (!Centreline.LengthParameter(split_length - (overlap / 2), out t2)) return null;

            if (!Centreline.Domain.IncludesParameter(t1) || !Centreline.Domain.IncludesParameter(t2)) return null;

            Curve[] split_curves;
            Plane split_plane;
            double percentage;

            Glulam Blank1, Blank2;
            GlulamData Data1, Data2;
            {
                percentage = (t1 - Centreline.Domain.Min) / (Centreline.Domain.Max - Centreline.Domain.Min);
                split_plane = GetPlane(t1);
                split_curves = Centreline.Split(t1);
                if (split_curves == null || split_curves.Length != 2) return null;

                List<Tuple<double, Plane>> Frames1 = Frames.Where(x => x.Item1 < t1).ToList();

                Data1 = Data.Duplicate();
                Data1.Samples = Math.Max(2, (int)(Data.Samples * percentage));

                Blank1 = CreateGlulam(split_curves[0], new Plane[] { split_plane }, Data1);
                Blank1.Frames.AddRange(Frames1);
                Blank1.SortFrames();
                Blank1.RecalculateFrames();


            }
            {
                percentage = (t2 - Centreline.Domain.Min) / (Centreline.Domain.Max - Centreline.Domain.Min);
                split_plane = GetPlane(t2);
                split_curves = Centreline.Split(t2);
                if (split_curves == null || split_curves.Length != 2) return null;

                List<Tuple<double, Plane>> Frames2 = Frames.Where(x => x.Item1 >= t2).ToList();

                Data2 = Data.Duplicate();
                Data2.Samples = Math.Max(2, (int)(Data.Samples * (1 - percentage)));

                Blank2 = CreateGlulam(split_curves[1], new Plane[] { split_plane }, Data2);
                Blank2.Frames.AddRange(Frames2);
                Blank2.SortFrames();
                Blank2.RecalculateFrames();
            }

            List<Glulam> blanks = new List<Glulam>() { Blank1, Blank2 };
            return blanks;
        }

        public List<Glulam> Split(double[] t, double overlap = 0.0)
        {
            Glulam temp = this;
            Array.Sort(t);
            List<Glulam> glulams = new List<Glulam>();

            for (int i = 1; i < t.Length - 1; ++i)
            {
                List<Glulam> splits = Split(t[i], overlap);

                if (splits == null || splits.Count < 2)
                    continue;

                if (splits[0] != null)
                    glulams.Add(splits[0]);
                temp = splits[1];
            }

            if (temp != null)
                glulams.Add(temp);

            return glulams;
        }

        /// <summary>
        /// Returns a list of mesh face indices that are outside of the fibre cutting angle limit.
        /// </summary>
        /// <param name="m">Input mesh to check against fibre cutting angle.</param>
        /// <param name="angle">Fibre cutting angle (in radians, default is 5 degrees (0.0872665 radians)).</param>
        /// <returns>Mesh face indices of faces outside of the fibre cutting angle.</returns>
        public int[] CheckFibreCuttingAngle(Mesh m, double angle = 0.0872665)
        {
            Mesh mm = MapToCurveSpace(m);

            List<int> fi = new List<int>();

            for (int i = 0; i < mm.Faces.Count; ++i)
            {
                double dot = Math.Abs(mm.FaceNormals[i] * Vector3d.ZAxis);
                if (dot > Math.Sin(angle))
                {
                    fi.Add(i);
                }
            }

            return fi.ToArray();
        }

        /// <summary>
        /// Overbend glulam to account for springback.
        /// </summary>
        /// <param name="t">Amount to overbend (1.0 is no effect. Less than 1.0 relaxes the curvature, more than 1.0 increases curvature.</param>
        /// <returns>New overbent glulam.</returns>
        public virtual Glulam Overbend(double t)
        {
            return this;
        }

        /// <summary>
        /// Maps a mesh onto the curve space of the glulam. This makes other analysis much easier.
        /// </summary>
        /// <param name="m">Mesh to map.</param>
        /// <returns>New mesh that is mapped onto curve space (Y-axis is axis of curve).</returns>
        public abstract Mesh MapToCurveSpace(Mesh m);

        /// <summary>
        /// Create a new Curve which is offset from the Centreline according to the Glulam frames. This means 
        /// that the new Curve will follow the orientation and twisting of this Glulam.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public abstract Curve CreateOffsetCurve(double x, double y, bool rebuild = false, int rebuild_pts = 20);
        public abstract Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, bool rebuild = false, int rebuild_pts = 20);

        /// <summary>
        /// Gets fibre direction throughout this Glulam, given another Glulam that contains it.
        /// </summary>
        /// <param name="blank">Glulam blank to compare against.</param>
        /// <param name="angles">List of angles. The fibre direction deviates by this much from the centreline.</param>
        /// <param name="divX">Number of sampling divisions in X.</param>
        /// <param name="divY">Number of sampling divisions in Y.</param>
        /// <param name="divZ">Number of sampling divisions along the length of the Glulam.</param>
        /// <returns></returns>
        public List<Ray3d> FibreDeviation(Glulam blank, out List<double> angles, int divX = 8, int divY = 8, int divZ = 50)
        {
            double stepX = Data.LamWidth * Data.NumWidth / (divX + 1);
            double stepY = Data.LamHeight * Data.NumHeight / (divY + 1);

            List<Ray3d> rays = new List<Ray3d>();
            angles = new List<double>();

            double[] tt = this.Centreline.DivideByCount(divZ, true);
            double t;

            for (int z = 0; z < tt.Length; ++z)
            {
                for (int y = -divY / 2; y <= divY / 2; ++y)
                {
                    for (int x = -divX / 2; x <= divX / 2; ++x)
                    {
                        Plane BePlane = this.GetPlane(tt[z]);
                        Point3d pt = BePlane.Origin + BePlane.YAxis * stepY * y + BePlane.XAxis * stepX * x;

                        blank.Centreline.ClosestPoint(pt, out t);

                        Vector3d tanBl = blank.Centreline.TangentAt(t);
                        Vector3d tanBe = this.Centreline.TangentAt(tt[z]);

                        double angle = Math.Acos(Math.Abs(tanBl * tanBe));

                        rays.Add(new Ray3d(pt, tanBl));
                        angles.Add(angle);
                    }
                }
            }

            return rays;
        }

        public List<Ray3d> FibreDeviation(Glulam blank, int divX = 8, int divY = 8, int divZ = 50)
        {
            List<double> angles;
            return FibreDeviation(blank, out angles, divX, divY, divZ);
        }

        public Brep GetEndSurface(int side, double offset, double extra_width, double extra_height, bool flip = false)
        {
            side = side.Modulus(2);
            Plane endPlane = GetPlane(side == 0 ? Centreline.Domain.Min : Centreline.Domain.Max);

            if ((flip && side == 1) || (!flip && side == 0))
                endPlane = endPlane.FlipAroundYAxis();

            endPlane.Origin = endPlane.Origin + endPlane.ZAxis * offset;

            double hwidth = Data.LamWidth * Data.NumWidth / 2 + extra_width;
            double hheight = Data.LamHeight * Data.NumHeight / 2 + extra_height;
            Rectangle3d rec = new Rectangle3d(endPlane, new Interval(-hwidth, hwidth), new Interval(-hheight, hheight));

            return Brep.CreateFromCornerPoints(rec.Corner(0), rec.Corner(1), rec.Corner(2), rec.Corner(3), Tolerance);
        }

        public Brep GetGlulamFace(tas.Core.Util.Side side)
        {
            Plane[] planes;
            double[] t;

            GenerateCrossSectionPlanes(Data.Samples, 0.0, out planes, out t, Data.InterpolationType);

            double hWidth = this.Width() / 2;
            double hHeight = this.Height() / 2;
            double x1, y1, x2, y2;
            x1 = y1 = x2 = y2 = 0;
            Rectangle3d face;

            switch (side)
            {
                case (Side.Back):
                    face = new Rectangle3d(planes.First(), new Interval(-hWidth, hWidth), new Interval(-hHeight, hHeight));
                    return Brep.CreateFromCornerPoints(face.Corner(0), face.Corner(1), face.Corner(2), face.Corner(3), 0.001);
                case (Side.Front):
                    face = new Rectangle3d(planes.Last(), new Interval(-hWidth, hWidth), new Interval(-hHeight, hHeight));
                    return Brep.CreateFromCornerPoints(face.Corner(0), face.Corner(1), face.Corner(2), face.Corner(3), 0.001);
                case (Side.Left):
                    x1 = hWidth; y1 = hHeight;
                    x2 = hWidth; y2 = -hHeight;
                    break;
                case (Side.Right):
                    x1 = -hWidth; y1 = hHeight;
                    x2 = -hWidth; y2 = -hHeight;
                    break;
                case (Side.Top):
                    x1 = hWidth; y1 = hHeight;
                    x2 = -hWidth; y2 = hHeight;
                    break;
                case (Side.Bottom):
                    x1 = hWidth; y1 = -hHeight;
                    x2 = -hWidth; y2 = -hHeight;
                    break;
            }

            Curve[] rules = new Curve[t.Length];
            for (int i = 0; i < planes.Length; ++i)
                    rules[i] = new Line(
                        planes[i].Origin + planes[i].XAxis * x1 + planes[i].YAxis * y1,
                        planes[i].Origin + planes[i].XAxis * x2 + planes[i].YAxis * y2
                        ).ToNurbsCurve();

            Brep[] loft = Brep.CreateFromLoft(rules, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
            if (loft == null || loft.Length < 1) throw new Exception("Glulam::GetGlulamFace::Loft failed!");

            Brep brep = loft[0];

            return brep;
        }

        public Brep[] GetGlulamFaces(int mask)
        {
            bool[] flags = new bool[6];
            List<Brep> breps = new List<Brep>();

            for (int i = 0; i < 6; ++i)
            {
                if ((mask & (1 << i)) > 0)
                    breps.Add(GetGlulamFace((Side)i));
            }

            return breps.ToArray();
        }

        public Brep GetSideSurface(int side, double offset, double width, double extension = 0.0, bool flip = false)
        {
            // TODO: Create access for Glulam ends, with offset (either straight or along Centreline).

            side = side.Modulus(2);
            double w2 = width / 2;

            Curve c = Centreline.DuplicateCurve();
            if (extension > 0.0)
                c = c.Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);

            double[] t = c.DivideByCount(Data.Samples, true);
            Curve[] rules = new Curve[t.Length];

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
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
            Centreline.ClosestPoint(pt, out ct);
            Vector3d nor2 = Centreline.PointAt(ct) - pt;
            nor2.Unitize();

            if (nor2 * nor < 0.0)
            {
                brep.Flip();
            }

            if (flip)
                brep.Flip();

            return brep;
        }

        public Point3d[] GenerateCorners(double offset = 0.0)
        {


            double x = Width();
            double y = Height();

            double x0 = 0, x1 = x, y0 = 0, y1 = y;
            double hx = x / 2, hy = y / 2;

            int numCorners = 4;

            m_section_corners = new Point3d[numCorners];

            switch (Data.SectionAlignment)
            {
                case (GlulamData.CrossSectionPosition.MiddleCentre):
                    x0 -= hx; y0 -= hy; x1 -= hx; y1 -= hy;
                    break;

                case (GlulamData.CrossSectionPosition.TopLeft):
                    y0 -= y; y1 -= y;
                    break;

                case (GlulamData.CrossSectionPosition.TopCentre):
                    x0 -= hx; y0 -= y; x1 -= hx; y1 -= y;
                    break;

                case (GlulamData.CrossSectionPosition.TopRight):
                    x0 -= x; y0 -= y; x1 -= x; y1 -= y;
                    break;

                case (GlulamData.CrossSectionPosition.MiddleLeft):
                    y0 -= hy; y1 -= hy;
                    break;

                case (GlulamData.CrossSectionPosition.MiddleRight):
                    x0 -= x; y0 -= hy; x1 -= x; y1 -= hy;
                    break;

                case (GlulamData.CrossSectionPosition.BottomLeft):
                    break;

                case (GlulamData.CrossSectionPosition.BottomCentre):
                    x0 -= hx; x1 -= hx; 
                    break;

                case (GlulamData.CrossSectionPosition.BottomRight):
                    x0 -= x; x1 -= x; 
                    break;
            }


            m_section_corners[0] = new Point3d(x0 - offset, y1 + offset, 0);
            m_section_corners[1] = new Point3d(x1 + offset, y1 + offset, 0);
            m_section_corners[2] = new Point3d(x1 + offset, y0 - offset, 0);
            m_section_corners[3] = new Point3d(x0 - offset, y0 - offset, 0);

            return m_section_corners;
        }

        /*
                public byte[] ToByteArray()
                {
                    byte[] centrelineBytes = Grasshopper.Kernel.GH_Convert.CommonObjectToByteArray(Centreline);
                    byte[] dataBytes = Data.ToByteArray();

                    List<byte> b = new List<byte>();

                    byte[] centrelineBytesCount = BitConverter.GetBytes((Int32)centrelineBytes.Length);
                    byte[] dataBytesCount = BitConverter.GetBytes((Int32)dataBytes.Length);

                    // write type
                    b.AddRange(BitConverter.GetBytes((Int32)Type()));

                    // write centreline
                    b.AddRange(BitConverter.GetBytes((Int32)centrelineBytes.Length));
                    b.AddRange(centrelineBytes);

                    // write planes
                    b.AddRange(BitConverter.GetBytes((Int32)Frames.Count));
                    for (int i = 0; i < Frames.Count; ++i)
                    {
                        Plane p = Frames[i].Item2;
                        b.AddRange(BitConverter.GetBytes(Frames[i].Item1));

                        b.AddRange(BitConverter.GetBytes(p.OriginX));
                        b.AddRange(BitConverter.GetBytes(p.OriginX));
                        b.AddRange(BitConverter.GetBytes(p.OriginX));

                        b.AddRange(BitConverter.GetBytes(p.XAxis.X));
                        b.AddRange(BitConverter.GetBytes(p.XAxis.Y));
                        b.AddRange(BitConverter.GetBytes(p.XAxis.Z));

                        b.AddRange(BitConverter.GetBytes(p.YAxis.X));
                        b.AddRange(BitConverter.GetBytes(p.YAxis.Y));
                        b.AddRange(BitConverter.GetBytes(p.YAxis.Z));
                    }

                    // write data
                    b.AddRange(BitConverter.GetBytes((Int32)dataBytes.Length));
                    b.AddRange(dataBytes);

                    return b.ToArray();
                }

                public static Glulam FromByteArray(byte[] b)
                {
                    int index = 0;
                    Glulam g;
                    GlulamType type = (GlulamType)BitConverter.ToInt32(b, index); index += 4;
                    int cl_byte_count = BitConverter.ToInt32(b, index); index += 4;

                    byte[] cl_bytes = new byte[cl_byte_count];
                    Array.Copy(b, index, cl_bytes, 0, cl_byte_count);
                    index += cl_byte_count;

                    Curve cl = Grasshopper.Kernel.GH_Convert.ByteArrayToCommonObject<Curve>(cl_bytes);
                    List<Plane> planes = new List<Plane>();
                    List<double> frame_t = new List<double>();

                    // read frames

                    int frame_count = BitConverter.ToInt32(b, index); index += 4;
                    for (int i = 0; i < frame_count; ++i)
                    {
                        double t, ox, oy, oz, xx, xy, xz, yx, yy, yz;
                        t = BitConverter.ToDouble(b, index); index += 8;
                        ox = BitConverter.ToDouble(b, index); index += 8;
                        oy = BitConverter.ToDouble(b, index); index += 8;
                        oz = BitConverter.ToDouble(b, index); index += 8;
                        xx = BitConverter.ToDouble(b, index); index += 8;
                        xy = BitConverter.ToDouble(b, index); index += 8;
                        xz = BitConverter.ToDouble(b, index); index += 8;
                        yx = BitConverter.ToDouble(b, index); index += 8;
                        yy = BitConverter.ToDouble(b, index); index += 8;
                        yz = BitConverter.ToDouble(b, index); index += 8;

                        frame_t.Add(t);
                        planes.Add(new Plane(new Point3d(ox, oy, oz), new Vector3d(xx, xy, xz), new Vector3d(yx, yy, yz)));
                    }

                    byte[] data_bytes = new byte[28];
                    Array.Copy(b, index, data_bytes, 0, 28);

                    GlulamData data = GlulamData.FromByteArray(data_bytes);

                    return CreateGlulam(cl, planes.ToArray(), data);
                }
        */
    }

    public enum GlulamType
    {
        Straight,
        SingleCurved,
        DoubleCurved//,
        //Freeform
    }

    public class GlulamData
    {
        public enum Interpolation
        {
            LINEAR = 0,
            HERMITE = 1,
            CUBIC = 2
        }

        public enum CrossSectionPosition
        {
            TopLeft,
            TopCentre,
            TopRight,
            MiddleLeft,
            MiddleCentre,
            MiddleRight,
            BottomLeft,
            BottomCentre,
            BottomRight
        }

        public static double DefaultWidth = 80.0;
        public static double DefaultHeight = 80.0;
        public static double DefaultSampleDistance = 50.0;
        public static int DefaultCurvatureSamples = 40;

        public int NumWidth, NumHeight;
        public double LamWidth, LamHeight;
        public int Samples;
        public Interpolation InterpolationType = Interpolation.LINEAR;
        public CrossSectionPosition SectionAlignment = CrossSectionPosition.MiddleCentre;


        public static GlulamData Default
        { get { return new GlulamData(); } }

        public GlulamData(int num_width = 4, int num_height = 4, double lam_width = 20.0, double lam_height = 20.0, int samples = 50, CrossSectionPosition alignment = CrossSectionPosition.MiddleCentre)
        {
            NumWidth = num_width;
            NumHeight = num_height;
            LamWidth = lam_width;
            LamHeight = lam_height;
            Samples = samples;
            SectionAlignment = alignment;
        }

        /// <summary>
        /// Get lamella widths and heights from input curve and cross-section guide frames.
        /// </summary>
        /// <param name="c">Centreline curve.</param>
        /// <param name="lamella_width">Maximum lamella width.</param>
        /// <param name="lamella_height">Maximum lamella height</param>
        /// <param name="frames">Guide frames.</param>
        /// <param name="k_samples">Number of curvature samples to use.</param>
        /// <returns>A pair of doubles for maximum curvature in X and Y directions.</returns>
        public static double[] GetLamellaSizes(Curve c, out double lamella_width, out double lamella_height, Plane[] frames = null, int k_samples = 0)
        {
            if (c.IsLinear())
            {
                lamella_width = double.MaxValue;
                lamella_height = double.MaxValue;
                return new double[] { 0, 0 };
            }
            
            Beam beam = new Lam.Beam(c, frames);
            if (k_samples < 3) k_samples = DefaultCurvatureSamples;

            double[] tt = beam.Centreline.DivideByCount(k_samples, false);

            double maxK = 0.0;
            int index = 0;
            Vector3d kvec = Vector3d.Unset;
            Vector3d tVec;

            double maxKX = 0.0;
            double maxKY = 0.0;
            double dotKX, dotKY;
            Plane tPlane;

            for (int i = 0; i < tt.Length; ++i)
            {
                tVec = beam.Centreline.CurvatureAt(tt[i]);
                tPlane = beam.GetPlane(tt[i]);

                dotKX = Math.Abs(tVec * tPlane.XAxis);
                dotKY = Math.Abs(tVec * tPlane.YAxis);

                maxKX = Math.Max(dotKX, maxKX);
                maxKY = Math.Max(dotKY, maxKY);

                if (tVec.Length > maxK)
                {
                    index = i;
                    kvec = tVec;
                    maxK = tVec.Length;
                }
            }

            if (maxKX == 0.0)
                lamella_width = double.MaxValue;
            else
                lamella_width = 1 / maxKX / Glulam.RadiusMultiplier;

            if (maxKY == 0.0)
                lamella_height = double.MaxValue;
            else
                lamella_height = 1 / maxKY / Glulam.RadiusMultiplier;

            return new double[] { maxKX, maxKY };

        }

        public GlulamData(Curve c, double width, double height, Plane[] frames = null, int glulam_samples = 50, int curve_samples = 0)
        {
            double lw, lh;
            GlulamData.GetLamellaSizes(c, out lw, out lh, frames, curve_samples);
            //var r = new double[] { 1 / k[0], 1 / k[1] };

            //lw = Math.Abs(r[0] - width / 2) / Glulam.RadiusMultiplier;
            //lh = Math.Abs(r[1] - height / 2) / Glulam.RadiusMultiplier;

            lw = Math.Min(lw, width);
            lh = Math.Min(lh, height);

            this.NumWidth = (int)Math.Ceiling(width / lw);
            this.NumHeight = (int)Math.Ceiling(height / lh);

            this.LamWidth = width / NumWidth;
            this.LamHeight = height / NumHeight;

            this.Samples = glulam_samples;
        }

        public static GlulamData FromCurveLimits(Curve c, Plane[] frames = null, int k_samples = 0)
        {

            return new GlulamData(c, 100.0, 100.0, frames, (int)(c.GetLength() / DefaultSampleDistance), k_samples);

            Beam beam = new Lam.Beam(c, frames);
            if (k_samples < 3) k_samples = DefaultCurvatureSamples;

            double[] tt = beam.Centreline.DivideByCount(k_samples, false);

            double maxK = 0.0;
            int index = 0;
            Vector3d kvec = Vector3d.Unset;
            Vector3d tVec;

            double maxKX = 0.0;
            double maxKY = 0.0;
            double dotKX, dotKY;
            Plane tPlane;

            for (int i = 0; i < tt.Length; ++i)
            {
                tVec = beam.Centreline.CurvatureAt(tt[i]);
                tPlane = beam.GetPlane(tt[i]);

                dotKX = tVec * tPlane.XAxis;
                dotKY = tVec * tPlane.YAxis;

                if (dotKX > maxKX)
                {
                    maxKX = dotKX;
                }
                if (dotKY > maxKY)
                {
                    maxKY = dotKY;
                }

                if (tVec.Length > maxK)
                {
                    index = i;
                    kvec = tVec;
                    maxK = tVec.Length;
                }
            }

            double lam_width = Math.Floor(1 / Math.Abs(maxKX) * Glulam.RadiusMultiplier);
            double lam_height = Math.Floor(1 / Math.Abs(maxKY) * Glulam.RadiusMultiplier);

            Plane frame = beam.GetPlane(tt[index]);

            double max_lam_width = (double)((int)Math.Ceiling(DefaultWidth / 10.0) * 10.0);
            double max_lam_height = (double)((int)Math.Ceiling(DefaultHeight / 10.0) * 10.0);

            //double lam_width = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.XAxis) * Glulam.RadiusMultiplier), max_lam_width));
            //double lam_height = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.YAxis) * Glulam.RadiusMultiplier), max_lam_height));

            //double max_lam_width = DefaultWidth;
            //double max_lam_height = DefaultHeight;

            //double lam_width = Math.Min(1 / (Math.Abs(kvec * frame.XAxis) * Glulam.RadiusMultiplier), max_lam_width);
            //double lam_height = Math.Min(1 / (Math.Abs(kvec * frame.YAxis) * Glulam.RadiusMultiplier), max_lam_height);

            if (lam_width == 0.0) lam_width = max_lam_width;
            if (lam_height == 0.0) lam_height = max_lam_height;

            GlulamData data = new GlulamData();

            data.LamHeight = lam_height;
            data.LamWidth = lam_width;
            data.NumHeight = (int)(Math.Ceiling(DefaultHeight / lam_height));
            data.NumWidth = (int)(Math.Ceiling(DefaultWidth / lam_width));
            data.Samples = (int)(c.GetLength() / DefaultSampleDistance);

            // I forget why this is here...
            if (data.NumHeight * data.LamHeight - DefaultHeight > 20.0)
                data.LamHeight = Math.Ceiling((DefaultHeight + 10.0) / data.NumHeight);
            if (data.NumWidth * data.LamWidth - DefaultWidth > 20.0)
                data.LamWidth = Math.Ceiling((DefaultWidth + 10.0) / data.NumWidth);

            return data;
        }

        public static GlulamData FromCurveLimits(Curve c, double width, double height, Plane[] frames = null)
        {
            Beam beam = new Lam.Beam(c, frames);

            double[] tt = beam.Centreline.DivideByCount(100, true);
            double maxK = 0.0;
            int index = 0;
            Vector3d kvec = Vector3d.Unset;
            Vector3d temp;

            for (int i = 0; i < tt.Length; ++i)
            {
                temp = beam.Centreline.CurvatureAt(tt[i]);
                if (temp.Length > maxK)
                {
                    index = i;
                    kvec = temp;
                    maxK = temp.Length;
                }
            }
            Plane frame = beam.GetPlane(tt[index]);

            if (frame == null)
                throw new Exception("Frame is null!");

            //double max_lam_width = Math.Ceiling(width);
            //double max_lam_height = Math.Ceiling(height);

            double max_lam_width = width;
            double max_lam_height = height;

            //double lam_width = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.XAxis) * Glulam.RadiusMultiplier), max_lam_width));
            //double lam_height = Math.Ceiling(Math.Min(1 / (Math.Abs(kvec * frame.YAxis) * Glulam.RadiusMultiplier), max_lam_height));

            double lam_width = Math.Min(1 / (Math.Abs(kvec * frame.XAxis) * Glulam.RadiusMultiplier), max_lam_width);
            double lam_height = Math.Min(1 / (Math.Abs(kvec * frame.YAxis) * Glulam.RadiusMultiplier), max_lam_height);

            if (lam_width == 0.0) lam_width = max_lam_width;
            if (lam_height == 0.0) lam_height = max_lam_height;

            GlulamData data = new GlulamData();

            data.LamHeight = lam_height;
            data.LamWidth = lam_width;
            data.NumHeight = (int)(Math.Ceiling(height / lam_height));
            data.NumWidth = (int)(Math.Ceiling(width / lam_width));
            data.Samples = (int)(c.GetLength() / DefaultSampleDistance);

            // I forget why this is here... 
            if (data.NumHeight * data.LamHeight - height > 20.0)
                data.LamHeight = Math.Ceiling((height + 10.0) / data.NumHeight);
            if (data.NumWidth * data.LamWidth - width > 20.0)
                data.LamWidth = Math.Ceiling((width + 10.0) / data.NumWidth);

            return data;
        }

        public GlulamData Duplicate()
        {
            GlulamData data = new GlulamData();
            data.NumHeight = NumHeight;
            data.NumWidth = NumWidth;
            data.LamHeight = LamHeight;
            data.LamWidth = LamWidth;
            data.Samples = Samples;

            return data;
        }

        public byte[] ToByteArray()
        {
            List<byte> b = new List<byte>();
            b.AddRange(BitConverter.GetBytes((Int32)NumHeight));
            b.AddRange(BitConverter.GetBytes((Int32)NumWidth));
            b.AddRange(BitConverter.GetBytes(LamHeight));
            b.AddRange(BitConverter.GetBytes(LamWidth));
            b.AddRange(BitConverter.GetBytes((Int32)Samples));

            return b.ToArray();
        }

        public static GlulamData FromByteArray(byte[] b)
        {
            if (b.Length != 28)
                throw new Exception("Byte array is wrong size for GlulamData!");

            GlulamData data = new GlulamData();
            data.NumHeight = BitConverter.ToInt32(b, 0);
            data.NumWidth = BitConverter.ToInt32(b, 4);
            data.LamHeight = BitConverter.ToDouble(b, 8);
            data.LamWidth = BitConverter.ToDouble(b, 16);
            data.Samples = BitConverter.ToInt32(b, 24);

            return data;
        }
    }
        
}
