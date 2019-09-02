using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

using tas.Core;
using tas.Core.Network;

namespace Kelowna
{
    public static class Modelling
    {
        public static List<object> debug = new List<object>();

        public static void Initialize()
        {
            debug = new List<object>();
        }

        public static void OffsetNodes(NodeGroup ng, double xOffset, double yOffset)
        {
            Polyline poly = new Polyline(
              ng.Nodes.Select(x => x.Frame.Origin));

            if (ng.CustomData.ContainsKey("LoopClosed") && (bool)ng.CustomData["LoopClosed"])
                poly.Add(poly[0]);

            var normal = ng.Frame.ZAxis;
            if (normal * Vector3d.ZAxis < 0) normal.Reverse();

            Polyline newPoly = Util.OffsetPolyline(poly, normal, xOffset, yOffset);

            if (newPoly.Count > 1)
                debug.Add(newPoly);

            int N = Math.Min(ng.Nodes.Count, newPoly.Count);
            for (int i = 0; i < N; ++i)
            {
                ng.Nodes[i].Frame.Origin = newPoly[i];
            }
        }

    }
}
