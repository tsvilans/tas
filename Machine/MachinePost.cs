/*
 * tasTools
 * A personal PhD research toolkit.
 * Copyright 2017-2018 Tom Svilans
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

namespace tas.Machine
{
    /// <summary>
    /// Base class for toolpath post-processor. Inherit from this
    /// to create machine-specific posts.
    /// </summary>
    public abstract class MachinePost
    {
        // Dummy variables
        public string Name = "MachinePost";
        public string Author = "Author";
        public string Date = System.DateTime.Now.ToShortDateString();
        public string ProgramTime = "X";
        public double MaterialWidth = 0, MaterialHeight = 0, MaterialDepth = 0;

        public List<Toolpath> Paths = new List<Toolpath>();

        public abstract object Compute();
        public void AddPath(Toolpath p) => Paths.Add(p);
        public void AddPaths(ICollection<Toolpath> p) => Paths.AddRange(p);

    }

    /// <summary>
    /// Post to convert to Robots targets and program.
    /// </summary>
    public class RobotsPost : MachinePost
    {
        public override object Compute()
        {
            throw new NotImplementedException();
        }
    }
    

}
