#if OBSOLETE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;

namespace tas.Lam
{
    public class GlulamFactory
    {
        #region Constraints
        public double MinLamellaWidth = 0.0;
        public double MaxLamellaWidth = double.MaxValue;

        public double MinLamellaHeight = 0.0;
        public double MaxLamellaHeight = double.MaxValue;

        public int MinLamellaCountWidth = 1;
        public int MaxLamellaCountWidth = int.MaxValue;

        public int MinLamellaCountHeight = 1;
        public int MaxLamellaCountHeight = int.MaxValue;

        public int CrossSectionSamples = 50;
        #endregion

        #region Glulam parameters
        public double LamellaWidth = double.MaxValue;
        public double LamellaHeight = double.MaxValue;

        public int LamellaCountWidth = 1;
        public int LamellaCountHeight = 1;

        public double Width = 100.0;
        public double Height = 200.0;

        Curve Centreline = null;
        List<Plane> Frames = new List<Plane>();

        #endregion

        public GlulamFactory()
        {

        }

        public Glulam Generate()
        {

            GlulamData data = new GlulamData(Centreline, Width, Height, Frames.ToArray(), CrossSectionSamples, 100);
            Glulam g = Glulam.CreateGlulam(Centreline, Frames.ToArray(), data);
            return g;
        }

    }
}
#endif