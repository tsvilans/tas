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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace BuildAll
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembly assem = Assembly.GetEntryAssembly();
            AssemblyName assemName = assem.GetName();
            Version ver = assemName.Version;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n\n   ---\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   tasTools .NET");
            Console.WriteLine("\n   ---\n\n");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("   build " + ver.ToString(4));
            Console.WriteLine("   A personal PhD research toolkit.");
            Console.WriteLine("   by Tom Svilans, 2016-2018");
            Console.WriteLine("   tsvi@kadk.dk");
            Console.WriteLine("   info@tomsvilans.com");
            Console.WriteLine();

            Console.WriteLine("   tomsvilans.com");
            Console.ReadLine();
        }
    }
}
