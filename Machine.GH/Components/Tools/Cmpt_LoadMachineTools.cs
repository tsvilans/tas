using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH.Components
{
    public class Cmpt_LoadMachineTools : GH_Component
    {
        public Cmpt_LoadMachineTools()
          : base("Load Machine Tools", "LoadMT",
              "Load machine tool definitions from XML file.",
              "tasMachine", "Tools")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "File path to tool library XML.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Tools", "T", "Loaded tools.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            DA.GetData("Path", ref path);

            // Load tool library
            System.Xml.XmlDocument xdoc = new System.Xml.XmlDocument();
            if (!System.IO.File.Exists(path) || !path.EndsWith(".xml"))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find library file.");
                return;
            }

            xdoc.Load(path);

            List<GH_MachineTool> tools = new List<GH_MachineTool>();

            var xtools = xdoc.DocumentElement.SelectNodes("tool");

            for (int i = 0; i < xtools.Count; ++i)
            {
                string name = xtools[i].SelectSingleNode("name").InnerText;
                int number = int.Parse(xtools[i].SelectSingleNode("number").InnerText);
                int offset = int.Parse(xtools[i].SelectSingleNode("offset").InnerText);
                double length = double.Parse(xtools[i].SelectSingleNode("length").InnerText);
                double diameter = double.Parse(xtools[i].SelectSingleNode("diameter").InnerText);
                int feed = int.Parse(xtools[i].SelectSingleNode("maxfeed").InnerText);
                int speed = int.Parse(xtools[i].SelectSingleNode("maxspeed").InnerText);

                tools.Add(new GH_MachineTool(new MachineTool(name, diameter, number, offset, length, feed, speed, feed / 3)));
            }

            DA.SetDataList(0, tools);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_LoadMachineTools_24x24;

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("68a397ae-a8c4-47ca-8c37-e70c67a57993"); }
        }
    }
}