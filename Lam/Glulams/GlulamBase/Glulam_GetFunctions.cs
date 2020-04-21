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

using Rhino.Geometry;
using tas.Core;
using tas.Core.Util;

using Rhino.Collections;

namespace tas.Lam
{
    public abstract partial class Glulam
    {

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

    }
}