using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using tas.Core.Types;

namespace tas.Machine.GH.Components
{
    public class Cmpt_ShowToolpath : GH_Component
    {

        public Cmpt_ShowToolpath()
          : base("Show Toolpath", "Show",
              "Display toolpath.",
              "tasMachine", "Machining")
        {
        }


        List<Line> m_lines = new List<Line>();
        List<int> m_types = new List<int>();

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath to display.", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddCurveParameter("Paths", "P", "Toolpath as Polylines.", GH_ParamAccess.list);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Toolpath> tpIn = new List<Toolpath>();

            DA.GetDataList("Toolpath", tpIn);

            List<Toolpath> m_toolpaths = tpIn.Select(x => x.Duplicate()).ToList();

            this.Message = string.Format("Got {0} toolpaths.", m_toolpaths.Count);

            Path path = new Path();
            m_types = new List<int>();

            for (int i = 0; i < m_toolpaths.Count; ++i)
            {
                for (int j = 0; j < m_toolpaths[i].Paths.Count; ++j)
                    for (int k = 0; k < m_toolpaths[i].Paths[j].Count; ++k)
                    {
                        path.Add(m_toolpaths[i].Paths[j][k].Plane);

                        if (m_toolpaths[i].Paths[j][k].IsRapid())
                            m_types.Add(0);
                        else if (m_toolpaths[i].Paths[j][k].IsFeed())
                            m_types.Add(1);
                        else if (m_toolpaths[i].Paths[j][k].IsPlunge())
                            m_types.Add(2);
                        else
                            m_types.Add(-1);
                    }
            }

            m_lines = new List<Line>();

            for (int i = 0; i < path.Count - 1; ++i)
                m_lines.Add(new Line(path[i].Origin, path[i + 1].Origin));


        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            for (int i = 0; i < m_lines.Count; ++i)
            {
                switch (m_types[i + 1])
                {
                    case (0):
                        args.Display.DrawLine(m_lines[i], System.Drawing.Color.Red);
                        break;
                    case (1):
                        args.Display.DrawLine(m_lines[i], System.Drawing.Color.LightBlue);
                        break;
                    case (2):
                        args.Display.DrawLine(m_lines[i], System.Drawing.Color.LimeGreen);
                        break;
                    case (-1):
                        args.Display.DrawLine(m_lines[i], System.Drawing.Color.Purple);
                        break;
                    default:
                        break;
                }
            }

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_ShowToolpath_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("af624288-def3-4a30-b7d5-7bb3636e1624"); }
        }
    }
}