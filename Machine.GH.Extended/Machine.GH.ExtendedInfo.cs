using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace tas.Machine.GH.Extended
{
   public class MachineGHExtendedInfo : GH_AssemblyInfo
  {
        public override string Name
        {
            get
            {
                return "tasMachineExtended";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                return "tasTools - A personal PhD research toolkit.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("68d725f6-25c6-463e-a0c6-0eaecb74ec17");
            }
        }

        public override string AuthorName
        {
            get
            {
                return "Tom Svilans";
            }
        }
        public override string AuthorContact
        {
            get
            {
                return "tsvi@kadk.dk";
            }
        }
    }
}
