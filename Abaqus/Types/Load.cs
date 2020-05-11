using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public abstract class Load
    {
        public string Name;
        public abstract string Type { get; }
        public double Magnitude;

        public ElementSet Set;

        public Load()
        {
            Name = "Load-1";
            Magnitude = 9.8;
            Set = null;
        }

    }

    public class GravityLoad : Load
    {
        public override string Type => "Dload";
    }

    public class SurfaceLoad : Load
    {
        public override string Type => "Dsload";

    }

}
