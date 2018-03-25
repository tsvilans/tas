using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace tas.Machine.GH
{
    public class MachineGHInfo : GH_AssemblyInfo
  {
    public override string Name
    {
        get
        {
            return "tasMachine";
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
            return new Guid("d88d7929-f861-4c18-a624-a8e72f589a4c");
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
