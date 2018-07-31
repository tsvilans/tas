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
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System.Drawing;
using GH_IO.Serialization;

namespace tas.Lam.GH
{

    public class GH_Glulam : GH_Goo<Glulam>, IGH_PreviewData
    {
        public GH_Glulam() { this.Value = null; }
        public GH_Glulam(GH_Glulam goo) { this.Value = goo.Value; }
        public GH_Glulam(Glulam native) { this.Value = native; }
        public override IGH_Goo Duplicate() => new GH_Glulam(this);
        public override bool IsValid => true;
        public override string TypeName => "GlulamGoo";
        public override string TypeDescription => "GlulamGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is Glulam)
            {
                Value = source as Glulam;
                return true;
            }
            else if (source is GH_Glulam)
            {
                Value = (source as GH_Glulam).Value;
                return true;
            }

            return false;
        }

        //public static implicit operator Glulam(GH_Glulam g) => g.Value;
        //public static implicit operator GH_Glulam(Glulam g) => new GH_Glulam(g);

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
            {
                object mesh = new GH_Mesh(Value.GetBoundingMesh(0, Value.Data.InterpolationType));

                target = (Q)mesh;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
            {
                object blank = new GH_Brep(Value.GetBoundingBrep());

                target = (Q)blank;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                object cl = new GH_Curve(Value.Centreline);
                target = (Q)cl;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(Lam.Glulam)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        public BoundingBox ClippingBox => Value.GetBoundingMesh(0, Value.Data.InterpolationType).GetBoundingBox(true);

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //args.Pipeline.DrawMeshShaded(Value.GetBoundingMesh(), args.Material);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }

        public override bool Write(GH_IWriter writer)
        {
            byte[] centrelineBytes = GH_Convert.CommonObjectToByteArray(Value.Centreline);
            writer.SetByteArray("centreline", 0, centrelineBytes);


            return base.Write(writer);
        }
    }
    
    public class GH_Assembly : GH_Goo<Assembly>, IGH_PreviewData
    {
        public GH_Assembly() { this.Value = null; }
        public GH_Assembly(GH_Assembly goo) { this.Value = goo.Value; }
        public GH_Assembly(Assembly native) { this.Value = native; }
        public override IGH_Goo Duplicate() => new GH_Assembly(this);
        public override bool IsValid => true;
        public override string TypeName => "GlulamAssemblyGoo";
        public override string TypeDescription => "GlulamAssemblyGoo";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is Assembly)
            {
                Value = source as Assembly;
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
            {
                Mesh[] meshes = Value.ToMesh();
                Mesh m = new Mesh();
                for (int i = 0; i < meshes.Length; ++i)
                {
                    m.Append(meshes[i]);
                }
                object mesh = new GH_Mesh(m);

                target = (Q)mesh;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(Assembly)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
            {
                Brep[] breps = Value.ToBrep();
                Brep b = new Brep();
                for (int i = 0; i < breps.Length; ++i)
                {
                    b.Append(breps[i]);
                }
                object brep = new GH_Brep(b);
                target = (Q)brep;
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        BoundingBox IGH_PreviewData.ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                Mesh[] meshes = Value.ToMesh();

                for (int i = 0; i < meshes.Length; ++i)
                {
                    box.Union(meshes[i].GetBoundingBox(true));
                }
                return box;
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //args.Pipeline.DrawMeshShaded(Value.GetBoundingMesh(), args.Material);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }
    }

    public class GH_GlulamWorkpiece : GH_Goo<GlulamWorkpiece>, IGH_PreviewData
    {
        public GH_GlulamWorkpiece() { this.Value = null; }
        public GH_GlulamWorkpiece(GH_GlulamWorkpiece goo) { this.Value = goo.Value; }
        public GH_GlulamWorkpiece(GlulamWorkpiece native) { this.Value = native; }
        public override IGH_Goo Duplicate() => new GH_GlulamWorkpiece(this);
        public override bool IsValid => true;
        public override string TypeName => "tasGlulamWorkpiece";
        public override string TypeDescription => "tasGlulamWorkpiece";
        public override string ToString() => Value.ToString(); //this.Value.ToString();
        public override object ScriptVariable() => Value;

        public override bool CastFrom(object source)
        {
            if (source is GlulamWorkpiece)
            {
                Value = source as GlulamWorkpiece;
                return true;
            }
            if (source is GH_GlulamWorkpiece)
            {
                Value = (source as GH_GlulamWorkpiece).Value;
                return true;
            }
            if (source is Lam.Glulam)
            {
                Value = new GlulamWorkpiece(new BasicAssembly(source as Lam.Glulam));
                return true;
            }
            if (source is GH_Glulam)
            {
                Value = new GlulamWorkpiece(new BasicAssembly((source as GH_Glulam).Value));
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
            {
                Mesh[] meshes = Value.GetMesh();
                Mesh m = new Mesh();
                for (int i = 0; i < meshes.Length; ++i)
                {
                    m.Append(meshes[i]);
                }
                object mesh = new GH_Mesh(m);

                target = (Q)mesh;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GlulamWorkpiece)))
            {
                object blank = Value;
                target = (Q)blank;
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
            {
                Brep[] breps = Value.GetBrep();
                Brep b = new Brep();
                for (int i = 0; i < breps.Length; ++i)
                {
                    b.Append(breps[i]);
                }
                object brep = new GH_Brep(b);
                target = (Q)brep;
                return true;
            }
            //if (typeof(Q).IsAssignableFrom(typeof(GH_)))
            if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
            {
                Curve[] crvs = Value.Blank.GetAllGlulams().Select(x => x.Centreline).ToArray();
                //target = crvs.Select(x => new GH_Curve(x)).ToList() as Q;
                object crv = new GH_Curve(crvs.FirstOrDefault());
                target = (Q)(crv);
                return true;
            }
            if (typeof(Q).IsAssignableFrom(typeof(List<GH_Curve>)))
            {
                Curve[] crvs = Value.Blank.GetAllGlulams().Select(x => x.Centreline).ToArray();
                //target = crvs.Select(x => new GH_Curve(x)).ToList() as Q;
                object crv = crvs.Select(x => new GH_Curve(x)).ToList();
                target = (Q)(crv);
                return true;
            }

            return base.CastTo<Q>(ref target);
        }

        BoundingBox IGH_PreviewData.ClippingBox
        {
            get
            {
                BoundingBox box = BoundingBox.Empty;

                Mesh[] meshes = Value.GetMesh();

                for (int i = 0; i < meshes.Length; ++i)
                {
                    box.Union(meshes[i].GetBoundingBox(true));
                }
                return box;
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //args.Pipeline.DrawMeshShaded(Value.GetBoundingMesh(), args.Material);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
        }
    }
    
}