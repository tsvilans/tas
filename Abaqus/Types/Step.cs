using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{

    public class Step
    {
        public string Name;
        public bool nlgeom = false;

        public bool Static = true;

        public List<BoundaryCondition> BoundaryConditions;
        public List<Load> Loads;

        public Step(string name = "Step-1")
        {
            Loads = new List<Load>();
            BoundaryConditions = new List<BoundaryCondition>();
            Name = name;
        }

    }
}
