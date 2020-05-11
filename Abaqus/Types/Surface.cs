using System;
using System.Collections.Generic;

namespace tas.Abaqus
{

    public class Surface
    {
        public string Type => "ELEMENT";
        public string Name = "Surface";
        public List<ElementSet> Sets;
        public List<string> ElementSides;
    }
}
