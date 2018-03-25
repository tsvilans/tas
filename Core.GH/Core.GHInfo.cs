using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace tas.Core.GH
{
    public class CoreGHInfo : GH_AssemblyInfo
  {
    public override string Name
    {
        get
        {
            return "tasCore";
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
            return new Guid("a0f15531-11a6-4adc-873a-12228058bfa3");
        }
    }

    public override string AuthorName
    {
        get
        {
            //Return a string identifying you or your company.
            return "Tom Svilans";
        }
    }
    public override string AuthorContact
    {
        get
        {
            //Return a string representing your preferred contact details.
            return "tsvi@kadk.dk";
        }
    }
}
}
