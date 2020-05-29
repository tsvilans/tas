using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{



    public class JsonGenerator : Job
    {
        public JsonGenerator() : base()
        {
        }

        public void WriteHeader(Dictionary<string, object> json)
        {
            var header = new Dictionary<string, object>();
            header.Add("job_name", JobName);
            header.Add("author", Author);

            json.Add("header", header);

            //inp.Add($"*Preprint, echo=NO, model=NO, history=NO, contact=NO");
        }

        public void WriteParts(Dictionary<string, object> json)
        {
            var parts = new List<object>();

            foreach (Part prt in Parts)
            {
                WritePart(parts, prt);
            }

            json.Add("parts", parts);

        }

        public void WritePart(List<object> parts, Part prt)
        {
            var part = new Dictionary<string, object>();
            part.Add("name", prt.Name);

            var nodes = new List<object>();

            // inp.Add($"*Part, name={prt.Name}");

            //inp.Add("*Node");

            foreach (Node n in prt.Nodes)
            {
                var node = new Dictionary<string, object>();
                node.Add("id", n.Id);
                node.Add("X", n.X);
                node.Add("Y", n.Y);
                node.Add("Z", n.Z);

                nodes.Add(node);
            }

            part.Add("nodes", nodes);

            if (prt.Elements.Count < 1) throw new Exception("Part must contain elements!");

            //inp.Add($"*Element, type={prt.Elements[0].Type}");


            var elements = new List<object>();

            foreach (Element ele in prt.Elements)
            {
                var jelement = new Dictionary<string, object>();
                jelement.Add("id", ele.Id);
                jelement.Add("indices", ele.Data);

                elements.Add(jelement);

            }

            part.Add("elements", elements);

            var orientations = new List<object>();


            //inp.Add("*Distribution, name=OrientationDistribution, location=ELEMENT, Table=OrientationDistributionTable");
            // inp.Add(", 1., 0., 0., 0., 1., 0.");

            foreach (ElementOrientation ori in prt.ElementOrientations)
            {
                var jori = new Dictionary<string, object>();
                jori.Add("id", ori.Id);
                jori.Add("ZX", ori.Data.ZAxis.X);
                jori.Add("ZY", ori.Data.ZAxis.Y);
                jori.Add("ZZ", ori.Data.ZAxis.Z);

                jori.Add("YX", ori.Data.YAxis.X);
                jori.Add("YY", ori.Data.YAxis.Y);
                jori.Add("YZ", ori.Data.YAxis.Z);

                orientations.Add(jori);
            }

            part.Add("orientations", orientations);

            var jsection = new Dictionary<string, object>();
            jsection.Add("name", "Glulam section");
            jsection.Add("material", prt.Material.Name);

            part.Add("section", jsection);

            parts.Add(part);
        }

        public void WriteAssemblies(Dictionary<string, object> json)
        {
            var assemblies = new List<object>();

            foreach (Assembly ass in Assemblies)
            {
                WriteAssembly(assemblies, ass);
            }

            json.Add("assemblies", assemblies);
        }

        public void WriteAssembly(List<object> assemblies, Assembly ass)
        {
            var jass = new Dictionary<string, object>();

            jass.Add("name", ass.Name);

            var jpinstances = new List<object>();

            //inp.Add($"*Assembly, name={ass.Name}");
            //inp.Add("**");

            foreach (PartInstance prt in ass.Instances)
            {
                var jprt = new Dictionary<string, object>();
                jprt.Add("name", prt.Name);
                jprt.Add("part_name", prt.Part.Name);

                jpinstances.Add(jprt);
            }

            jass.Add("part_instances", jpinstances);

            /*

            foreach (ElementSet set in ass.ElementSets)
            {
              if (set.Data.Count < 1) continue;

              line = "*Elset, elset={set.Name}";
              if (set.Instance != null)
                line += ", instance={set.Instance.Name}";
              inp.Add(line);

              line = set.Data[0].ToString() + ", ";
              for (int i = 1; i < set.Data.Count; ++i)
              {
                if (i.Modulus(16) < 1)
                {
                  inp.Add(line);
                  line = "";
                }

                line += set.Data[i].ToString() + ", ";
              }
              //line = string.Join<int>(",", set.Data);

              inp.Add(line);
            }

            foreach (NodeSet set in ass.NodeSets)
            {
              if (set.Data.Count < 1) continue;

              line = "*Nset, nset={set.Name}";
              if (set.Instance != null)
                line += ", instance={set.Instance.Name}";
              inp.Add(line);

              line = set.Data[0].ToString() + ", ";
              for (int i = 1; i < set.Data.Count; ++i)
              {
                if (i.Modulus(16) < 1)
                {
                  inp.Add(line);
                  line = "";
                }

                line += set.Data[i].ToString() + ", ";
              }
              //line = string.Join<int>(",", set.Data);

              inp.Add(line);
            }

            foreach (Surface srf in ass.Surfaces)
            {
              inp.Add($"*Surface, type={srf.Type}, name={srf.Name}");
              int N = Math.Min(srf.Sets.Count, srf.ElementSides.Count);

              for (int i = 0; i < N; ++i)
              {
                inp.Add($"{srf.Sets[i].Name}, {srf.ElementSides[i]}");
              }
            }

            */

            assemblies.Add(jass);
        }

        public void WriteMaterials(Dictionary<string, object> json)
        {
            var materials = new List<object>();

            foreach (Material mat in Materials)
            {
                WriteMaterial(materials, mat);
            }

            json.Add("materials", materials);


        }

        public void WriteMaterial(List<object> materials, Material mat)
        {
            var jmat = new Dictionary<string, object>();
            jmat.Add("name", mat.Name);
            jmat.Add("density", mat.Density);

            var engcon = new Dictionary<string, object>();
            engcon.Add("type", "ENGINEERING CONSTANTS");
            engcon.Add("data", mat.EngineeringConstants);

            jmat.Add("elastic", engcon);

            materials.Add(jmat);
        }

        public void WriteSteps(Dictionary<string, object> json)
        {
            var jsteps = new List<object>();

            foreach (Step step in Steps)
            {
                WriteStep(jsteps, step);
            }

            json.Add("steps", jsteps);
        }

        protected void WriteStep(List<object> jsteps, Step step)
        {

            var jstep = new Dictionary<string, object>();
            jstep.Add("name", step.Name);

            var jbounds = new List<object>();

            foreach (BoundaryCondition bc in step.BoundaryConditions)
            {
                WriteBoundaryCondition(jbounds, bc);
            }

            jstep.Add("boundary_conditions", jbounds);

            var jloads = new List<object>();

            foreach (Load lo in step.Loads)
            {
                WriteLoad(jloads, lo);
            }

            jstep.Add("loads", jloads);

            jsteps.Add(jstep);
        }

        /*
        protected void WriteBoundaryConditions(List<string> inp)
        {
            inp.Add("**");
            inp.Add("** BOUNDARY CONDITIONS");
            inp.Add("**");

            foreach (BoundaryCondition bc in BoundaryConditions)
            {
                WriteBoundaryCondition(inp, bc);
            }
        }
        */

        protected void WriteBoundaryCondition(List<object> jbounds, BoundaryCondition bc)
        {
            var jbound = new Dictionary<string, object>();

            jbound.Add("name", bc.Name);
            jbound.Add("type", bc.Type);
            jbound.Add("type_string", bc.Type.ToString());

            if (bc.Set != null)
                jbound.Add("element_set", bc.Set.Name);

            jbounds.Add(jbound);
        }

        protected void WriteLoad(List<object> jloads, Load lo)
        {
            var jload = new Dictionary<string, object>();
            jload.Add("name", lo.Name);
            jload.Add("type", lo.Type);
            jload.Add("gravity", new double[] { 9.8, 0, 0, -1 });

            jloads.Add(jload);
        }

        public Dictionary<string, object> Generate()
        {
            Dictionary<string, object> json = new Dictionary<string, object>();

            WriteHeader(json);

            // Write all parts
            WriteParts(json);

            // Write all assemblies
            WriteAssemblies(json);

            // Write all materials
            WriteMaterials(json);

            // Write all steps with associated boundary conditions and loads
            WriteSteps(json);

            return json;
        }
    }

}
