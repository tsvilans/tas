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

        public abstract void GenerateCrossSectionPlanes(int N, double extension, out Plane[] planes, out double[] t, GlulamData.Interpolation interpolation = GlulamData.Interpolation.LINEAR);


    }
}