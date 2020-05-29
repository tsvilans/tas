using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class Job
    {
        public List<Assembly> Assemblies;
        public List<Part> Parts;
        public List<Material> Materials;
        public List<BoundaryCondition> BoundaryConditions;
        public List<Load> Loads;
        public List<Step> Steps;

        public string JobName, Author;

        public Job()
        {
            Assemblies = new List<Assembly>();
            Parts = new List<Part>();
            Materials = new List<Material>();
            BoundaryConditions = new List<BoundaryCondition>();
            Loads = new List<Load>();
            Steps = new List<Step>();
            JobName = "Job-99";
        }

    }
}
