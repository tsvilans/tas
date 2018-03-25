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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using Grasshopper.Kernel;
//using GH_IO.Serialization;

namespace tas.Machine
{

    public class ToolSettings
    {
        public double ToolDiameter;
        public double StepOver;
        public double StepDown;

        public ToolSettings()
        {
            ToolDiameter = 12.0;
            StepOver = 8.0;
            StepDown = 8.0;
        }

        //public bool Write(GH_IWriter writer)
        //{
        //    writer.SetDouble("ToolDiameter", ToolDiameter);
        //    writer.SetDouble("StepDown", StepDown);
        //    writer.SetDouble("StepOver", StepOver);
        //    return true;
        //}

        //public bool Read(GH_IReader reader)
        //{
        //    ToolDiameter = reader.GetDouble("ToolDiameter");
        //    StepDown = reader.GetDouble("StepDown");
        //    StepOver = reader.GetDouble("StepOver");
        //    return true;
        //}

    }
}
