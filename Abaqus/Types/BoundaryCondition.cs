using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Abaqus
{
    public class BoundaryCondition
    {
        public string Name;
        public NodeSet Set;

        // For CUSTOM type: if either are non-zero, then "Either a single degree of freedom 
        // or the first and last of a range of degrees of freedom can be specified."
        // i.e. either 'node or node set, degree of freedom' or 
        // 'node or node set, first degree of freedom, last degree of freedom'
        public int FirstConstrained = 0;
        public int LastConstrained = 0;

        public BoundaryConditionType Type;

        public BoundaryCondition(string name = "BC-1", BoundaryConditionType type = BoundaryConditionType.PINNED, NodeSet nset = null)
        {
            //Name = "BC-1";
            //Type = "Displacement/Rotation";

            Name = name;
            Type = type;
            Set = nset;
        }

        public enum BoundaryConditionType
        {
            XSYMM,
            YSYMM,
            ZSYMM,
            ENCASTRE,
            PINNED,
            XASYMM,
            YASYMM,
            ZASYMM,
            CUSTOM
        }
    }
}
