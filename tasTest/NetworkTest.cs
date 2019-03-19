using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Rhino.Geometry;

using tas.Core.Network;

namespace tas.Testing
{
    public static class NetworkTest
    {
        public static string JsonSerializeNetwork(Net net)
        {
            JsonSerializerSettings jsonSS = new JsonSerializerSettings();
            jsonSS.Formatting = Formatting.Indented;
            jsonSS.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSS.PreserveReferencesHandling = PreserveReferencesHandling.None;

            return JsonConvert.SerializeObject(net, jsonSS);
        }

        public static Net JsonDeserializeNetwork(string json)
        {
            /*
            JsonSerializerSettings jsonSS = new JsonSerializerSettings();
            jsonSS.Formatting = Formatting.Indented;
            jsonSS.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSS.PreserveReferencesHandling = PreserveReferencesHandling.None;
            */

            return JsonConvert.DeserializeObject<Net>(json);
        }

        public static string JsonSerializeNodes()
        {                    
            JsonSerializerSettings jsonSS = new JsonSerializerSettings();
            jsonSS.Formatting = Formatting.Indented;
            jsonSS.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; //If there is referenced object then it is not shown in the json serialisation
                                                                         //jsonSS.ReferenceLoopHandling = ReferenceLoopHandling.Serialize; //Throws stackoverflow error
            //jsonSS.PreserveReferencesHandling = PreserveReferencesHandling.All;
            jsonSS.PreserveReferencesHandling = PreserveReferencesHandling.None;

            Net net = new Net();
            Node n1 = new Node();
            Node n2 = new Node();
            n1.Parent = n2;

            net.Link(n1, n2);
            /*
            var nodes = net.GetAllNodes();
            var edges = net.GetAllEdges();

            var str = new System.Text.StringBuilder();


            foreach (var node in nodes)
            {
                str.Append(JsonConvert.SerializeObject(node, jsonSS));
                str.AppendLine();
            }

            foreach (var edge in edges)
            {
                str.Append(JsonConvert.SerializeObject(edge, jsonSS));
                str.AppendLine();
            }
            */
            return JsonConvert.SerializeObject(net, jsonSS);
        }
    }
}
