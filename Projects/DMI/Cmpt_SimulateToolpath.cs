using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

using tas.Core;
using tas.Core.Util;
using tas.Machine;
using tas.Machine.GH;

namespace tas.Projects.DMI.GH
{
    public class Cmpt_SimulateToolpath : GH_Component
    {
        public Cmpt_SimulateToolpath()
          : base("Simulate Toolpath", "SimTP",
              "Simulate the current position of the machine tool at a specific time parameter.",
              "tasMachine", "Machining")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpaths", "TP", "List of Toolpaths to simulate.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Time", "T", "Normalized time parameter (0-1).", GH_ParamAccess.item, 0.0);
            pManager.AddBooleanParameter("Reset", "R", "Reset the simulation.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Target", "T", "Position and orientation of the machine tool at the specified time.", GH_ParamAccess.item);
            pManager.AddGenericParameter("MachineTool", "MT", "Machine tool data of current toolpath.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double m_time = 0.0;
            bool m_reset = false;

            DA.GetData("Time", ref m_time);
            DA.GetData("Reset", ref m_reset);

            //----------------------
            if (m_wp == null || m_times == null || m_feeds == null || m_times.Count < 1 || m_reset)
            {
                List<Toolpath> paths = new List<Toolpath>();
                if (!DA.GetDataList(0, paths)) return;

                BuildSimulation(paths);
            }
            //----------------------

            if (m_times.Count > 0)
            {

                Plane target;
                target = m_wp[0].Plane;
                //target = Plane.WorldXY;

                double tSearch = cTime * m_time;

                int index = m_times.BinarySearch(tSearch);
                if (index < 0)
                {
                    index = ~index;
                }

                if (index > 0)
                {

                    double tPrev = m_times[index - 1];
                    double tNext = m_times[index];
                    double t = (tSearch - tPrev) / (tNext - tPrev);

                    target = Interpolation.InterpolatePlanes2(m_wp[index - 1].Plane, m_wp[index].Plane, t);
                }

                //----------------------

                m_active = SearchToolpath(tSearch);

                DA.SetData("Target", target);
                DA.SetData("MachineTool", new GH_MachineTool(m_active.Tool));

            }
        }

        Toolpath m_active = null;

        //List<Mesh> m_meshes = null;

        List<Waypoint> m_wp = null;
        List<double> m_times = null;
        List<double> m_feeds = null;
        List<Toolpath> m_toolpaths = new List<Toolpath>();
        List<double> m_toolpath_times = new List<double>();

        double cTime = 0;

        public void BuildSimulation(List<Toolpath> toolpaths)
        {
            m_wp = new List<Waypoint>();
            m_times = new List<double>();
            m_feeds = new List<double>();

            m_toolpaths = new List<Toolpath>();
            m_toolpath_times = new List<double>();

            Waypoint cTarget, cPrev;
            cTime = 0;

            cPrev = toolpaths.First().Paths.First().First();
            //m_times.Add(0);

            int feed_index = 0;

            for (int i = 0; i < toolpaths.Count; ++i)
            {
                Toolpath tp = toolpaths[i];

                m_toolpaths.Add(tp);
                m_toolpath_times.Add(cTime);

                for (int j = 0; j < tp.Paths.Count; ++j)
                {
                    for (int k = 0; k < tp.Paths[j].Count; ++k)
                    {
                        cTarget = tp.Paths[j][k];

                        m_wp.Add(cTarget);
                        if (cTarget.IsFeed())
                            m_feeds.Add(tp.Tool.FeedRate);
                        else if (cTarget.IsPlunge())
                            m_feeds.Add(tp.Tool.PlungeRate);
                        else if (cTarget.IsRapid())
                            m_feeds.Add(tp.Tool.FeedRate * 2);
                        else
                            throw new Exception("Fuck!");

                        cTime += cTarget.Plane.Origin.DistanceTo(cPrev.Plane.Origin) / m_feeds[feed_index];
                        feed_index++;

                        m_times.Add(cTime);
                        cPrev = cTarget;

                    }
                }
            }

            foreach (double d in m_toolpath_times)
            {
            }

            if (m_wp.Count < 1)
                throw new Exception("No waypoints found!");

        }

        Toolpath SearchToolpath(double tSearch)
        {
            int index = m_toolpath_times.BinarySearch(tSearch);

            if (index < 0)
            {
                index = ~index;
                index--;
            }
            return m_toolpaths[index];
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //return tas.Projects.DMI.GH.Properties.Resources.icon_oriented_polyline_component_24x24;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{a773f757-c628-4613-ad77-56a38aa473b1}"); }
        }
    }
}
