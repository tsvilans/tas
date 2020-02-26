using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

using tas.Core;
using tas.Machine;
using tas.Machine.GH;

namespace tas.Projects.DMI.GH
{
    public class Cmpt_VisualizeHaas : GH_Component
    {
        public Cmpt_VisualizeHaas()
          : base("Visualize Haas", "VizHaas",
              "Visualize Haas TM-3 milling centre.",
              "tasTools", "Machining")
        {
            this.Message = "Booting up...";
            xforms = new Transform[DOF + 2];
            meshes = new Mesh[DOF + 2];
            m_machine_meshes = new List<Mesh>();

            machine_part_xforms = new Transform[DOF + 2];

            // Initialize arrays
            for (int i = 0; i < xforms.Length; ++i)
                xforms[i] = Transform.Identity;

            for (int i = 0; i < meshes.Length; ++i)
                meshes[i] = null;

            for (int i = 0; i < machine_part_xforms.Length; ++i)
                machine_part_xforms[i] = Transform.Identity;

            // Hard-code machine part transformations (could also just move the meshes in place)
            machine_part_xforms[0] = Transform.Translation(new Vector3d(0, 0, -506));
            machine_part_xforms[1] = Transform.Translation(new Vector3d(-508, -254, -506));
            machine_part_xforms[2] = Transform.Translation(new Vector3d(-508, -254, -506));
            machine_part_xforms[3] = Transform.Translation(new Vector3d(0, 0, -101.6));
            machine_part_xforms[4] = Transform.Translation(new Vector3d(0, 0, 0));

        }

        int DOF = 3;

        // Create transform and mesh arrays
        Transform[] xforms;
        Mesh[] meshes;
        List<Mesh> m_machine_meshes;

        int[] wp_relations = // which of the DOFs is the workpiece linked to
            new int[]{
                0, 1, 2 // base, bedX, bedY
          };
        int[][] axis_relations = new int[][]{ // declare which axes depend on which other ones
          new int[]{0},
          new int[]{1,0},
          new int[]{2,1,0},
          new int[]{3,0},
          new int[]{4,3,0}};
        Transform[] machine_part_xforms;

        // HAAS LIMITS ---------
        Interval[] m_limits = new Interval[]{
            new Interval(-1016, 0),
            new Interval(-508, 0),
            new Interval(-406, 0)
            };

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Baseplane", "B", "Baseplane of the whole machine. Use this to move the machine around.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddPlaneParameter("Workplane", "W", "Workplane to define G54 work offset, relative to the world origin.", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddPointParameter("Point", "P", "Tool position point to visualize.", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddGenericParameter("MachineTool", "MT", "Active machine tool.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Toolpath", "TP", "Active toolpath.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Stock mesh", "M", "Mesh of workpiece material.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Machine meshes", "MM", "Meshes of machine components", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Reset", "R", "Reset visualization engine.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Path", "P", "Transformed toolpath as curve.", GH_ParamAccess.item);
            pManager.AddGenericParameter("Toolpath", "TP", "Transformed toolpath for visualization.", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Work offset", "G54", "Work offset relative to the machine.", GH_ParamAccess.item);

            pManager.AddMeshParameter("Stock mesh", "M", "Transformed mesh of workpiece material.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Machine meshes", "MM", "Transformed meshes of machine components", GH_ParamAccess.list);

        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane m_baseplane = Plane.WorldXY;
            DA.GetData("Baseplane", ref m_baseplane);

            Plane m_workplane = Plane.WorldXY;
            DA.GetData("Workplane", ref m_workplane);

            Point3d m_point = Point3d.Origin;
            DA.GetData("Point", ref m_point);

            MachineTool m_machine_tool = null;
            DA.GetData("MachineTool", ref m_machine_tool);

            Toolpath m_toolpath = null;
            DA.GetData("Toolpath", ref m_toolpath);

            Mesh m_stock = null;
            DA.GetData("Stock mesh", ref m_stock);

            bool m_reset = false;
            DA.GetData("Reset", ref m_reset);

            Toolpath tpViz = null; int tpVizN = 0;
            if (m_toolpath != null)
                tpViz = m_toolpath.Duplicate();

            // Transform the workpiece and TCP to the G54 work offset
            Transform G54_xform = Transform.PlaneToPlane(Plane.WorldXY, m_workplane);
            m_stock.Transform(G54_xform);

            m_point.Transform(G54_xform);

            if (tpViz != null)
                tpVizN = XFORM(tpViz, G54_xform);

            // Machine coordinates
            var coords = new double[]{
              m_point.X,
              m_point.Y,
              m_point.Z + m_machine_tool.Length};

            bool is_good;

            for (int i = 0; i < 3; ++i)
                if (!m_limits[i].IncludesParameter(coords[i]))
                {
                    is_good = false;
                }
            is_good = true;

            if (!IsInMachineLimits(coords))
                this.Message = "Out of bounds!";
            else
                this.Message = "";

            if (m_reset)
            {
                m_machine_meshes = new List<Mesh>();
                DA.GetDataList("Machine meshes", m_machine_meshes);

                // Hard-code machine part transformations (could also just move the meshes in place)
                machine_part_xforms[0] = Transform.Translation(new Vector3d(0, 0, -506));
                machine_part_xforms[1] = Transform.Translation(new Vector3d(-508, -254, -506));
                machine_part_xforms[2] = Transform.Translation(new Vector3d(-508, -254, -506));
                machine_part_xforms[3] = Transform.Translation(new Vector3d(0, 0, -101.6));
                machine_part_xforms[4] = Transform.Translation(new Vector3d(0, 0, 0));
            }

                // Hard-code meshes
                meshes[0] = // chassis
                  m_machine_meshes[0].DuplicateMesh();
                meshes[2] = // bed (moves in XY)
                  m_machine_meshes[1].DuplicateMesh();
                meshes[3] = // tower
                  m_machine_meshes[2].DuplicateMesh();
                meshes[4] = //new Mesh();
                  m_machine_meshes[3].DuplicateMesh(); // tool
                meshes[4].Append(
                  Mesh.CreateFromCylinder(new Cylinder(new Circle(
                  new Plane(
                  new Point3d(0, 0, 0),
                  -Vector3d.XAxis, Vector3d.YAxis),
                  m_machine_tool.Diameter / 2), m_machine_tool.Length), 4, 24));


            // Pre-move all the machine parts in place
            for (int i = 0; i < machine_part_xforms.Length; ++i)
            {
                if (meshes[i] == null) continue;
                meshes[i].Transform(machine_part_xforms[i]);
            }
            
            // Global transform
            xforms[0] = Transform.PlaneToPlane(Plane.WorldXY, m_baseplane);

            // Convert point to transforms
            xforms[1] = Transform.Translation(new Vector3d(-m_point.X, 0, 0));
            xforms[2] = Transform.Translation(new Vector3d(0, -m_point.Y, 0));
            xforms[3] = Transform.Translation(new Vector3d(0, 0, m_point.Z + m_machine_tool.Length));

            // Transform machine parts
            for (int i = meshes.Length - 1; i >= 0; --i)
            {
                if (meshes[i] == null) continue;
                for (int j = 0; j < axis_relations[i].Length; ++j)
                {
                    meshes[i].Transform(xforms[axis_relations[i][j]]);
                }
            }

            // Transform workpiece
            Transform total = G54_xform;
            for (int i = wp_relations.Length - 1; i >= 0; --i)
            {
                m_stock.Transform(xforms[wp_relations[i]]);
                m_workplane.Transform(xforms[wp_relations[i]]);
                if (tpViz != null)
                {
                    tpVizN = XFORM(tpViz, xforms[wp_relations[i]]);
                }
                total = Transform.Multiply(total, xforms[wp_relations[i]]);
            }

            DA.SetData("Path", new Polyline(tpViz.Paths.SelectMany(x => x.Select(y => y.Plane.Origin))));
            DA.SetData("Toolpath", new GH_Toolpath(tpViz));
            DA.SetData("Work offset", m_workplane);

            DA.SetData("Stock mesh", m_stock);
            DA.SetDataList("Machine meshes", meshes);
        }

        protected bool IsInMachineLimits(double[] coords)
        {
            if (coords.Length != 3) throw new Exception("Invalid DOF in IsInMachineLimits()!");

            for (int i = 0; i < 3; ++i)
                if (!m_limits[i].IncludesParameter(coords[i]))
                    return false;
            return true;
        }

        // Waypoint transformation
        public int XFORM(Toolpath tp, Transform xform)
        {
            int N = 0;
            for (int i = 0; i < tp.Paths.Count; ++i)
            {
                //Path new_path = new Path();
                for (int j = 0; j < tp.Paths[i].Count; ++j)
                {
                    Waypoint wp = tp.Paths[i][j];

                    if (XFORM(ref wp, xform))
                        ++N;

                    tp.Paths[i][j] = wp;
                }
            }

            return N;
        }

        public bool XFORM(ref Waypoint wp, Transform xform)
        {
            Plane p = wp.Plane;

            if (!xform.IsValid)
                throw new System.Exception(string.Format("Bad xform: {0} {1}", xform.IsValid, xform));

            if (!p.Transform(xform))
                return false;

            wp.Plane = p;
            return true;
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
            get { return new Guid("{99505f11-64c5-4746-b691-57b5366b8907}"); }
        }
    }
}
