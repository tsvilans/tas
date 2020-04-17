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

namespace tas.Lam
{
    public abstract class FreeformGlulam : Glulam
    {
        /// <summary>
        /// Generate a series of planes on the glulam cross-section. TODO: Re-implement as GlulamOrientation function
        /// </summary>
        /// <param name="N">Number of planes to extract.</param>
        /// <param name="extension">Extension of the centreline curve</param>
        /// <param name="planes">Output cross-section planes.</param>
        /// <param name="t">Output t-values along centreline curve.</param>
        /// <param name="interpolation">Type of interpolation to use (default is Linear).</param>
        public override void GenerateCrossSectionPlanes(int N, double extension, out Plane[] planes, out double[] t, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            N = Math.Max(N, 2);

            planes = new Plane[N];
            Curve CL;
            if (Centreline.IsClosed)
                CL = Centreline.DuplicateCurve();
            else
                CL = Centreline.Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);

            t = CL.DivideByCount(N - 1, true);

            GlulamOrientation TempOrientation = Orientation.Duplicate();
            TempOrientation.Remap(Centreline, CL);

            for (int i = 0; i < N; ++i)
            {
                Vector3d v = TempOrientation.GetOrientation(CL, t[i]);
                planes[i] = tas.Core.Util.Misc.PlaneFromNormalAndYAxis(CL.PointAt(t[i]), CL.TangentAt(t[i]), v);
            }

            return;
            /*
            double[] ft = new double[Frames.Count];
            double[] fa = new double[Frames.Count];

            Plane temp;
            for (int i = 0; i < Frames.Count; ++i)
            {
                CL.PerpendicularFrameAt(Frames[i].Item1, out temp);
                ft[i] = Frames[i].Item1;
                //fa[i] = Math.Acos(temp.YAxis * Frames[i].Item2.YAxis);
                fa[i] = Vector3d.VectorAngle(temp.YAxis, Frames[i].Item2.YAxis, Frames[i].Item2);
            }

            for (int i = 1; i < fa.Length; ++i)
            {
                if (fa[i] - fa[i - 1] > Math.PI)
                    fa[i] -= Constants.Tau;
                else if (fa[i] - fa[i - 1] < -Math.PI)
                    fa[i] += Constants.Tau;
            }

            int res;
            int max = ft.Length - 1;
            double mu;

            double[] angles = new double[N];

            if (Frames.Count < 3)
                interpolation = GlulamData.Interpolation.LINEAR;

            switch (interpolation)
            {
                case (GlulamData.Interpolation.HERMITE): // Hermite Interpolation
                    for (int i = 0; i < N; ++i)
                    {
                        if (t[i] < ft[0])
                        {
                            angles[i] = fa[0];
                            continue;
                        }
                        else if (t[i] > ft.Last())
                        {
                            angles[i] = fa.Last();
                            continue;
                        }

                        res = Array.BinarySearch(ft, t[i]);
                        if (res < 0)
                        {
                            res = ~res;
                            res--;
                        }

                        if (res == 0 && res < max - 1)
                        {
                            mu = (t[i] - ft[0]) / (ft[1] - ft[0]);
                            angles[i] = Interpolation.HermiteInterpolate(fa[0], fa[0], fa[1], fa[2], mu, 0, 0);
                        }
                        else if (res > 0 && res < max - 1)
                        {
                            mu = (t[i] - ft[res]) / (ft[res + 1] - ft[res]);
                            angles[i] = Interpolation.HermiteInterpolate(fa[res - 1], fa[res], fa[res + 1], fa[res + 2], mu, 0, 0);

                        }
                        else if (res > 0 && res < max)
                        {
                            mu = (t[i] - ft[res]) / (ft[res + 1] - ft[res]);
                            angles[i] = Interpolation.HermiteInterpolate(fa[res - 1], fa[res], fa[res + 1], fa[res + 1], mu, 0, 0);
                        }
                        else if (res == max)
                        {
                            angles[i] = fa[res];
                        }

                        else
                            continue;
                    }
                    break;

                case (GlulamData.Interpolation.CUBIC): // Cubic Interpolation
                    for (int i = 0; i < N; ++i)
                    {
                        if (t[i] <= ft[0])
                        {
                            angles[i] = fa[0];
                            continue;
                        }
                        else if (t[i] >= ft.Last())
                        {
                            angles[i] = fa.Last();
                            continue;
                        }

                        res = Array.BinarySearch(ft, t[i]);
                        if (res < 0)
                        {
                            res = ~res;
                            res--;
                        }

                        if (res == 0 && res < max - 1)
                        {
                            mu = (t[i] - ft[0]) / (ft[1] - ft[0]);
                            angles[i] = Interpolation.CubicInterpolate(fa[0], fa[0], fa[1], fa[2], mu);
                        }
                        else if (res > 0 && res < max - 1)
                        {
                            mu = (t[i] - ft[res]) / (ft[res + 1] - ft[res]);
                            angles[i] = Interpolation.CubicInterpolate(fa[res - 1], fa[res], fa[res + 1], fa[res + 2], mu);

                        }
                        else if (res > 0 && res < max)
                        {
                            mu = (t[i] - ft[res]) / (ft[res + 1] - ft[res]);
                            angles[i] = Interpolation.CubicInterpolate(fa[res - 1], fa[res], fa[res + 1], fa[res + 1], mu);
                        }
                        else if (res == max)
                        {
                            angles[i] = fa[res];
                        }

                        else
                            continue;
                    }
                    break;

                default: // Default linear interpolation
                    for (int i = 0; i < N; ++i)
                    {
                        res = Array.BinarySearch(ft, t[i]);
                        if (res < 0)
                        {
                            res = ~res;
                            res--;
                        }
                        if (res >= 0 && res < max)
                        {
                            if (ft[res + 1] - ft[res] == 0)
                                mu = 0.5;
                            else
                                mu = Math.Min(1.0, Math.Max(0, (t[i] - ft[res]) / (ft[res + 1] - ft[res])));
                            angles[i] = Interpolation.Lerp(fa[res], fa[res + 1], mu);
                        }
                        else if (res < 0)
                            angles[i] = fa[0];
                        else if (res >= max)
                            angles[i] = fa[max];
                    }
                    break;
            }

            for (int i = 0; i < N; ++i)
            {
                CL.PerpendicularFrameAt(t[i], out temp);
                temp.Transform(Rhino.Geometry.Transform.Rotation(angles[i], temp.ZAxis, temp.Origin));
                planes[i] = temp;
            }
            */
        }

        public override Mesh GetBoundingMesh(double offset = 0.0, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            Mesh m = new Mesh();

            //Curve CL = Centreline.Extend(CurveEnd.Both, offset, CurveExtensionStyle.Smooth);
            
            double[] DivParams;
            Plane[] xPlanes;
            // Old way of generating cross-section planes
            //GenerateCrossSectionPlanes(Data.Samples, offset, out planes, out t, Data.InterpolationType);

            // Experimental new way of generative cross-section planes
            int N = Math.Max(Data.Samples, 2);
            var parameters = Centreline.DivideByCount(N - 1, true).ToList();

            xPlanes = parameters.Select(x => tas.Core.Util.Misc.PlaneFromNormalAndYAxis(
                Centreline.PointAt(x),
                Centreline.TangentAt(x),
                Orientation.GetOrientation(Centreline, x))).ToArray();

            //double Step = (Centreline.Domain.Max - Centreline.Domain.Min) / Samples;
            double hW = Data.NumWidth * Data.LamWidth / 2 + offset;
            double hH = Data.NumHeight * Data.LamHeight / 2 + offset;

            // vertex index and next frame vertex index
            int i4;
            int ii4;

            double Length = Centreline.GetLength() + offset * 2;
            double MaxT = parameters.Last() - parameters.First();
            double Width = Data.NumWidth * Data.LamWidth / 1000;
            double Height = Data.NumHeight * Data.LamHeight / 1000;

            for (int i = 0; i < xPlanes.Length; ++i)
            {
                i4 = i * 8;
                ii4 = i4 - 8;


                for (int j = -1; j <= 1; j += 2)
                {
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Point3d v = xPlanes[i].Origin + hW * j * xPlanes[i].XAxis + hH * k * xPlanes[i].YAxis;
                        m.Vertices.Add(v);
                        m.Vertices.Add(v);
                    }
                }

                //double DivV = DivParams[i] / MaxT;
                double DivV = parameters[i] / MaxT * Length / 1000;
                m.TextureCoordinates.Add(2 * Width + 2 * Height, DivV);
                m.TextureCoordinates.Add(0.0, DivV);

                m.TextureCoordinates.Add(Height, DivV);
                m.TextureCoordinates.Add(Height, DivV);

                m.TextureCoordinates.Add(2 * Height + Width, DivV);
                m.TextureCoordinates.Add(2 * Height + Width, DivV);

                m.TextureCoordinates.Add(Width + Height, DivV);
                m.TextureCoordinates.Add(Width + Height, DivV);


                if (i > 0)
                {

                    m.Faces.AddFace(i4 + 2,
                      ii4 + 2,
                      ii4 + 1,
                      i4 + 1);
                    m.Faces.AddFace(i4 + 6,
                      ii4 + 6,
                      ii4 + 3,
                      i4 + 3);
                    m.Faces.AddFace(i4 + 4,
                      ii4 + 4,
                      ii4 + 7,
                      i4 + 7);
                    m.Faces.AddFace(i4,
                      ii4,
                      ii4 + 5,
                      i4 + 5);
                }
            }

            Plane pplane;

            // Start cap
            pplane = xPlanes.First();
            Point3d vc = pplane.Origin + hW * -1 * pplane.XAxis + hH * -1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * -1 * pplane.XAxis + hH * 1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * 1 * pplane.XAxis + hH * -1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * 1 * pplane.XAxis + hH * 1 * pplane.YAxis;
            m.Vertices.Add(vc);

            m.TextureCoordinates.Add(0, 0);
            m.TextureCoordinates.Add(0, Height);
            m.TextureCoordinates.Add(Width, 0);
            m.TextureCoordinates.Add(Width, Height);

            m.Faces.AddFace(m.Vertices.Count - 4,
              m.Vertices.Count - 3,
              m.Vertices.Count - 1,
              m.Vertices.Count - 2);

            // End cap
            pplane = xPlanes.Last();
            vc = pplane.Origin + hW * -1 * pplane.XAxis + hH * -1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * -1 * pplane.XAxis + hH * 1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * 1 * pplane.XAxis + hH * -1 * pplane.YAxis;
            m.Vertices.Add(vc);
            vc = pplane.Origin + hW * 1 * pplane.XAxis + hH * 1 * pplane.YAxis;
            m.Vertices.Add(vc);

            m.TextureCoordinates.Add(0, 0);
            m.TextureCoordinates.Add(0, Height);
            m.TextureCoordinates.Add(Width, 0);
            m.TextureCoordinates.Add(Width, Height);

            m.Faces.AddFace(m.Vertices.Count - 2,
              m.Vertices.Count - 1,
              m.Vertices.Count - 3,
              m.Vertices.Count - 4);
            //m.UserDictionary.ReplaceContentsWith(GetArchivableDictionary());
            //m.UserDictionary.Set("glulam", GetArchivableDictionary());

            return m;
        }

        public Polyline[] GetCrossSections(double offset = 0.0)
        {
            Curve CL;
            if (offset > 0)
                CL = Centreline.Extend(CurveEnd.Both, offset, CurveExtensionStyle.Smooth);
            else
                CL = Centreline.DuplicateCurve();

            Plane[] planes;
            double[] tt;
            GenerateCrossSectionPlanes(Data.Samples, offset, out planes, out tt, Data.InterpolationType);

            double hW = Data.NumWidth * Data.LamWidth / 2 + offset;
            double hH = Data.NumHeight * Data.LamHeight / 2 + offset;

            Polyline[] xSections = new Polyline[Data.Samples];

            Rhino.Geometry.Transform xform;
            Polyline pl = new Polyline(
                new Point3d[] {
                    new Point3d(-hW, -hH, 0),
                    new Point3d(hW, -hH, 0),
                    new Point3d(hW, hH, 0),
                    new Point3d(-hW, hH, 0),
                    new Point3d(-hW, -hH, 0)});

            Polyline temp;

            for (int i = 0; i < planes.Length; ++i)
            {
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, planes[i]);
                temp = new Polyline(pl); temp.Transform(xform);
                xSections[i] = temp;
            }

            return xSections;
        }

        public override Brep GetBoundingBrep(double offset = 0.0)
        {
            //double[] t;
            Plane[] planes;

            // Old way of generating cross-section planes
            //GenerateCrossSectionPlanes(Data.Samples, offset, out planes, out t, Data.InterpolationType);

            // Experimental new way of generative cross-section planes
            int N = Math.Max(Data.Samples, 2);
            var parameters = Centreline.DivideByCount(N - 1, true).ToList();

            planes = parameters.Select(x => tas.Core.Util.Misc.PlaneFromNormalAndYAxis(
                Centreline.PointAt(x),
                Centreline.TangentAt(x),
                Orientation.GetOrientation(Centreline, x))).ToArray();


            //double hwidth = Width() / 2 + offset;
            //double hheight = Height() / 2 + offset;

            int numCorners = 4;
            //if (m_section_corners == null || offset > 0.0)
            GenerateCorners(offset);

            //corners[0] = new Point3d(-hwidth, hheight, 0);
            //corners[1] = new Point3d(hwidth, hheight, 0);
            //corners[2] = new Point3d(hwidth, -hheight, 0);
            //corners[3] = new Point3d(-hwidth, -hheight, 0);

            List<Point3d>[] crvPts = new List<Point3d>[numCorners];
            for (int i = 0; i < numCorners; ++i)
            {
                crvPts[i] = new List<Point3d>();
            }

            Transform xform;
            Point3d temp;

            for (int i = 0; i < Data.Samples; ++i)
            {
                planes[i] = planes[i].FlipAroundYAxis();
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, planes[i]);

                for (int j = 0; j < numCorners; ++j)
                {
                    temp = new Point3d(m_section_corners[j]);
                    temp.Transform(xform);
                    crvPts[j].Add(temp);
                }
            }

            Curve[] edges = new Curve[numCorners + 4];

            for (int i = 0; i < numCorners; ++i)
            {
                edges[i] = Curve.CreateInterpolatedCurve(crvPts[i], 3);
            }

            edges[4] = new Line(crvPts[3][0], crvPts[0][0]).ToNurbsCurve();
            edges[5] = new Line(crvPts[2][0], crvPts[1][0]).ToNurbsCurve();

            edges[6] = new Line(crvPts[2][Data.Samples - 1], crvPts[1][Data.Samples - 1]).ToNurbsCurve();
            edges[7] = new Line(crvPts[3][Data.Samples - 1], crvPts[0][Data.Samples - 1]).ToNurbsCurve();

            Brep[] sides = new Brep[numCorners + 2];
            int ii = 0;
            for (int i = 0; i < numCorners; ++i)
            {
                ii = (i + 1).Modulus(numCorners);
                sides[i] = Brep.CreateFromLoft(
                  new Curve[] { edges[i], edges[ii] },
                  Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];
            }

            sides[numCorners + 0] = Brep.CreateFromLoft(
              new Curve[] { edges[numCorners + 0], edges[numCorners + 1] },
              Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

            sides[numCorners + 1] = Brep.CreateFromLoft(
              new Curve[] { edges[numCorners + 2], edges[numCorners + 3] },
              Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

            Brep brep = Brep.JoinBreps(
              sides,
              Tolerance
              )[0];

            //brep.UserDictionary.Set("glulam", GetArchivableDictionary());

            return brep;
        }

        public override List<Brep> GetLamellaBreps()
        {
            double Length = Centreline.GetLength();
            double hW = Data.NumWidth * Data.LamWidth / 2;
            double hH = Data.NumHeight * Data.LamHeight / 2;
            double[] DivParams = Centreline.DivideByCount(Data.Samples, true);

            List<Curve>[,] LoftCurves = new List<Curve>[Data.NumWidth, Data.NumHeight];
            List<Brep> LamellaBreps = new List<Brep>();

            // initialize curve lists
            for (int i = 0; i < Data.NumWidth; ++i)
                for (int j = 0; j < Data.NumHeight; ++j)
                    LoftCurves[i, j] = new List<Curve>();

            for (int i = 0; i < DivParams.Length; ++i)
            {
                Plane p = GetPlane(DivParams[i]);

                for (int j = 0; j < Data.NumWidth; ++j)
                {
                    for (int k = 0; k < Data.NumHeight; ++k)
                    {
                        Rectangle3d rec = new Rectangle3d(p,
                            new Interval(-hW + j * Data.LamWidth, -hW + (j + 1) * Data.LamWidth),
                            new Interval(-hH + k * Data.LamHeight, -hH + (k + 1) * Data.LamHeight));
                        LoftCurves[j, k].Add(rec.ToNurbsCurve());
                    }
                }
            }

            for (int i = 0; i < Data.NumWidth; ++i)
            {
                for (int j = 0; j < Data.NumHeight; ++j)
                {
                    Brep[] brep = Brep.CreateFromLoft(LoftCurves[i, j], Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                    if (brep != null && brep.Length > 0)
                        LamellaBreps.Add(brep[0].CapPlanarHoles(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance));
                }
            }
            return LamellaBreps;
        }

        public override List<Mesh> GetLamellaMeshes()
        {
            return base.GetLamellaMeshes();
        }

        public override List<Curve> GetLamellaCurves()
        {
            double[] DivParams;
            Plane[] planes;
            //GenerateCrossSectionPlanes(Data.Samples, 0, out xPlanes, out DivParams, Data.InterpolationType);

            // Old way of generating cross-section planes
            //GenerateCrossSectionPlanes(Data.Samples, offset, out planes, out t, Data.InterpolationType);

            // Experimental new way of generative cross-section planes
            int N = Math.Max(Data.Samples, 2);
            var parameters = Centreline.DivideByCount(N - 1, true).ToList();

            planes = parameters.Select(x => tas.Core.Util.Misc.PlaneFromNormalAndYAxis(
                Centreline.PointAt(x),
                Centreline.TangentAt(x),
                Orientation.GetOrientation(Centreline, x))).ToArray();

            List<Point3d>[] crvPts = new List<Point3d>[Data.Lamellae.Length];
            for (int i = 0; i < Data.Lamellae.Length; ++i)
            {
                crvPts[i] = new List<Point3d>();
            }

            // ****************

            Transform xform;
            Point3d temp;

            double hWidth = Data.NumWidth * Data.LamWidth / 2;
            double hHeight = Data.NumHeight * Data.LamHeight / 2;
            double hLw = Data.LamWidth / 2;
            double hLh = Data.LamHeight / 2;

            List<Point3d> LamellaPoints = new List<Point3d>();

            for (int x = 0; x < Data.Lamellae.GetLength(0); ++x)
            {
                for (int y = 0; y < Data.Lamellae.GetLength(1); ++y)
                {
                    LamellaPoints.Add(
                        new Point3d(
                            -hWidth + hLw + x * Data.LamWidth,
                            -hHeight + hLh + y * Data.LamHeight,
                            0));
                }
            }

            for (int i = 0; i < Data.Samples; ++i)
            {
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, planes[i]);

                for (int j = 0; j < Data.Lamellae.Length; ++j)
                {
                    temp = new Point3d(LamellaPoints[j]);
                    temp.Transform(xform);
                    crvPts[j].Add(temp);
                }
            }

            Curve[] LamellaCentrelines = new Curve[Data.Lamellae.Length];

            for (int i = 0; i < Data.Lamellae.Length; ++i)
            {
                LamellaCentrelines[i] = Curve.CreateInterpolatedCurve(crvPts[i], 3);
            }

            return LamellaCentrelines.ToList();
            /*

            List<Rhino.Geometry.Curve> crvs = new List<Rhino.Geometry.Curve>();


            Rhino.Geometry.Plane plane;
            //Centreline.PerpendicularFrameAt(Centreline.Domain.Min, out plane);



            List<List<List<Rhino.Geometry.Point3d>>> verts;

            verts = new List<List<List<Point3d>>>();
            for (int i = 0; i < Data.NumHeight; ++i)
            {
                verts.Add(new List<List<Point3d>>());
                for (int j = 0; j < Data.NumWidth; ++j)
                {
                    verts[i].Add(new List<Rhino.Geometry.Point3d>());
                }
            }
            double t;
            for (int i = 0; i < xPlanes.Length; ++i)
            {
                plane = xPlanes[i];
                var xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, plane);


                for (int j = 0; j < Data.NumHeight; ++j)
                {
                    for (int k = 0; k < Data.NumWidth; ++k)
                    {
                        Rhino.Geometry.Point3d p = new Rhino.Geometry.Point3d(
                            k * Data.LamWidth - hWidth + hLw, 
                            j * Data.LamHeight - hHeight + hLh, 
                            0.0);
                        p.Transform(xform);
                        verts[j][k].Add(p);
                    }
                }
            }

            for (int i = 0; i < Data.NumHeight; ++i)
            {
                for (int j = 0; j < Data.NumWidth; ++j)
                {
                    crvs.Add(new Rhino.Geometry.Polyline(verts[i][j]).ToNurbsCurve());
                }
            }

            return crvs;
            */
        }

        public override void Transform(Transform x)
        {
            Centreline.Transform(x);
            Orientation.Transform(x);
            /*
            for (int i = 0; i < Frames.Count; ++i)
            {
                Plane p = Frames[i].Item2;
                p.Transform(x);

                Frames[i] = new Tuple<double, Plane>(Frames[i].Item1, p);
            }
            */
        }

        public override Plane GetPlane(double t)
        {
            Vector3d v = Orientation.GetOrientation(Centreline, t);
            return tas.Core.Util.Misc.PlaneFromNormalAndYAxis(Centreline.PointAt(t), Centreline.TangentAt(t), v);
            /*
            Tuple<Plane, Plane, double> faround = FramesAround(t);
            Plane plane;

            //double tt = Util.CosineInterpolate(0, 1.0, faround.Item3);
            double tt = faround.Item3;

            plane = Interpolation.InterpolatePlanes2(faround.Item1, faround.Item2, tt);
            plane.Origin = Centreline.PointAt(t);
            plane.Transform(Rhino.Geometry.Transform.Rotation(plane.ZAxis, Centreline.TangentAt(t), plane.Origin));

            return plane;
            */
        }
        /*
        public override Tuple<Plane, Plane, double> FramesAround(double t)
        {
            if (Frames.Count < 1) return null;

            double tt = 0;
            int index = 1;
            int last = Frames.Count - 1;

            if (t <= Frames[0].Item1)
                return new Tuple<Plane, Plane, double>(Frames[0].Item2, Frames[0].Item2, 0.0);
            else if (t > Frames[last].Item1)
            {
                return new Tuple<Plane, Plane, double>(Frames[last].Item2, Frames[last].Item2, 1.0);
            }

            for (int i = 1; i < Frames.Count; ++i)
            {
                if (t > Frames[i].Item1) continue;
                else
                {
                    index = i;
                    break;
                }
            }
            tt = (t - Frames[index - 1].Item1) / (Frames[index].Item1 - Frames[index - 1].Item1);
            return new Tuple<Plane, Plane, double>(Frames[index - 1].Item2, Frames[index].Item2, tt);
        }
        */
        public override Glulam Overbend(double t)
        {
            PolyCurve pc = Centreline.DuplicateCurve() as PolyCurve;
            if (pc == null) return this;

            // fix this domain issue... not working for some reason
            PolyCurve pco = pc.OverBend(t);
            pco.Domain = pc.Domain;

            FreeformGlulam g = this.Duplicate() as FreeformGlulam;
            g.Centreline = pco;
            //g.RecalculateFrames();

            return g;
        }

        public abstract override GlulamType Type();

        public override double GetMaxCurvature(ref double width, ref double height)
        {
            double[] t = Centreline.DivideByCount(CurvatureSamples, false);
            double max_kw = 0.0, max_kh = 0.0, max_k = 0.0;
            Plane temp;
            Vector3d k;
            for (int i = 0; i < t.Length; ++i)
            {
                temp = GetPlane(t[i]);

                k = Centreline.CurvatureAt(t[i]);
                max_kw = Math.Max(max_kw, Math.Abs(k * temp.XAxis));
                max_kh = Math.Max(max_kh, Math.Abs(k * temp.YAxis));
                max_k = Math.Max(max_k, k.Length);
            }
            width = max_kw;
            height = max_kh;
            return max_k;
        }

        public override string ToString() => "FreeformGlulam";
        

        public override void ReduceTwist(double factor, bool start_with_first = true)
        {
            throw new Exception("TODO: Move to GlulamOrientation and refactor.");
            /*
            if (Frames.Count < 2) return;

            if (start_with_first)
            {
                Plane pframe;
                Centreline.PerpendicularFrameAt(Frames.First().Item1, out pframe);
                double angle = Vector3d.VectorAngle(Frames.First().Item2.YAxis, pframe.YAxis);
                double anglex = Vector3d.VectorAngle(Frames.First().Item2.YAxis, pframe.XAxis);
                if (anglex < 1.57) angle *= -1;

                for (int i = 1; i < Frames.Count; ++i)
                {
                    double t = Frames[i].Item1;
                    Plane p = Frames[i].Item2;

                    Centreline.PerpendicularFrameAt(Frames[i].Item1, out pframe);
                    double temp = Vector3d.VectorAngle(p.YAxis, pframe.YAxis);

                    temp = (temp - angle) * factor;
                    p.Transform(Rhino.Geometry.Transform.Rotation(temp, p.ZAxis, p.Origin));

                    Frames[i] = new Tuple<double, Plane>(t, p);
                }
            }
            */
        }

        public override Mesh MapToCurveSpace(Mesh m)
        {
            throw new NotImplementedException();
        }

        public override Curve CreateOffsetCurve(double x, double y, bool rebuild = false, int rebuild_pts = 20)
        {
            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(this.Data.Samples, true);


            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                pts.Add(p.Origin + p.XAxis * x + p.YAxis * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);

            if (new_curve == null)
                throw new Exception("FreeformGlulam::CreateOffsetCurve:: Failed to create interpolated curve!");

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }

        public override Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, bool rebuild = false, int rebuild_pts = 20)
        {
            if (!offset_start && !offset_end) return Centreline.DuplicateCurve();
            if (offset_start && offset_end) return CreateOffsetCurve(x, y, rebuild, rebuild_pts);

            List<Point3d> pts = new List<Point3d>();
            double[] t = Centreline.DivideByCount(this.Data.Samples, true);

            double tmin = offset_start ? t.First() : t.Last();
            double tmax = offset_end ? t.Last() : t.First();

            for (int i = 0; i < t.Length; ++i)
            {
                Plane p = GetPlane(t[i]);
                double l = Ease.QuadOut(Interpolation.Unlerp(tmin, tmax, t[i]));
                pts.Add(p.Origin + p.XAxis * l * x + p.YAxis * l * y);
            }

            Curve new_curve = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Uniform,
                Centreline.TangentAtStart, Centreline.TangentAtEnd);

            if (new_curve == null)
                throw new Exception(this.ToString() + "::CreateOffsetCurve:: Failed to create interpolated curve!");

            double len = new_curve.GetLength();
            new_curve.Domain = new Interval(0.0, len);

            if (rebuild)
                new_curve = new_curve.Rebuild(rebuild_pts, new_curve.Degree, true);

            return new_curve;
        }
    }
}
