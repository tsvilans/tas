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
        /// <summary>
        /// Initializes a new instance of the Cmpt_ShowToolpath class.
        /// </summary>
        public Cmpt_ShowToolpath()
          : base("Show Toolpath", "Show",
              "Display toolpath.",
              "tasTools", "Machining")
        {
        }


        List<Line> m_lines = new List<Line>();
        List<int> m_types = new List<int>();

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Toolpath", "T", "Toolpath to display.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddCurveParameter("Paths", "P", "Toolpath as Polylines.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> iObjs = new List<object>();
            List<Toolpath> m_toolpaths = new List<Toolpath>();

            DA.GetDataList("Toolpath", iObjs);
            if (iObjs == null || iObjs.Count < 1)
                return;

            Toolpath inTP = null;
            for (int i = 0; i < iObjs.Count; ++i)
            {
                if (iObjs[i] is GH_Toolpath)
                    inTP = (iObjs[i] as GH_Toolpath).Value;
                else if (iObjs[i] is Toolpath)
                    inTP = iObjs[i] as Toolpath;

                if (inTP == null)
                    return;

                Toolpath tp = inTP.Duplicate();

                tp.CreateLeadsAndLinks();
                m_toolpaths.Add(tp);
            }

            this.Message = string.Format("Got {0} toolpaths.", m_toolpaths.Count);

            PPolyline path = new PPolyline();
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


            /*
            List<GH_Curve> plines = new List<GH_Curve>();
            foreach (Toolpath tp in m_toolpaths)
            {
                for (int i = 0; i < tp.Paths.Count; ++i)
                {
                    plines.Add(new GH_Curve(new Polyline(tp.Paths[i].Select(x => x.Plane.Origin)).ToNurbsCurve()));
                }
            }

            DA.SetDataList("Paths", plines);
            */
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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af624288-def3-4a30-b7d5-7bb3636e1624"); }
        }
    }
}