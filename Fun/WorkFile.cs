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

using tas.Lam;
using Rhino.Geometry;
using Rhino.DocObjects;
using System.Drawing;
using Rhino;

namespace tas.ESR2
{
    public class WorkFile
    {
        public string Name;
        private Glulam g = null;
        private Brep beam = null;

        // list of cutting surfaces
        public List<Brep> Cuts_Surface;
        public List<Brep> Cuts_GlueSurface;
        public List<Brep> Cuts_Joint;
        public List<Line> Drills_Joint;

        public WorkFile(string name)
        {
            Name = name;

            Cuts_Surface = new List<Brep>();
            Cuts_GlueSurface = new List<Brep>();
            Cuts_Joint = new List<Brep>();

            Drills_Joint = new List<Line>();
        }

        public void SetGlulam(Glulam glulam)
        {
            g = glulam.Duplicate();
        }

        public void SetBeam(Brep b)
        {
            beam = b.Duplicate() as Brep;
        }

        public void Write(string path)
        {
            if (!System.IO.Directory.Exists(path))
                throw new Exception("Path does not exist.");

            if (g == null || beam == null)
                throw new Exception("Must add Glulam and Beam geometry!");

            Rhino.FileIO.File3dm file = new Rhino.FileIO.File3dm();

            // Setup layers
            Layer lRoot = new Layer();
            lRoot.Name = "tas";
            file.AllLayers.Add(lRoot);
            lRoot = file.AllLayers.Last();

            Layer lNC = new Layer();
            lNC.Name = "NC";
            lNC.IsVisible = false;
            lNC.IsLocked = true;
            file.AllLayers.Add(lNC);
            lNC = file.AllLayers.Last();
            lNC.ParentLayerId = lRoot.Id;

            Layer lBL = new Layer();
            lBL.Name = "BL";
            lBL.IsLocked = true;
            file.AllLayers.Add(lBL);
            lBL = file.AllLayers.Last();
            lBL.ParentLayerId = lRoot.Id;

            Layer lBE = new Layer();
            lBE.Name = "BE";
            lBE.IsLocked = true;
            file.AllLayers.Add(lBE);
            lBE = file.AllLayers.Last();
            lBE.ParentLayerId = lRoot.Id;

            Layer lBL_Centreline = new Layer();
            lBL_Centreline.Name = "BL_Centreline";
            lBL_Centreline.Index = 1;
            lBL_Centreline.Color = Color.FromArgb(0, 0, 255);
            lBL_Centreline.ParentLayerId = lBL.Id;
            file.AllLayers.Add(lBL_Centreline);
            lBL_Centreline = file.AllLayers.Last();

            Layer lBL_Geometry = new Layer();
            lBL_Geometry.Name = "BL_Geometry";
            lBL_Geometry.Color = Color.FromArgb(125, 200, 125);
            lBL_Geometry.ParentLayerId = lBL.Id;
            file.AllLayers.Add(lBL_Geometry);
            lBL_Geometry = file.AllLayers.Last();

            Layer lBL_Safety = new Layer();
            lBL_Safety.Name = "BL_Safety";
            lBL_Safety.IsVisible = false;
            lBL_Safety.Color = Color.FromArgb(200, 200, 200);
            lBL_Safety.ParentLayerId = lBL.Id;
            file.AllLayers.Add(lBL_Safety);
            lBL_Safety = file.AllLayers.Last();

            Layer lBE_Geometry = new Layer();
            lBE_Geometry.Name = "BE_Geometry";
            lBE_Geometry.Color = Color.FromArgb(100, 100, 100);
            lBE_Geometry.ParentLayerId = lBE.Id;
            file.AllLayers.Add(lBE_Geometry);
            lBE_Geometry = file.AllLayers.Last();

            Layer lNC_Surface = new Layer();
            lNC_Surface.Name = "NC_Surface";
            lNC_Surface.Color = Color.FromArgb(255, 0, 255);
            lNC_Surface.ParentLayerId = lNC.Id;
            file.AllLayers.Add(lNC_Surface);
            lNC_Surface = file.AllLayers.Last();

            Layer lNC_Normals = new Layer();
            lNC_Normals.Name = "NC_SurfaceNormals";
            lNC_Normals.Color = Color.FromArgb(255, 0, 150);
            lNC_Normals.ParentLayerId = lNC.Id;
            file.AllLayers.Add(lNC_Normals);
            lNC_Normals = file.AllLayers.Last();

            Layer lNC_GlueFace = new Layer();
            lNC_GlueFace.Name = "NC_GlueFace";
            lNC_GlueFace.Color = Color.FromArgb(255, 0, 255);
            lNC_GlueFace.ParentLayerId = lNC.Id;
            file.AllLayers.Add(lNC_GlueFace);
            lNC_GlueFace = file.AllLayers.Last();

            Layer lNC_Joints = new Layer();
            lNC_Joints.Name = "NC_Joints";
            lNC_Joints.Color = Color.FromArgb(255, 0, 255);
            lNC_Joints.ParentLayerId = lNC.Id;
            file.AllLayers.Add(lNC_Joints);
            lNC_Joints = file.AllLayers.Last();

            Layer lNC_Drill = new Layer();
            lNC_Drill.Name = "NC_Drill";
            lNC_Drill.Color = Color.FromArgb(255, 0, 0);
            lNC_Drill.ParentLayerId = lNC.Id;
            file.AllLayers.Add(lNC_Drill);
            lNC_Drill = file.AllLayers.Last();

            // Add objects

            ObjectAttributes oa;

            oa = new ObjectAttributes();

            oa.LayerIndex = lBL_Centreline.Index;
            oa.Name = "Glulam_Centreline";
            file.Objects.AddCurve(g.Centreline, oa);

            oa.LayerIndex = lBL_Geometry.Index;
            oa.UserDictionary.Set("LamWidth", g.Data.LamWidth);
            oa.UserDictionary.Set("LamHeight", g.Data.LamHeight);
            oa.UserDictionary.Set("NumWidth", g.Data.NumWidth);
            oa.UserDictionary.Set("NumHeight", g.Data.NumHeight);

            oa.Name = "Glulam_BoundingMesh";
            Guid blank_id = file.Objects.AddBrep(g.GetBoundingBrep(), oa);

            oa.LayerIndex = lBE_Geometry.Index;
            oa.UserDictionary.Clear();
            oa.Name = "Beam_Geometry";
            file.Objects.AddBrep(beam, oa);

            oa.LayerIndex = lBL_Safety.Index;
            oa.Name = "Glulam_Safety";

            Brep blank_safety = g.GetBoundingBrep(50.0);
            if (blank_safety != null)
                file.Objects.AddBrep(blank_safety, oa);


            for (int i = 0; i < Cuts_Surface.Count; ++i)
            {
                oa.LayerIndex = lNC_Surface.Index;
                oa.Name = string.Format("Machining_Surface_{0:00}", i);
                file.Objects.AddBrep(Cuts_Surface[i], oa);

                oa.LayerIndex = lNC_Normals.Index;
                oa.Name = string.Format("Machining_Surface_Normal_{0:00}", i);
                BrepFace bf = Cuts_Surface[i].Faces[0];
                Vector3d normal = bf.NormalAt(bf.Domain(0).Mid, bf.Domain(1).Mid);
                Point3d point = bf.PointAt(bf.Domain(0).Mid, bf.Domain(1).Mid);

                file.Objects.AddTextDot(string.Format("NC_Srf_{0:00}", i), point + normal * 100.0, oa);
                file.Objects.AddLine(new Line(point, point + normal * 100.0), oa);
            }

            for (int i = 0; i < Cuts_GlueSurface.Count; ++i)
            {
                oa.LayerIndex = lNC_Surface.Index;
                oa.Name = string.Format("Glue_Surface_{0:00}", i);
                file.Objects.AddBrep(Cuts_GlueSurface[i], oa);

                oa.LayerIndex = lNC_Normals.Index;
                oa.Name = string.Format("Glue_Surface_Normal_{0:00}", i);
                BrepFace bf = Cuts_GlueSurface[i].Faces[0];
                Vector3d normal = bf.NormalAt(bf.Domain(0).Mid, bf.Domain(1).Mid);
                Point3d point = bf.PointAt(bf.Domain(0).Mid, bf.Domain(1).Mid);

                file.Objects.AddTextDot(string.Format("NC_Glu_{0:00}", i), point + normal * 100.0, oa);
                file.Objects.AddLine(new Line(point, point + normal * 100.0), oa);
            }

            for (int i = 0; i < Cuts_Joint.Count; ++i)
            {
                oa.LayerIndex = lNC_Joints.Index;
                oa.Name = string.Format("Joint_Surface_{0:00}", i);
                file.Objects.AddBrep(Cuts_Joint[i], oa);

                oa.LayerIndex = lNC_Normals.Index;
                oa.Name = string.Format("Joint_Surface_Normal_{0:00}", i);
                BrepFace bf = Cuts_Joint[i].Faces[0];
                Vector3d normal = bf.NormalAt(bf.Domain(0).Mid, bf.Domain(1).Mid);
                Point3d point = bf.PointAt(bf.Domain(0).Mid, bf.Domain(1).Mid);

                file.Objects.AddTextDot(string.Format("NC_Jnt_{0:00}", i), point + normal * 100.0, oa);
                file.Objects.AddLine(new Line(point, point + normal * 100.0), oa);
            }

            for (int i = 0; i < Drills_Joint.Count; ++i)
            {
                oa.LayerIndex = lNC_Drill.Index;
                oa.Name = string.Format("Joint_Drill_{0:00}", i);

                Vector3d dir = Drills_Joint[i].Direction;
                dir.Unitize();

                file.Objects.AddTextDot(string.Format("NC_Drill_{0:00}", i), Drills_Joint[i].From - dir * 40.0, oa);
                file.Objects.AddLine(Drills_Joint[i], oa);
            }

            // Write notes and data
            string notes = "";
            notes += "This file was created with tasTools (Tom Svilans, 2017).\n\n";
            notes += "Blank data:\n\n";
            notes += string.Format("LamWidth\n{0}\n", g.Data.LamWidth);
            notes += string.Format("LamHeight\n{0}\n", g.Data.LamHeight);
            notes += string.Format("NumWidth\n{0}\n", g.Data.NumWidth);
            notes += string.Format("NumHeight\n{0}\n", g.Data.NumHeight);
            notes += string.Format("Length\n{0}\n", g.Centreline.GetLength());

            file.Notes.Notes = notes;
            file.Settings.ModelUnitSystem = UnitSystem.Millimeters;

            file.Write(path + "\\" + Name + ".3dm", 5);
        }
    }
}
