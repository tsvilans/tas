using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class Set
    {
        public List<int> Data;
        public string Name;
        public PartInstance Instance = null;

        public Set(string name = "Set", PartInstance instance = null)
        {
            Name = name;
            Instance = instance;
            Data = new List<int>();

        }
    }

    public class ElementSet : Set 
    { 
        public ElementSet(string name = "ElementSet", PartInstance instance = null) : base(name, instance)
        {
        }

    }
    public class NodeSet : Set 
    {
        public NodeSet(string name = "NodeSet", PartInstance instance = null) : base(name, instance)
        {
        }
    }

}
