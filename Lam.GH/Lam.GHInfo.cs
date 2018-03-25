/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017 Tom Svilans
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 */

using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace tas.Lam.GH
{
    public class LamGHInfo : GH_AssemblyInfo
  {
    public override string Name
    {
        get
        {
            return "tasLam";
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
            return new Guid("0e312402-3391-48ca-a538-5e7efb3890d8");
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

        public override GH_LibraryLicense License
        {
            get
            {
                return GH_LibraryLicense.opensource;
            }
        }
    }
}
