using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class Material
    {
        public string Name;
        public double Density;
        public double[] EngineeringConstants;

        public Material()
        {
            Name = "Material";
            Density = 1.0;
            EngineeringConstants = new double[9];
        }

        public static Material BasicWood
        {
            get
            {
                return new Material
                {
                    Name = "BasicWood",
                    Density = 0.5,
                    EngineeringConstants = new double[] { 14788, 1848, 1087, 0.39, 0.46, 0.67, 1220, 971, 366 }
                };
            }
        }
        // Presets taken from https://www.researchgate.net/publication/333317649_Verification_of_Orthotropic_Model_of_Wood
        public static Material Pine
        {
            get { return new Material { Name = "Pine", Density = 0.4, EngineeringConstants = new double[] { 6919, 271, 450, 0.388, 0.375, 0.278, 262, 354, 34 } }; }
        }
        public static Material SitkaSpruce
        {
            get { return new Material { Name = "SitkaSpruce", Density = 0.4, EngineeringConstants = new double[] { 11880, 511, 927, 0.467, 0.372, 0.245, 725, 760, 36 } }; }
        }

    }
}
