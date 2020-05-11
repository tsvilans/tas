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
        /// <param name="frames">Output cross-section planes.</param>
        /// <param name="parameters">Output t-values along centreline curve.</param>
        /// <param name="interpolation">Type of interpolation to use (default is Linear).</param>
        public override void GenerateCrossSectionPlanes(int N, double extension, out Plane[] frames, out double[] parameters, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            // Experimental new way of generative cross-section planes
            Curve curve;
            if (extension > 0 && false)
                curve = Centreline.Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);
            else
                curve = Centreline;


            parameters = curve.DivideByCount(N - 1, true).ToArray();
            frames = new Plane[N];

            var vectors = Orientation.GetOrientations(curve, parameters);

            for (int i = 0; i < N; ++i)
            {
                frames[i] = Misc.PlaneFromNormalAndYAxis(
                    curve.PointAt(parameters[i]),
                    curve.TangentAt(parameters[i]),
                    vectors[i]);
            }

            return;

            N = Math.Max(N, 2);

            frames = new Plane[N];
            Curve CL;
            if (Centreline.IsClosed)
                CL = Centreline.DuplicateCurve();
            else
                CL = Centreline.Extend(CurveEnd.Both, extension, CurveExtensionStyle.Smooth);

            parameters = CL.DivideByCount(N - 1, true);

            GlulamOrientation TempOrientation = Orientation.Duplicate();
            TempOrientation.Remap(Centreline, CL);

            for (int i = 0; i < N; ++i)
            {
                Vector3d v = TempOrientation.GetOrientation(CL, parameters[i]);
                frames[i] = tas.Core.Util.Misc.PlaneFromNormalAndYAxis(CL.PointAt(parameters[i]), CL.TangentAt(parameters[i]), v);
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

            int N = Math.Max(Data.Samples, 6);

            GenerateCrossSectionPlanes(N, offset, out Plane[] frames, out double[] parameters, Data.InterpolationType);

            GetSectionOffset(out double offsetX, out double offsetY);
            Point3d[] m_corners = GenerateCorners();

            double hW = Data.NumWidth * Data.LamWidth / 2 + offset;
            double hH = Data.NumHeight * Data.LamHeight / 2 + offset;

            // vertex index and next frame vertex index
            int i4;
            int ii4;

            //double texLength = (Centreline.GetLength() + offset * 2) / 1000;
            //double MaxT = parameters.Last() - parameters.First();

            double texWidth = Data.NumWidth * Data.LamWidth / 1000;
            double texHeight = Data.NumHeight * Data.LamHeight / 1000;

            for (int i = 0; i < frames.Length; ++i)
            {
                i4 = i * 8;
                ii4 = i4 - 8;

                double texLength = Centreline.GetLength(
                  new Interval(Centreline.Domain.Min, parameters[i])) / 1000;

                for (int j = 0; j < m_corners.Length; ++j)
                {
                    Point3d v = frames[i].PointAt(m_corners[j].X, m_corners[j].Y);
                    m.Vertices.Add(v);
                    m.Vertices.Add(v);
                }

                //double DivV = parameters[i] / MaxT * Length / 1000;
                m.TextureCoordinates.Add(texLength, 2 * texWidth + 2 * texHeight);
                m.TextureCoordinates.Add(texLength, 0.0);

                m.TextureCoordinates.Add(texLength, texHeight);
                m.TextureCoordinates.Add(texLength, texHeight);

                m.TextureCoordinates.Add(texLength, 2 * texHeight + texWidth);
                m.TextureCoordinates.Add(texLength, 2 * texHeight + texWidth);

                m.TextureCoordinates.Add(texLength, texWidth + texHeight);
                m.TextureCoordinates.Add(texLength, texWidth + texHeight);


                if (i > 0)
                {
                    m.Faces.AddFace(i4 + 2,
                      ii4 + 2,
                      ii4 + 1,
                      i4 + 1);
                    m.Faces.AddFace(i4 + 5,
                      ii4 + 5,
                      ii4 + 3,
                      i4 + 3);
                    m.Faces.AddFace(i4 + 7,
                      ii4 + 7,
                      ii4 + 4,
                      i4 + 4);

                    m.Faces.AddFace(i4,
                      ii4,
                      ii4 + 7,
                      i4 + 7);
                }
            }

            Plane pplane;

            // Start cap
            pplane = frames.First();
            for (int j = 0; j < m_corners.Length; ++j)
                m.Vertices.Add(pplane.PointAt(m_corners[j].X, m_corners[j].Y));

            m.TextureCoordinates.Add(0, 0);
            m.TextureCoordinates.Add(0, texHeight);
            m.TextureCoordinates.Add(texWidth, 0);
            m.TextureCoordinates.Add(texWidth, texHeight);

            m.Faces.AddFace(m.Vertices.Count - 4,
              m.Vertices.Count - 3,
              m.Vertices.Count - 2,
              m.Vertices.Count - 1);

            // End cap
            pplane = frames.Last();
            for (int j = 0; j < m_corners.Length; ++j)
                m.Vertices.Add(pplane.PointAt(m_corners[j].X, m_corners[j].Y));

            m.TextureCoordinates.Add(0, 0);
            m.TextureCoordinates.Add(0, texHeight);
            m.TextureCoordinates.Add(texWidth, 0);
            m.TextureCoordinates.Add(texWidth, texHeight);

            m.Faces.AddFace(m.Vertices.Count - 1,
              m.Vertices.Count - 2,
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
            int N = Math.Max(Data.Samples, 6);

            GenerateCrossSectionPlanes(N, offset, out Plane[] frames, out double[] parameters, Data.InterpolationType);

            int numCorners = 4;
            GenerateCorners(offset);

            List<Point3d>[] crvPts = new List<Point3d>[numCorners];
            for (int i = 0; i < numCorners; ++i)
            {
                crvPts[i] = new List<Point3d>();
            }

            Transform xform;
            Point3d temp;

            for (int i = 0; i < N; ++i)
            {
                //frames[i] = frames[i].FlipAroundYAxis();
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, frames[i]);

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
            double[] DivParams = Centreline.DivideByCount(Data.Samples - 1, true);

            GetSectionOffset(out double offsetX, out double offsetY);

            Point3d[,,] AllPoints = new Point3d[Data.NumWidth + 1, Data.NumHeight + 1, DivParams.Length];
            Point3d[,] CornerPoints = new Point3d[Data.NumWidth + 1, Data.NumHeight + 1];

            for (int x = 0; x <= Data.NumWidth; ++x)
            {
                for (int y = 0; y <= Data.NumHeight; ++y)
                {
                    CornerPoints[x, y] = new Point3d(
                        -hW + offsetX + x * Data.LamWidth,
                        -hH + offsetY + y * Data.LamHeight,
                        0);
                }
            }

            GenerateCrossSectionPlanes(Math.Max(Data.Samples, 6), 0.0, out Plane[] frames, out double[] parameters, Data.InterpolationType);

            Transform xform;
            Point3d temp;
            for (int i = 0; i < frames.Length; ++i)
            {
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, frames[i]);

                for (int x = 0; x <= Data.NumWidth; ++x)
                {
                    for (int y = 0; y <= Data.NumHeight; ++y)
                    {
                        temp = new Point3d(CornerPoints[x, y]);
                        temp.Transform(xform);
                        AllPoints[x, y, i] = temp;
                    }
                }
            }

            Curve[,] EdgeCurves = new Curve[Data.NumWidth + 1, Data.NumHeight + 1];
            for (int x = 0; x <= Data.NumWidth; ++x)
            {
                for (int y = 0; y <= Data.NumHeight; ++y)
                {
                    Point3d[] pts = new Point3d[frames.Length];
                    for (int z = 0; z < frames.Length; ++z)
                    {
                        pts[z] = AllPoints[x, y, z];
                    }
                    EdgeCurves[x, y] = Curve.CreateInterpolatedCurve(pts, 3);
                }
            }

            List<Brep> LamellaBreps = new List<Brep>();

            for (int x = 0; x < Data.NumWidth; ++x)
            {
                for (int y = 0; y < Data.NumHeight; ++y)
                {
                    Curve[] edges = new Curve[8];
                    edges[4] = new Line(AllPoints[x, y, 0], AllPoints[x + 1, y, 0]).ToNurbsCurve();
                    edges[5] = new Line(AllPoints[x, y + 1, 0], AllPoints[x + 1, y + 1, 0]).ToNurbsCurve();

                    edges[6] = new Line(AllPoints[x, y, frames.Length - 1], AllPoints[x + 1, y, frames.Length - 1]).ToNurbsCurve();
                    edges[7] = new Line(AllPoints[x, y + 1, frames.Length - 1], AllPoints[x + 1, y + 1, frames.Length - 1]).ToNurbsCurve();

                    Brep[] sides = new Brep[6];

                    sides[0] = Brep.CreateFromLoft(
                          new Curve[] { EdgeCurves[x,y], EdgeCurves[x + 1, y] },
                          Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];
                    sides[1] = Brep.CreateFromLoft(
                          new Curve[] { EdgeCurves[x + 1, y], EdgeCurves[x + 1, y + 1] },
                          Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];
                    sides[2] = Brep.CreateFromLoft(
                      new Curve[] { EdgeCurves[x + 1, y + 1], EdgeCurves[x, y + 1] },
                      Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];
                    sides[3] = Brep.CreateFromLoft(
                      new Curve[] { EdgeCurves[x, y + 1], EdgeCurves[x, y] },
                      Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

                    sides[4] = Brep.CreateFromLoft(
                      new Curve[] { edges[4], edges[5] },
                      Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

                    sides[5] = Brep.CreateFromLoft(
                      new Curve[] { edges[6], edges[7] },
                      Point3d.Unset, Point3d.Unset, LoftType.Straight, false)[0];

                    Brep brep = Brep.JoinBreps(
                      sides,
                      Tolerance
                      )[0];

                    LamellaBreps.Add(brep);
                }
            }

            return LamellaBreps;
        }

        public override List<Mesh> GetLamellaMeshes() => base.GetLamellaMeshes();

        public override List<Curve> GetLamellaCurves()
        {
            int N = Math.Max(Data.Samples, 6);
            GenerateCrossSectionPlanes(N, 0.0, out Plane[] frames, out double[] parameters, Data.InterpolationType);

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

            GetSectionOffset(out double offsetX, out double offsetY);

            List<Point3d> LamellaPoints = new List<Point3d>();

            for (int x = 0; x < Data.Lamellae.GetLength(0); ++x)
            {
                for (int y = 0; y < Data.Lamellae.GetLength(1); ++y)
                {
                    LamellaPoints.Add(
                        new Point3d(
                            -hWidth + offsetX + hLw + x * Data.LamWidth,
                            -hHeight + offsetY + hLh + y * Data.LamHeight,
                            0));
                }
            }

            for (int i = 0; i < N; ++i)
            {
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, frames[i]);

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

        public override Glulam Overbend(double t)
        {
            PolyCurve pc = Centreline.DuplicateCurve() as PolyCurve;
            if (pc == null) return this;

            // fix this domain issue... not working for some reason
            PolyCurve pco = pc.OverBend(t);
            pco.Domain = pc.Domain;

            FreeformGlulam g = this.Duplicate() as FreeformGlulam;
            g.Centreline = pco;

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
        
        /// <summary>
        /// Attempt to reduce twist in a Glulam with a variable orientation. TODO: Move to GlulamOrientation.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="start_with_first"></param>
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

            GenerateCrossSectionPlanes(Math.Max(6, Data.Samples), 0.0, out Plane[] planes, out double[] parameters, Data.InterpolationType);

            for (int i = 0; i < planes.Length; ++i)
            {
                Plane p = planes[i];
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
