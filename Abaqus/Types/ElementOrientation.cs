using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

namespace tas.Abaqus
{
    public struct ElementOrientation
    {
        public int Id;
        public Plane Data;
        public ElementOrientation(Plane plane, int id)
        {
            Id = id;
            Data = plane;
        }
    }
}
