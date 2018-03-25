using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace tas.Lam
{
    // from: https://stackoverflow.com/a/15143390
    class FeatureLoader
    {
        const string PluginTypeName = "tas.Lam.Feature";

        /// <summary>Loads all plugins from a DLL file.</summary>
        /// <param name="fileName">The filename of a DLL, e.g. "C:\Prog\MyApp\MyPlugIn.dll"</param>
        /// <returns>A list of plugin objects.</returns>
        /// <remarks>One DLL can contain several types which implement `IMyPlugin`.</remarks>
        public List<Feature> LoadPluginsFromFile(string fileName)
        {
            System.Reflection.Assembly asm;
            Feature plugin;
            List<Feature> plugins;
            Type tInterface;

            plugins = new List<Feature>();
            asm = System.Reflection.Assembly.LoadFrom(fileName);
            if (asm == null) return plugins;

            foreach (Type t in asm.GetExportedTypes())
            {
                tInterface = t.GetInterface(PluginTypeName);
                if (tInterface != null && (t.Attributes & TypeAttributes.Abstract) !=
                    TypeAttributes.Abstract)
                {

                    plugin = (Feature)Activator.CreateInstance(t);
                    plugins.Add(plugin);
                }
            }
            return plugins;
        }
    }
}
