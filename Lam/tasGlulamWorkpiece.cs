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

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using tas.Core;

namespace tas.Lam
{
    [Serializable]
    public class GlulamWorkpiece
    {
        #region Static variables
        public static double GlobalTolerance = 1.0;
        public static double GlobalClearance = 6.0;
        #endregion

        public Guid Id { get; protected set; }

        public string Name;
        public Assembly Blank;
        public List<FeatureProxy> Features;
        public Plane Plane;

        public List<Guid> ChildObjects;

        public int RFID;
        public GlulamWorkpiece(GlulamWorkpiece wp)
        {
            Id = Guid.NewGuid();
            Name = wp.Name;
            Blank = wp.Blank.Duplicate();
            Plane = wp.Plane;

            ChildObjects = new List<Guid>();
            foreach (Guid id in wp.ChildObjects)
            {
                ChildObjects.Add(id);
            }
            Features = new List<FeatureProxy>();
            foreach (FeatureProxy f in wp.Features)
            {
                Features.Add(f);
            }
        }

        public GlulamWorkpiece(Assembly blank) : this(blank, "GlulamWorkpiece")
        {
            
        }

        public GlulamWorkpiece(Assembly blank, string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Blank = blank;
            ChildObjects = new List<Guid>();
            Features = new List<FeatureProxy>();
        }

        public void Transform(Transform x)
        {
            Blank.Transform(x);
            Plane.Transform(x);

            foreach (Guid id in ChildObjects)
            {
                Rhino.DocObjects.RhinoObject obj = Rhino.RhinoDoc.ActiveDoc.Objects.Find(id);
                if (obj == null) continue;
                obj.Geometry.Transform(x);
            }
            foreach (FeatureProxy f in Features)
            {
                f.Transform(x);
            }
        }

        public override string ToString()
        {
            return "GlulamWorkpiece(\"" + this.Name + "\")";
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == this.GetType() && obj.GetHashCode() == this.GetHashCode())
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public GlulamWorkpiece Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as GlulamWorkpiece;
        }

        public GlulamWorkpiece Duplicate()
        {
            return new GlulamWorkpiece(this);
        }

        public Mesh[] GetMesh()
        {
            List<Mesh> Cutters = new List<Mesh>();
            for (int i = 0; i < Features.Count; ++i)
            {
                Cutters.AddRange(((Feature)Features[i]).GetCutterMeshes(this.Blank));
            }

            //Mesh[] Cuts = Mesh.CreateBooleanDifference(Blank.ToMesh(), Cutters); // OLD VERSION
            Mesh[] BlankMesh = Blank.ToMesh();
//            Mesh[] Cuts = Util.Carve(BlankMesh, Cutters, CarveSharp.CarveSharp.CSGOperations.AMinusB).ToArray();
            Mesh[] Cuts = null;


            if (Cuts != null && Cuts.Length > 0)
            {
                return Cuts.ToArray();
            }
            else
                return BlankMesh;
        }

        public Brep[] GetBrep()
        {
            List<Brep> Cutters = new List<Brep>();
            for (int i = 0; i < Features.Count; ++i)
            {
                Cutters.AddRange(((Feature)Features[i]).GetCutterBreps(this.Blank));
            }

            Brep[] BlankBrep = Blank.ToBrep();
            Brep[] Cuts = Rhino.Geometry.Brep.CreateBooleanDifference(BlankBrep, Cutters, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);

            if (Cuts != null && Cuts.Length > 0)
            {
                return Cuts.ToArray();
            }
            else
                return BlankBrep;
        }

        public Mesh[] GetBlankMesh()
        {
            return Blank.GetAllGlulams().Select(x => x.GetBoundingMesh()).ToArray();
        }

        #region STATIC HELPER METHODS

        /*
        /// <summary>
        /// Basic method to find either lap joint or splice joint conditions. Only works 
        /// if the workpiece is built on a DirectedAssembly (has a Centreline).
        /// </summary>
        /// <param name="wpA">First workpiece to compare.</param>
        /// <param name="wpB">Second workpiece to compare.</param>
        /// <param name="MaxSearchDistance">Maximum distance within which to search for joint.</param>
        public static List<object> FindJointConditions(GlulamWorkpiece wpA, GlulamWorkpiece wpB, double MaxSearchDistance = 100.0)
        {
            double JCEndTolerance = 0.1;

            DirectedAssembly asA = wpA.Blank as DirectedAssembly;
            DirectedAssembly asB = wpB.Blank as DirectedAssembly;
            if (asA == null || asB == null) return new List<object>();

            List<object> debug = new List<object>();

            for (int ca = 0; ca < asA.Centrelines.Length; ++ca)
            {
                for (int cb = 0; cb < asB.Centrelines.Length; ++cb)
                {
                    CurveIntersections cis = Rhino.Geometry.Intersect.Intersection.CurveCurve(asA.Centrelines[ca], asB.Centrelines[cb], MaxSearchDistance, 1.0);

                    if (cis == null || cis.Count < 1) continue;

                    foreach (IntersectionEvent ie in cis)
                    {
                        JointCondition jc = JointCondition.Unset;

                        bool endA = false, endB = false;

                        if (ie.IsOverlap)
                            jc = JointCondition.Overlap;
                        else
                        {
                            endA = Math.Abs(ie.ParameterA - asA.Centrelines[ca].Domain.Min) < JCEndTolerance || 
                                Math.Abs(ie.ParameterA - asA.Centrelines[ca].Domain.Max) < JCEndTolerance;
                            endB = Math.Abs(ie.ParameterB - asB.Centrelines[cb].Domain.Min) < JCEndTolerance || 
                                Math.Abs(ie.ParameterB - asB.Centrelines[cb].Domain.Max) < JCEndTolerance;

                            if (endA && endB)
                                jc = JointCondition.VJoint;
                            else if (endA || endB)
                                jc = JointCondition.TJoint;
                            else
                                jc = JointCondition.XJoint;
                        }

                        // TODO: This needs to be more elegant.
                        if (jc == JointCondition.Overlap) // Splice joint
                        {
                            double overlap_length = asA.Centrelines[ca].GetLength(ie.OverlapA);

                            Point3d mid = (ie.PointA + ie.PointA2) / 2;
                            double midt = ie.OverlapA.Mid;
                            bool Flip = Math.Abs(ie.OverlapA.Min - asA.Centrelines[ca].Domain.Min) < Math.Abs(asA.Centrelines[ca].Domain.Max - ie.OverlapA.Max);

                            Plane midp = asA.GetPlane(midt, ca);
                            debug.Add(midp);
                            Plane XP = new Plane(mid, midp.ZAxis, midp.XAxis);

                            if (Flip)
                                XP = XP.FlipAroundYAxis();

                            XP.Transform(Rhino.Geometry.Transform.Rotation(0.15, XP.YAxis, XP.Origin));

                            Glulam blankA = asA.GetSubElement(ca);
                            Glulam blankB = asB.GetSubElement(cb);

                            double joint_width = Math.Max(blankA.Data.LamWidth * blankA.Data.NumWidth, blankB.Data.LamWidth * blankB.Data.NumWidth);

                            SpliceJoint sj = new SpliceJoint(wpA.Blank, wpB.Blank, XP);
                            sj.Width = joint_width;
                            sj.Height = 140.0;
                            sj.Overlap = 120.0;
                            sj.MeshResolution = 4;

                            wpA.Features.Add(new FeatureProxy(sj, XP));
                            wpB.Features.Add(new FeatureProxy(sj, XP));
                            debug.Add("SpliceJoint");
                        }
                        else if (jc == JointCondition.XJoint) // Lap joint or T-joint (temporarily)
                        {
                            Point3d pA = ie.PointA, pB = ie.PointB;

                            if (pA.DistanceTo(pB) > MaxSearchDistance) continue;

                            Plane jpA = asA.GetPlane(ie.ParameterA);
                            Plane jpB = asB.GetPlane(ie.ParameterB);

                            bool FlipWidths = false;

                            Plane XP;
                            if (Math.Abs(jpA.XAxis * jpB.ZAxis) > Math.Abs(jpA.YAxis * jpB.ZAxis))
                                XP = new Plane((jpA.Origin + jpB.Origin) / 2, jpA.ZAxis, jpA.XAxis);
                            else
                            {
                                XP = new Plane((jpA.Origin + jpB.Origin) / 2, jpA.ZAxis, jpA.YAxis);
                                FlipWidths = true;
                            }

                            if ((jpB.Origin - jpA.Origin) * XP.ZAxis < 0.0)
                            {
                                XP = new Plane(XP.Origin, -XP.XAxis, XP.YAxis);
                            }

                            LapJoint lj = new LapJoint(asA,
                                asB, XP);
                            lj.Tolerance = GlobalTolerance;
                            lj.Clearance = GlobalClearance;

                            if (jc == JointCondition.TJoint)
                                lj.Name = "TJoint";

                            double Width, Height;
                            if (asA.TryGetWidthAndHeight(out Width, out Height))
                            {
                                lj.BeamAHeight = Height;
                                lj.BeamAWidth = Width - 2.0;
                            }
                            if (asB.TryGetWidthAndHeight(out Width, out Height))
                            {
                                lj.BeamBHeight = Height;
                                lj.BeamBWidth = Width - 2.0;
                            }

                            if (FlipWidths)
                            {
                                double temp = lj.BeamBHeight;
                                lj.BeamBHeight = lj.BeamBWidth;
                                lj.BeamBWidth = temp;

                                temp = lj.BeamAHeight;
                                lj.BeamAHeight = lj.BeamAWidth;
                                lj.BeamAWidth = temp;
                            }

                            wpA.Features.Add(new FeatureProxy(lj, XP));
                            wpB.Features.Add(new FeatureProxy(lj, XP));
                            debug.Add("LapJoint");
                        }
                        else if (jc == JointCondition.TJoint)
                        {
                            double w, h;
                            if (endA)
                            {
                                Plane XP = asA.GetPlane(ie.ParameterA);
                                if (ie.ParameterA - asA.Centrelines[ca].Domain.Min > asA.Centrelines[ca].Domain.Max - ie.ParameterA)
                                    XP = XP.FlipAroundYAxis();

                                asB.TryGetWidthAndHeight(out w, out h);
                                XP.Origin = XP.Origin + XP.ZAxis * w / 2;

                                FootJoint fj = new FootJoint(asA, XP);

                                wpA.Features.Add(new FeatureProxy(fj, XP));
                                debug.Add("TJoint");
                            }
                            else if (endB)
                            {
                                Plane XP = asB.GetPlane(ie.ParameterB);

                                if (ie.ParameterB - asB.Centrelines[cb].Domain.Min > asB.Centrelines[cb].Domain.Max - ie.ParameterB)
                                    XP = XP.FlipAroundYAxis();

                                asA.TryGetWidthAndHeight(out w, out h);
                                XP.Origin = XP.Origin + XP.ZAxis * w / 2;

                                FootJoint fj = new FootJoint(asB, XP);

                                wpB.Features.Add(new FeatureProxy(fj, XP));
                                debug.Add("TJoint");
                            }
                            else
                                throw new Exception("GlulamWorkpiece::FindJointConditions:: This should not have happened...");
                        }
                    }
                }
            }

            return debug;
        }
        */

        /*

        /// <summary>
        /// Test method for adding test joints. This simulates adding a pocket and some drilled holes
        /// (diamater: 10mm, depth: 30mm) from a plane that is offset 25mm from the Centreline. Once
        /// again only works with a workpiece built on a DirectedAssembly, though chould probably
        /// be extended to work with any Assembly if I just wasn't so goddamn lazy.
        /// </summary>
        /// <param name="wp">Workpiece to add TestJoints to.</param>
        /// <param name="holes">A list of points which will be turned into 2d points and 
        /// repeated for each TestJoint.</param>
        /// <param name="N">Number of TestJoints. The Centreline will be divided into this 
        /// many segments, with TestJoints at the split points.</param>
        /// <param name="outline">Optional 2d pocket outline. This will be 'faced' from 
        /// the workpiece.</param>
        public static void CreateTestFeatures(GlulamWorkpiece wp, List<Point3d> holes, int N = 3, Polyline outline = null)
        {
            DirectedAssembly da = wp.Blank as DirectedAssembly;
            if (da == null) return;

            for (int ca = 0; ca < da.Centrelines.Length; ++ca)
            {

                double[] test_t = da.Centrelines[ca].DivideByCount(N, false);
                if (test_t == null || test_t.Length < 1) return;

                foreach (double t in test_t)
                {
                    TestJoint jt = new TestJoint();
                    jt.Diameter = 10.0;
                    jt.Depth = 30.0;
                    if (outline != null)
                        jt.Outline = outline;
                    jt.Holes = holes.Select(p => new Point2d(p.X, p.Y)).ToList();

                    Plane test_p = da.GetPlane(t);

                    test_p = new Plane(test_p.Origin + test_p.YAxis * 25.0, test_p.ZAxis, test_p.XAxis);
                    jt.BasePlane = test_p;

                    wp.Features.Add(new FeatureProxy(jt, test_p));
                }
            }
        }
        */

        #endregion

    }

#if false
    #region NEW_Workpiece_definitions

    public class WorkStep
    {
        List<WorkStep> Children;
        List<Operation> Operations;
        List<object> Result;
        object Source;
        
        public WorkStep()
        {
            Children = new List<WorkStep>();
            Operations = new List<Operation>();
            Result = new List<object>();
        }

        public void BuildSource()
        {

        }

        public void BuildResult()
        {

        }

        public List<WorkStep> CollectChildren()
        {
            List<WorkStep> ws = new List<WorkStep>();
            for (int i = 0; i < Children.Count; ++i)
            {
                ws.AddRange(Children[i].CollectChildren());
            }
            return ws;
        }
    }

    public abstract class Operation
    {
        public Brep Driver;
        public abstract void Execute();

    }

    #endregion
#endif
}
