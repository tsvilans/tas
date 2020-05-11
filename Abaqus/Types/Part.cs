using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class Part
    {
        public string Name;
        public List<Node> Nodes;
        public List<Element> Elements;
        public List<ElementOrientation> ElementOrientations;
        public Material Material;

        public Part()
        {
            Name = "Part";
            Nodes = new List<Node>();
            Elements = new List<Element>();
            ElementOrientations = new List<ElementOrientation>();
            Material = new Material();
        }
    }

    public class PartInstance
    {
        public string Name;
        public Part Part;
    }
}
