﻿/*
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
using tas.Core;

using Rhino.Geometry;

namespace tas.Lam
{
    class StraightGlulam : Glulam
    {
        public StraightGlulam(Curve centreline, Plane[] planes) : base()
        {
            Plane plane;
            if (planes == null || planes.Length < 1)
                centreline.PerpendicularFrameAt(centreline.Domain.Min, out plane);
            else
                plane = planes[0];

            if (!centreline.IsLinear(Tolerance)) throw new Exception("StraightGlulam only works with a linear centreline!");
            //Line l = new Line(centreline.PointAtStart, centreline.PointAtEnd);

            plane.Origin = centreline.PointAtStart;
            if (Math.Abs(plane.ZAxis * centreline.TangentAtStart) < 0)
                plane = plane.FlipAroundYAxis();
            if (Math.Abs(plane.ZAxis * centreline.TangentAtStart) < 0.999)
            {
                plane.Transform(Rhino.Geometry.Transform.Rotation(plane.ZAxis, centreline.TangentAtStart, plane.Origin));
            }

            Frames = new List<Tuple<double, Plane>>() { new Tuple<double, Plane>(centreline.Domain.Min, plane) };
            Centreline = centreline;
            RecalculateFrames();
        }

        public StraightGlulam(Curve centreline) : base()
        {
            Centreline = centreline;
            Plane p;
            Centreline.PerpendicularFrameAt(Centreline.Domain.Min, out p);
            Frames = new List<Tuple<double, Plane>>() { new Tuple<double, Plane>(centreline.Domain.Min, p) };
        }

        public override void GenerateCrossSectionPlanes(int N, double offset, out Plane[] planes, out double[] t, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            Curve CL = Centreline.Extend(CurveEnd.Both, offset, CurveExtensionStyle.Line);
            t = new double[] { CL.Domain.Min, CL.Domain.Max };
            planes = new Plane[] { GetPlane(t[0]), GetPlane(t[1]) };
        }

        public override Mesh GetBoundingMesh(double offset = 0.0, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR)
        {
            //Curve CL = Centreline.Extend(CurveEnd.Both, offset, CurveExtensionStyle.Line);

            Mesh m = new Mesh();
            double[] DivParams;
            Plane[] xPlanes;
            GenerateCrossSectionPlanes(0, offset, out xPlanes, out DivParams, interpolation);

            double hW = Data.NumWidth * Data.LamWidth / 2 + offset;
            double hH = Data.NumHeight * Data.LamHeight / 2 + offset;

            Plane pplane;

            // vertex index and next frame vertex index
            int i4;
            int ii4;

            double Length = Centreline.GetLength() + offset * 2;
            double MaxT = DivParams.Last() - DivParams.First();
            double Width = Data.NumWidth * Data.LamWidth / 1000;
            double Height = Data.NumHeight * Data.LamHeight / 1000;


            for (int i = 0; i < DivParams.Length; ++i)
            {
                i4 = i * 8;
                ii4 = i4 - 8;

                pplane = xPlanes[i];

                for (int j = -1; j <= 1; j += 2)
                {
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Point3d v = pplane.Origin + hW * j * pplane.XAxis + hH * k * pplane.YAxis;
                        m.Vertices.Add(v);
                        m.Vertices.Add(v);
                    }
                }

                //double DivV = DivParams[i] / MaxT;
                double DivV = DivParams[i] / MaxT * Length / 1000;
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

            // Start cap
            pplane = GetPlane(DivParams.First());
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
            pplane = GetPlane(DivParams.Last());
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

            m.Vertices.CullUnused();
            m.Compact();

            m.UserDictionary.ReplaceContentsWith(GetArchivableDictionary());
            return m;
        }

        public override Brep GetBoundingBrep(double offset = 0.0)
        {
            Curve CL = Centreline.Extend(CurveEnd.Both, offset, CurveExtensionStyle.Line);
            double Length = CL.GetLength();
            double hW = Data.NumWidth * Data.LamWidth / 2 + offset;
            double hH = Data.NumHeight * Data.LamHeight / 2 + offset;
            double[] DivParams = new double[] { CL.Domain.Min, CL.Domain.Max };

            Curve[][] LoftCurves = new Curve[4][];

            for (int i = 0; i < 4; ++i)
                LoftCurves[i] = new Curve[2];

            Rhino.Geometry.Transform xform;
            Line l1 = new Line(new Point3d(-hW, hH, 0), new Point3d(hW, hH, 0));
            Line l2 = new Line(new Point3d(hW, hH, 0), new Point3d(hW, -hH, 0));
            Line l3 = new Line(new Point3d(hW, -hH, 0), new Point3d(-hW, -hH, 0));
            Line l4 = new Line(new Point3d(-hW, -hH, 0), new Point3d(-hW, hH, 0));
            Line temp;

            for (int i = 0; i < DivParams.Length; ++i)
            {
                Plane p = GetPlane(DivParams[i]);
                xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, p);
                temp = l1; temp.Transform(xform);
                LoftCurves[0][i] = temp.ToNurbsCurve();
                temp = l2; temp.Transform(xform);
                LoftCurves[1][i] = temp.ToNurbsCurve();
                temp = l3; temp.Transform(xform);
                LoftCurves[2][i] = temp.ToNurbsCurve();
                temp = l4; temp.Transform(xform);
                LoftCurves[3][i] = temp.ToNurbsCurve();
            }

            Brep brep = new Brep();

            for (int i = 0; i < 4; ++i)
            {
                Brep[] loft = Brep.CreateFromLoft(LoftCurves[i], Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                if (loft == null || loft.Length < 1)
                    continue;
                for (int j = 0; j < loft.Length; ++j)
                    brep.Append(loft[j]);
            }

            brep.JoinNakedEdges(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            brep = brep.CapPlanarHoles(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            brep.UserDictionary.ReplaceContentsWith(GetArchivableDictionary());
            return brep;

        }

        public override List<Brep> GetLamellaBreps()
        {
            double Length = Centreline.GetLength();
            double hW = Data.NumWidth * Data.LamWidth / 2;
            double hH = Data.NumHeight * Data.LamHeight / 2;
            double[] DivParams = new double[] { Centreline.Domain.Min, Centreline.Domain.Max };

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
                    Brep[] brep = Brep.CreateFromLoft(LoftCurves[i,j], Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                    if (brep != null && brep.Length > 0)
                        LamellaBreps.Add(brep[0].CapPlanarHoles(Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance));
                }
            }
            return LamellaBreps;
        }

        public override double GetMaxCurvature(ref double width, ref double height)
        {
            width = 0.0;
            height = 0.0;
            return 0.0;
        }

        public override List<Mesh> GetLamellaMeshes()
        {
            return base.GetLamellaMeshes();
        }

        public override List<Curve> GetLamellaCurves()
        {
            List<Curve> lam_crvs = new List<Curve>();

            double hWidth = Data.NumWidth * Data.LamWidth / 2;
            double hHeight = Data.NumHeight * Data.LamHeight / 2;
            Plane plane = Frames[0].Item2;

            double hLw = Data.LamWidth / 2;
            double hLh = Data.LamHeight / 2;

            Transform xform = Rhino.Geometry.Transform.PlaneToPlane(Plane.WorldXY, plane);

            for (int i = 0; i < Data.NumHeight; ++i)
            {
                for (int j = 0; j < Data.NumWidth; ++j)
                {
                    Point3d p = new Point3d(j * Data.LamWidth - hWidth + hLw, i * Data.LamHeight - hHeight + hLh, 0.0);
                    Line l = new Line(p, Vector3d.ZAxis * Centreline.GetLength());
                    l.Transform(xform);

                    lam_crvs.Add(l.ToNurbsCurve());
                }
            }

            return lam_crvs;
        }

        public override Plane GetPlane(double t)
        {
            Plane p = Frames[0].Item2;
            p.Origin = Centreline.PointAt(t);

            return p;
        }

        public override Tuple<Plane, Plane, double> FramesAround(double t)
        {
            return new Tuple<Plane, Plane, double>(Frames[0].Item2, Frames[0].Item2, 0.0);
        }

        public override void Transform(Transform x)
        {
            Centreline.Transform(x);
            for (int i = 0; i < Frames.Count; ++i)
            {
                Frames[i].Item2.Transform(x);
            }
        }

        public override GlulamType Type()
        {
            return GlulamType.Straight;
        }

        public override string ToString()
        {
            return "StraightGlulam";
        }

        public override void ReduceTwist(double factor, bool start_with_first = true)
        {
            return;
        }

        public override Mesh MapToCurveSpace(Mesh m)
        {
            Mesh mesh = m.DuplicateMesh();
            Plane p = new Plane(Frames.First().Item2);

            p.Origin = Centreline.PointAtStart;

            mesh.Transform(Rhino.Geometry.Transform.PlaneToPlane(p, Plane.WorldXY));
            return mesh;
        }

        public override bool InKLimitsComponent(out bool width, out bool height)
        {
            width = height = true;
            return true;
        }

        public override Curve CreateOffsetCurve(double x, double y, bool rebuild = false, int rebuild_pts = 20)
        {
            Plane p = Frames.First().Item2;
            //p.Origin = Centreline.PointAtStart;
            Curve copy = Centreline.DuplicateCurve();
            copy.Transform(Rhino.Geometry.Transform.Translation(p.XAxis * x + p.YAxis * y));
            return copy;
        }

        public override Curve CreateOffsetCurve(double x, double y, bool offset_start, bool offset_end, bool rebuild = false, int rebuild_pts = 20)
        {
            throw new NotImplementedException();
        }
    }
}