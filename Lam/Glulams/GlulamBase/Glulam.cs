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
    public abstract partial class Glulam
    {
        public static double RadiusMultiplier = 200.0; // This is the Eurocode 5 formula: lamella thickness cannot exceed 1/200th of the curvature radius.
        public static int CurvatureSamples = 100; // Number of samples to samples curvature at.
        public static double RadiusTolerance = 0.00001; // For curvature calculations: curvature radius and lamella thickness cannot exceed this


        #region Static variables and methods
        protected static double Tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
        protected static double OverlapTolerance = 1.0 * Rhino.RhinoMath.UnitScale(Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem, Rhino.UnitSystem.Millimeters);
        protected static double AngleTolerance = Math.Cos(2);

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
        // /*protected*/public List<Tuple<double, Plane>> Frames;

        public Curve Centreline { get; protected set;}
        public GlulamData Data;
        public GlulamOrientation Orientation;

        // Protected
        protected Point3d[] m_section_corners = null; // Cached section corners

        public abstract void CalculateLamellaSizes(double width, double height);

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
            //props.Add("frames", GetAllPlanes());

            double max_kw = 0.0, max_kh = 0.0;
            props.Add("max_curvature", GetMaxCurvature(ref max_kw, ref max_kh));
            props.Add("max_curvature_width", max_kw);
            props.Add("max_curvature_height", max_kh);
            props.Add("type", ToString());
            props.Add("type_id", (int)Type());
            
            return props;
        }

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

            //var planes = GetAllPlanes();
            //ArchivableDictionary pd = new ArchivableDictionary();

            //for (int i = 0; i < planes.Length; ++i)
            //{
            //    pd.Set(string.Format("Frame_{0}", i), planes[i]);
            //}
            //ad.Set("frames", pd);

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

        public virtual GlulamType Type() => GlulamType.Straight;

        public virtual Plane GetPlane(double t) => Plane.Unset;


        //public Plane[] GetAllPlanes() => Frames.Select(x => x.Item2).ToArray();

        //public void SortFrames() => Frames.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        //public void ValidateFrames() => Frames = Frames.Where(x => x.Item2.IsValid).ToList();

        public Plane GetPlane(Point3d p)
        {
            double t;
            Centreline.ClosestPoint(p, out t);
            return GetPlane(t);
        }


        public override bool Equals(object obj)
        {
            if (obj is Glulam && (obj as Glulam).ID == ID)
                return true;
            return false;
        }
        public override int GetHashCode() => ID.GetHashCode();
        public override string ToString() => "Glulam";


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
            //Point3d[] temp_points = Frames.Select(x => x.Item2.Origin).ToArray();


            Curve Reversed = Centreline.DuplicateCurve();
            Reversed.Reverse();

            Orientation.Remap(Centreline, Reversed);
            Centreline = Reversed;

            /*
            double t;
            for (int i = 0; i < Frames.Count; ++i)
            {
                Centreline.ClosestPoint(temp_points[i], out t);
                Plane p = Frames[i].Item2.FlipAroundYAxis();
                Frames[i] = new Tuple<double, Plane>(t, p);
            }

            Frames.Reverse();
            */
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

        /*
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
        */

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

            GlulamOrientation NewOrientation = Orientation.Duplicate();
            NewOrientation.Join(glulam.Orientation);

            //List<Plane> NewFrames = new List<Plane>();
            //NewFrames.AddRange(Frames.Select(x => x.Item2));
            //NewFrames.AddRange(glulam.Frames.Select(x => x.Item2));

            Glulam new_glulam = CreateGlulam(NewCentreline[0], NewOrientation, Data.Duplicate());

            //Glulam new_glulam = CreateGlulam(NewCentreline[0], NewFrames.ToArray());

            //new_glulam.RemoveDuplicateFrames();

            //new_glulam.Data = Data.Duplicate();
            new_glulam.Data.Samples = Data.Samples + glulam.Data.Samples;

            return new_glulam;
        }

        /*
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
        */
        /*
        /// <summary>
        /// Get the glulam frames (planes) around curve parameter t. Useful for interpolation or getting intermediate orientations.
        /// </summary>
        /// <param name="t">Curve parameter to evaluate.</param>
        /// <returns>Tuple consisting of the frame before, the frame after, and the normalized parameter between them where t is.</returns>
        public virtual Tuple<Plane, Plane, double> FramesAround(double t)
        {
            return new Tuple<Plane, Plane, double>(Plane.Unset, Plane.Unset, 0.0);
        }
        */
        /// <summary>
        /// Duplicate glulam data.
        /// </summary>
        /// <returns></returns>
        public Glulam Duplicate()
        {
            //Curve c = Centreline.Duplicate() as Curve;
            //Plane[] NewPlanes = Frames.Select(x => x.Item2).ToArray();
            //GlulamData data = Data;

            return CreateGlulam(Centreline.DuplicateCurve(), Orientation.Duplicate(), Data.Duplicate());
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

            //List<Tuple<double, Plane>> Frames1 = Frames.Where(x => x.Item1 < t).ToList();
            //List<Tuple<double, Plane>> Frames2 = Frames.Where(x => x.Item1 >= t).ToList();

            GlulamData Data1 = Data.Duplicate();
            Data1.Samples = (int)(Data.Samples * percentage);

            List<GlulamOrientation> SplitOrientations = Orientation.Split(new double[] { t });

            Glulam Blank1 = CreateGlulam(split_curves[0], SplitOrientations[0], Data1);
            //Glulam Blank1 = CreateGlulam(split_curves[0], new Plane[] { split_plane }, Data1);
            //Blank1.Frames.AddRange(Frames1);
            //Blank1.SortFrames();
            //Blank1.RecalculateFrames();

            GlulamData Data2 = Data.Duplicate();
            Data2.Samples = (int)(Data.Samples * (1 - percentage));

            Glulam Blank2 = CreateGlulam(split_curves[1], SplitOrientations[1], Data2);
            //Glulam Blank2 = CreateGlulam(split_curves[1], new Plane[] { split_plane }, Data2);
            //Blank2.Frames.AddRange(Frames2);
            //Blank2.SortFrames();
            //Blank2.RecalculateFrames();

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

            List<GlulamOrientation> SplitOrientations = Orientation.Split(new double[] { domain.Min, domain.Max });

            //List<Tuple<double, Plane>> NewFrames = Frames.Where(x => domain.IncludesParameter(x.Item1)).ToList();
            //NewFrames.Insert(0, new Tuple<double, Plane>(domain.Min, this.GetPlane(domain.Min)));
            //NewFrames.Add(new Tuple<double, Plane>(domain.Max, this.GetPlane(domain.Max)));

            Glulam glulam = CreateGlulam(Centreline.Trim(domain), SplitOrientations[1], data);
            //Glulam glulam = CreateGlulam(Centreline.Trim(domain), NewFrames.Select(x => x.Item2).ToArray(), data);

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

                //List<Tuple<double, Plane>> Frames1 = Frames.Where(x => x.Item1 < t1).ToList();

                var SplitOrientation = Orientation.Split(new double[] { t1 });

                Data1 = Data.Duplicate();
                Data1.Samples = Math.Max(2, (int)(Data.Samples * percentage));

                Blank1 = CreateGlulam(split_curves[0], SplitOrientation[0], Data1);
                //Blank1 = CreateGlulam(split_curves[0], new Plane[] { split_plane }, Data1);
                
                //Blank1.Frames.AddRange(Frames1);
                //Blank1.SortFrames();
                //Blank1.RecalculateFrames();


            }
            {
                percentage = (t2 - Centreline.Domain.Min) / (Centreline.Domain.Max - Centreline.Domain.Min);
                split_plane = GetPlane(t2);
                split_curves = Centreline.Split(t2);
                if (split_curves == null || split_curves.Length != 2) return null;

                //List<Tuple<double, Plane>> Frames2 = Frames.Where(x => x.Item1 >= t2).ToList();

                var SplitOrientation = Orientation.Split(new double[] { t2 });

                Data2 = Data.Duplicate();
                Data2.Samples = Math.Max(2, (int)(Data.Samples * (1 - percentage)));

                Blank2 = CreateGlulam(split_curves[1], SplitOrientation[1], Data2);
                //Blank2 = CreateGlulam(split_curves[1], new Plane[] { split_plane }, Data2);
                //Blank2.Frames.AddRange(Frames2);
                //Blank2.SortFrames();
                //Blank2.RecalculateFrames();
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

       
}
