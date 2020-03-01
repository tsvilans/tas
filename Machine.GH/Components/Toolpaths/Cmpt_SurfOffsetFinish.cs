using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using ClipperLib;
using StudioAvw.Geometry;

using tas.Core;
using tas.Core.Util;

namespace tas.Machine.GH.Toolpaths
{
    public class Cmpt_SurfOffsetFinish : GH_Component
    {

        public Cmpt_SurfOffsetFinish()
          : base("Finishing - Surface Offset ", "Surf Offset Finish",
              "Simple surface offset finishing toolpath strategy.",
              "tasTools", "Toolpaths")
        {
        }

        Plane _workplane;
        List<GeometryBase> _boundaries;
        GeometryBase _geometry;
        double _tool_d;
        double _step_o;
        double _resolution;
        double _min_z;
        bool _calc;
        bool _simplify;

        string _debug;
        List<Polyline> _paths;

        // Plane Workplane, Mesh Stock, Mesh Geometry, double MaxDepth, 
        // double Stepover, double Stepdown, bool Calculate, ref object Toolpath
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Workplane", "WP", "Workplane of toolpath.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddGeometryParameter("Boundary", "Bnd", "Boundary to constrain toolpath to.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry", "Geo", "Drive geometry as GeometryBase.", GH_ParamAccess.item);
            pManager.AddNumberParameter("ToolDiameter", "TD", "Diameter of cutter.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter("Stepover", "StpO", "Stepover distance.", GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("Resolution", "Res", "Resolution at which to decimate curves.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("MaxDepth", "MinZ", "Maximum depth to go to.", GH_ParamAccess.item, -40.0);
            pManager.AddBooleanParameter("Simplify Output", "S", "Simplify the resultant path to avoid redundant points.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Calculate", "Calc", "Calculate toolpath.", GH_ParamAccess.item, false);

            pManager[1].Optional = true;

        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Toolpath", "TP", "Output toolpath.", GH_ParamAccess.list);
            pManager.AddTextParameter("debug", "d", "Debugging info.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData("Calculate", ref this._calc);
            if (this._calc)
            {
                this._boundaries = new List<GeometryBase>();
                // get inputs
                DA.GetData("Workplane", ref this._workplane);
                DA.GetDataList("Boundary", this._boundaries);
                if (!DA.GetData("Geometry", ref this._geometry))
                    return;
                DA.GetData("Stepover", ref this._step_o);
                DA.GetData("ToolDiameter", ref this._tool_d);
                DA.GetData("Stepover", ref this._step_o);
                DA.GetData("Resolution", ref this._resolution);
                DA.GetData("MaxDepth", ref this._min_z);
                DA.GetData("Simplify Output", ref this._simplify);

                this._debug = "Begin...";

                this._paths = new List<Polyline>();
                Mesh[] meshes;
                Mesh mesh = new Mesh();
                List<Polyline> boundaries = new List<Polyline>();

                for (int i = 0; i < this._boundaries.Count; ++i)
                {
                    if (this._boundaries[i].ObjectType == Rhino.DocObjects.ObjectType.Curve)
                    {
                        Curve c = Curve.ProjectToPlane(this._boundaries[i] as Curve, this._workplane);
                        Polyline pltemp;
                        Polyline3D.ConvertCurveToPolyline(c, out pltemp);
                        boundaries.Add(pltemp);
                    }
                    else if (this._boundaries[i].ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh m = this._boundaries[i] as Mesh;
                        boundaries.AddRange(m.GetOutlines(this._workplane));
                    }
                    else if (this._boundaries[i].ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Surface s = this._boundaries[i] as Surface;

                    }
                }
                List<Polyline> bpl;
                //bpl = Polyline3D.ConvertCurvesToPolyline(boundaries).ToList();
                bpl = boundaries;

                if (this._geometry.ObjectType == Rhino.DocObjects.ObjectType.Brep)
                {
                    meshes = Mesh.CreateFromBrep(this._geometry as Brep, MeshingParameters.QualityRenderMesh);
                }
                else if (this._geometry.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    meshes = Mesh.CreateFromBrep(this._geometry as Brep, MeshingParameters.QualityRenderMesh);
                }
                else if (this._geometry.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    meshes = new Mesh[1] { this._geometry as Mesh };
                }
                else
                {
                    this._debug += "Couldn't convert geometry...\n";
                    DA.SetData("debug", this._debug);
                    return;
                }

                for (int i = 0; i < meshes.Length; ++i)
                {
                    mesh.Append(meshes[i]);
                }

                //List<Polyline> silhouette = new List<Polyline>();
                Polyline[] mout = mesh.GetOutlines(this._workplane);
                if (mout == null)
                {
                    this._debug += "Failed to create mesh outline...\n";
                    DA.SetData("debug", this._debug);
                    return;
                }

                List<Polyline> area = Polyline3D.Boolean(ClipType.ctIntersection, bpl,
                    mout, this._workplane, 0.01, true);

                List<Polyline> ppaths = new List<Polyline>();
                //ppaths.AddRange(area);
                ppaths.AddRange(tas.Core.Util.Misc.InsetUntilNone(area, this._step_o, this._workplane));

                Polyline pl = new Polyline();

                // for every polyline in ppaths, project it back onto the geometry
                for (int i = 0; i < ppaths.Count; ++i)
                {
                    // reconstruct polyline at appropriate sampling density
                    List<Point3d> vlist = new List<Point3d>();
                    for (int j = 0; j < ppaths[i].Count - 1; ++j)
                    {
                        vlist.Add(ppaths[i][j]);
                        Vector3d dv = new Vector3d(ppaths[i][j] - ppaths[i][j + 1]);
                        if (dv.Length > this._resolution)
                        {
                            int div = (int)(dv.Length / this._resolution + 1);
                            for (int k = 0; k < div; ++k)
                            {
                                double t = k / (double)(div - 1);
                                vlist.Add(Interpolation.Lerp(ppaths[i][j], ppaths[i][j + 1], t));
                            }
                        }

                    }
                    vlist.Add(ppaths[i][ppaths[i].Count - 1]);

                    // remove double
                    for (int j = vlist.Count - 1; j > 0; --j)
                    {
                        Vector3d v = vlist[j] - vlist[j - 1];
                        if (v.Length < 1e-12) vlist.RemoveAt(j);
                    }

                    Polyline temp_pl = new Polyline(vlist);

                    // project point back onto surface
                    Ray3d ray;
                    double d;
                    for (int j = 0; j < temp_pl.Count; ++j)
                    {
                        ray = new Ray3d(temp_pl[j], -this._workplane.ZAxis);
                        d = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, ray);
                        temp_pl[j] = temp_pl[j] + this._workplane.ZAxis * -d;
                        MeshPoint mp = mesh.ClosestMeshPoint(temp_pl[j], 10.0);
                        Vector3d n = mesh.NormalAt(mp);
                        // adjust points based on surface normal (to compensate for endmill radius)
                    }
                    if (this._simplify)
                        temp_pl = temp_pl.SimplifyPolyline(0.00001);

                    this._paths.Add(temp_pl);
                }
            }

                    // output data
            if (this._paths != null)
                DA.SetDataList("Toolpath", this._paths);
            DA.SetData("debug", this._debug);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_SurfaceOffset_24x24;

            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{0277865d-a19d-4b16-91f9-a14cff728360}"); }
        }
    }
}