using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;
using System.Linq;

namespace tas.Lam.GH
{
    public class Util
    {
        public ArchivableDictionary GlulamPropertiesToArchivableDictionary(Glulam g)
        {
            ArchivableDictionary ad = new ArchivableDictionary();

            var gd = g.GetProperties();

            //ad.Set("id", g.ID);
            ad.Set("centreline", g.Centreline);
            ad.Set("width", g.Width());
            ad.Set("height", g.Height());
            ad.Set("lamella_width", g.Data.LamWidth);
            ad.Set("lamella_height", g.Data.LamHeight);
            ad.Set("lamella_count_width", g.Data.NumWidth);
            ad.Set("lamella_count_height", g.Data.NumHeight);
            ad.Set("volume", g.GetVolume());
            ad.Set("samples", g.Data.Samples);

            /*
            var planes = g.GetAllPlanes();
            ArchivableDictionary pd = new ArchivableDictionary();

            for (int i = 0; i < planes.Length; ++i)
            {
                pd.Set(string.Format("Frame_{0}", i), planes[i]);
            }
            ad.Set("frames", pd);

            */
            double max_kw = 0.0, max_kh = 0.0;
            ad.Set("max_curvature", g.GetMaxCurvature(ref max_kw, ref max_kh));
            ad.Set("max_curvature_width", max_kw);
            ad.Set("max_curvature_height", max_kh);
            ad.Set("type", g.ToString());
            ad.Set("type_id", (int)g.Type());

            return ad;
        }
    }
}