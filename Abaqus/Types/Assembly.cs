using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class Assembly
    {
        public string Name;
        public List<PartInstance> Instances;
        public List<Surface> Surfaces;
        public List<ElementSet> ElementSets;
        public List<NodeSet> NodeSets;


        public Assembly()
        {
            Instances = new List<PartInstance>();
            Surfaces = new List<Surface>();
            ElementSets = new List<ElementSet>();
            NodeSets = new List<NodeSet>();
        }
    }
}
