/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
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
using tas.Core.Types;

namespace tas.Lam
{
    public abstract class FeatureX
    {
        public Guid ID { get; protected set; }
        public string Name;

        public FeatureX()
        {
            ID = Guid.NewGuid();
        }

        protected List<Brep> m_result;

        public abstract bool Compute();
        public List<Brep> GetCuttingGeometry()
        {
            if (m_result != null)
                return m_result;
            else
                throw new Exception("Cutting geometry has not been computed!");
        }

    }

    public abstract class Feature
    {
        public Guid ID { get; protected set; }
        public string Name;

        //public static ToolSettings DefaultTool;

        public Plane BasePlane;
        public GeometryBase Geo;

        public Feature()
        {
            ID = Guid.NewGuid();
        }

        public abstract GeometryBase GetGeometry();
        public abstract Mesh[] GetCutterMeshes(GlulamAssembly Beam);
        public abstract Brep[] GetCutterBreps(GlulamAssembly Beam);
        public abstract GlulamAssembly[] ConnectedAssemblies();
        //public abstract OrientedPolyline[] GetToolpaths(Assembly Beam, ToolSettings Tool);

        /// <summary>
        /// This is called to update any feature planes or data that depend on the connected Assemblies.
        /// </summary>
        public abstract void Update();

        public virtual void Transform(Transform x)
        {
            BasePlane.Transform(x);
            if (Geo != null)
                Geo.Transform(x);
        }

        static public implicit operator Feature(FeatureProxy fp)
        {
            return fp.Data;
        }
    }

    public class FeatureProxy
    {
        public Feature Data;
        public Plane Plane;

        public FeatureProxy(Feature f, Plane p)
        {
            Data = f;
            this.Plane = p;
        }

        public void Transform(Transform x)
        {
            if (!this.Plane.Transform(x))
                throw new Exception("Could not transform.");
        }
    }
/*
    public enum JointCondition
    {
        XJoint,
        TJoint,
        VJoint,
        Overlap,
        Split,
        Butt,
        Unset
    }
*/

}
