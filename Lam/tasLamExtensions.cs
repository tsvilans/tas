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
    public static class GlulamExtensionMethods
    {
        /// <summary>
        /// Calculate the normal deviation from the longitudinal direction of a glulam blank per vertex or mesh face. Can use acos to get the actual angle.
        /// </summary>
        /// <param name="mesh">Input mesh</param>
        /// <param name="curve">Curve to get tangent</param>
        /// <param name="faces">Calculate for faces instead of vertices</param>
        /// <returns>List of deviations between 0 and 1 for each mesh vertex or face.</returns>
        public static List<double> CalculateTangentDeviation(this Mesh mesh, Curve curve, bool faces = false)
        {
            List<double> deviations = new List<double>();

            if (faces)
            {
                mesh.FaceNormals.ComputeFaceNormals();

                Vector3d tangent = Vector3d.Unset;
                double t = 0.0;

                for (int i = 0; i < mesh.Faces.Count; ++i)
                {
                    curve.ClosestPoint(mesh.Faces.GetFaceCenter(i), out t);
                    tangent = curve.TangentAt(t);

                    deviations.Add(Math.Abs(tangent * mesh.FaceNormals[i]));
                }
            }
            else
            {
                mesh.Normals.ComputeNormals();

                Vector3d tangent = Vector3d.Unset;
                double t = 0.0;

                for (int i = 0; i < mesh.Vertices.Count; ++i)
                {
                    curve.ClosestPoint(mesh.Vertices[i], out t);
                    tangent = curve.TangentAt(t);

                    deviations.Add(Math.Abs(tangent * mesh.Normals[i]));
                }
            }

            return deviations;
        }

        public static List<Vector3d> CalculateTangentVector(this Mesh mesh, Curve curve, bool faces = false)
        {
            List<Vector3d> l_vectors = new List<Vector3d>();

            Point3d cp = Point3d.Unset;
            double t = 0.0;

            if (faces)
            {
                for (int i = 0; i < mesh.Faces.Count; ++i)
                {
                    curve.ClosestPoint(mesh.Faces.GetFaceCenter(i), out t);
                    l_vectors.Add(curve.TangentAt(t));
                }
            }
            else
            {
                for (int i = 0; i < mesh.Vertices.Count; ++i)
                {
                    curve.ClosestPoint(mesh.Vertices[i], out t);
                    l_vectors.Add(curve.TangentAt(t));
                }
            }

            return l_vectors;
        }

    }
}
