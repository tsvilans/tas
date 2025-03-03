using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

//using Robots;

#if !(FULL2)

namespace tas.Machine.GH
{
    public class tasTP_Processor_Component : GH_Component
    {
        public tasTP_Processor_Component()
          : base("tasToolpath: Processor", "tasTP: Process",
              "Connect multiple toolpaths together, calculate rapids, links, and lead-ins / -outs.",
              "tasTools", "Machining")
        {
        }

        Plane _workplane;
        List<Polyline> _toolpaths;
        Frame _frame;

        Brep _drive_surface;
        double _tooltwist;
        Tool _tool;
        double _feedrate;
        double _rapidrate;
        double _safe_z;
        double _rapid_z;
        double _ramp_height;

        bool _calc;
        bool _is_drive_surface;
        bool _extern;
        int _config;
        bool _flip_forearm;

        Polyline _path;
        List<Target> _targets;
        List<Plane> _planes;

        string _debug;

        /// <summary>
        ///ToolpathProcessor_v2.0
        ///Tom Svilans, April 9, 2016
        ///
        ///Description:
        ///Takes polylines and an optional reference surface and converts them to a toolpath
        ///with tool orientations, ramps, and speeds.Some things are still hard - coded, and
        ///the Toolpath class could be optimized and expanded, but it will handle most simple
        ///toolpaths.
        /// </summary>
        /// <param name="Workplane">Global output workplane for linked toolpaths.</param>
        /// <param name="Tool">Tool being used.</param>
        /// <param name="Toolpaths">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <param name="DriveSurface">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <param name="Feed rate">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <param name="Rapid rate">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <param name="Ramp height">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// <param name="Tool twist">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Output workplane for linked toolpaths.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddGenericParameter("Tool", "T", "Tool being used.", GH_ParamAccess.item);
            pManager.AddCurveParameter("Toolpaths", "TPs", "Input toolpaths to link.", GH_ParamAccess.list);
            pManager.AddBrepParameter("DriveSurface", "DSrf", "Surface to use for tool orientation.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Safe Z", "SafeZ", "Z-height for safe tool moves.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Rapid Z", "RapZ", "Z-height for rapid moves.", GH_ParamAccess.item, 20.0);
            pManager.AddNumberParameter("Feed rate", "FR", "Tool feedrate.", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Rapid rate", "RR", "Rapid speed.", GH_ParamAccess.item, 100.0);
            pManager.AddNumberParameter("Ramp height", "RH", "Height of ramp for lead-ins.", GH_ParamAccess.item, 12.0);
            pManager.AddNumberParameter("Tool twist", "TTw", "Degree of rotation around tool axis.", GH_ParamAccess.item, 0.0);
            pManager.AddPlaneParameter("Frame", "F", "Optional reference work frame.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddBooleanParameter("External axes", "Ext", "Toolpath uses external axes.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Configuration", "C", "Robot joint configuration as integer.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Calculate", "Calc", "Process toolpaths.", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Targets", "T", "Robot Targets of linked toolpath.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Path", "P", "Linked toolpath as polyline.", GH_ParamAccess.item);
            //pManager.AddPlaneParameter("Planes", "Pl", "Target planes (for debugging).", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging output.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData("Calculate", ref this._calc);
            if (!this._calc)
            {
                DA.SetDataList("Targets", this._targets);
                DA.SetData("Path", this._path);
                //DA.SetDataList("Planes", this._planes);
                DA.SetData("debug", this._debug);
                return;
            }
            this._debug = "";
            this._path = null;
            this._targets = null;
            this._planes = null;
            Plane frame_t = new Plane();

            this._toolpaths = new List<Polyline>();
            // collect inputs
            DA.GetData("Workplane", ref this._workplane);
            DA.GetData("External axes", ref this._extern);
            DA.GetData("Configuration", ref this._config);

            if (!DA.GetData("Frame", ref frame_t))
                _frame = Frame.Default;
                //_frame = new Frame(Plane.WorldXY, "tasFrame");
            else
                _frame = new Frame(frame_t, -1, -1, "tasFrame");

            Robots.Grasshopper.GH_Tool toolGH = null;
            DA.GetData("Tool", ref toolGH);
            this._tool = toolGH?.Value;

            this._debug += toolGH.ToString() + "\n";
            this._debug += toolGH.GetType().ToString() + "\n";

            if (this._tool == null)
                this._debug += "Tool conversion failed...\n";
            else
            {
                this._debug += this._tool.ToString() + "\n";
                this._debug += this._tool.GetType().ToString() + "\n";
            }

            // gather and convert, if necessary, input curves
            List<Curve> in_crvs = new List<Curve>();
            if (!DA.GetDataList("Toolpaths", in_crvs))
                return;
            this._toolpaths = tasUtility.CurvesToPolylines(in_crvs, 1.0);

            this._is_drive_surface = DA.GetData("DriveSurface", ref this._drive_surface);

            DA.GetData("Safe Z", ref this._safe_z);
            DA.GetData("Rapid Z", ref this._rapid_z);
            DA.GetData("Feed rate", ref this._feedrate);
            DA.GetData("Rapid rate", ref this._rapidrate);
            DA.GetData("Ramp height", ref this._ramp_height);
            DA.GetData("Tool twist", ref this._tooltwist);
            // /collect inputs

            // set up speeds
            Speed sp_feed = new Speed(this._feedrate, Math.PI, "feed");
            Speed sp_rapid = new Speed(this._rapidrate, Math.PI, "rapid");

            // rotate workplane to account for tool twist
            Transform rot = Transform.Rotation(this._tooltwist, _workplane.ZAxis, _workplane.Origin);
            _workplane.Transform(rot);

            // init toolpath
            Toolpath prog = new Toolpath();
            prog.External2 = this._extern;
            prog.External1 = this._extern;

            prog.Configuration = (Target.RobotConfigurations)this._config;
            prog.DefFrame = _frame;

            if (this._tool != null)
                prog.DefTool = this._tool;
            else
                this._debug += "Tool conversion failed again...\n";
            prog.DefSpeed = sp_rapid;
        prog.DefMotion = Target.Motions.Linear;

            if (this._is_drive_surface)
                prog.InputSurface = this._drive_surface;
            prog.Orientation = Toolpath.ToolOrientation.SurfaceNormal;
            prog.Workplane = this._workplane;

            for (int i = 0; i < this._toolpaths.Count; ++i)
            {
                if (this._toolpaths[i] == null)
                    continue;

                // lead in
                Polyline ramp = (Polyline)Util.CreateRamp(this._toolpaths[i], this._workplane, this._ramp_height, this._ramp_height * 5);
                Point3d p = Util.ProjectToPlane(ramp[0], prog.Workplane);
                prog.CreateTarget(p + prog.Workplane.ZAxis * (this._safe_z + this._rapid_z));
                prog.CreateTarget(p + prog.Workplane.ZAxis * (this._safe_z));

                // machining speed
                prog.DefSpeed = sp_feed;
                prog.DefMotion = Target.Motions.Linear;

                for (int j = 0; j < ramp.Count; ++j)
                {
                    prog.CreateTarget(ramp[j], Toolpath.ToolOrientation.ZAxis);
                }
                /*
                Point3d p = tasUtility.ProjectToPlane(this._toolpaths[i][0], prog.Workplane);
                Vector3d d2p = new Vector3d(p - this._toolpaths[i][0]);
                Vector3d ramp = new Vector3d(tasUtility.ProjectToPlane(this._toolpaths[i][1], prog.Workplane) - p);
                if (ramp.Length < this._ramp_height)
                    ramp = new Vector3d(tasUtility.ProjectToPlane(this._toolpaths[i][this._toolpaths[i].Count - 2], prog.Workplane) - p);
                ramp.Unitize();
                p += ramp * this._ramp_height;
                p += prog.Workplane.ZAxis * (this._safe_z + this._rapid_z);
                prog.CreateTarget(p, Toolpath.ToolOrientation.ZAxis);
                p -= prog.Workplane.ZAxis * this._rapid_z;
                p -= d2p;
                prog.CreateTarget(p, Toolpath.ToolOrientation.ZAxis);
                */


                // make toolpath targets
                for (int j = 0; j < this._toolpaths[i].Count; ++j)
                {
                    prog.CreateTarget(this._toolpaths[i][j]);
                }

                // lead out
                Point3d p2 = Util.ProjectToPlane(this._toolpaths[i][this._toolpaths[i].Count - 1], prog.Workplane);
                p2 += prog.Workplane.ZAxis * this._safe_z;
                prog.DefSpeed = sp_rapid;
                prog.CreateTarget(p2, Toolpath.ToolOrientation.ZAxis);
                p2 += prog.Workplane.ZAxis * this._rapid_z;
                //prog.DefSpeed = spr;
                prog.CreateTarget(p2, Toolpath.ToolOrientation.ZAxis);
            }

            // yield toolpath
            this._targets = prog.Targets;
            this._path = prog.Path();
            this._planes = prog.Planes;
            

        DA.SetDataList("Targets", this._targets);
        DA.SetData("Path", this._path);
        //DA.SetDataList("Planes", this._planes);
        DA.SetData("debug", this._debug);
            

  }


        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.icon_toolpath_processor_component_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{65382d6d-af67-4aa0-b100-f3d74e5ebd85}"); }
        }
    }
}


#endif