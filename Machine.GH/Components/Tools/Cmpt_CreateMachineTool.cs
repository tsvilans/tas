﻿using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace tas.Machine.GH.Components
{
    public class Cmpt_CreateMachineTool : GH_Component
    {
        public Cmpt_CreateMachineTool()
          : base("Create Machine Tool", "Machine Tool",
              "Create a machine tool from tool data.",
              "tasMachine", UiNames.ToolSection)
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Name", "N", "Name of tool.", GH_ParamAccess.item, "MachineTool");
            pManager.AddNumberParameter(
                "Diameter", "D", "Tool diameter.", GH_ParamAccess.item, 12.0);
            pManager.AddNumberParameter(
                "StepOver", "SO", "Stepover.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter(
                "StepDown", "SD", "Stepdown.", GH_ParamAccess.item, 6.0);
            pManager.AddIntegerParameter(
                "Number ID", "N", "Tool number.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter(
                "Height ID", "H", "Tool height offset Id.", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter(
                "Length", "L", "Tool length.", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter(
                "Feedrate", "F", "Tool cutting rate.", GH_ParamAccess.item, 200);
            pManager.AddIntegerParameter(
                "Plunge", "P", "Tool plunge rate.", GH_ParamAccess.item, 100);
            pManager.AddIntegerParameter(
                "Speed", "S", "Tool spindle speed.", GH_ParamAccess.item, 10000);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("MachineTool", "T", "Machine tool data.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize variables
            string m_name = default;
            double m_diameter = default,
                m_length = default,
                m_stepover = default,
                m_stepdown = default;
            int m_number_id = default, 
                m_offset_id = default, 
                m_feedrate = default,
                m_plunge = default, 
                m_speed = default;

            // Harvest inputs
            DA.GetData("Name", ref m_name);
            DA.GetData("Diameter", ref m_diameter);
            DA.GetData("StepOver", ref m_stepover);
            DA.GetData("StepDown", ref m_stepdown);
            DA.GetData("Number ID", ref m_number_id);
            DA.GetData("Height ID", ref m_offset_id);
            DA.GetData("Length", ref m_length);
            DA.GetData("Feedrate", ref m_feedrate);
            DA.GetData("Plunge", ref m_plunge);
            DA.GetData("Speed", ref m_speed);


            // Check for invalid inputs
            if (string.IsNullOrEmpty(m_name))
                m_name = "DefaultMachineTool";
            if (m_diameter <= 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tool diameter.");
                return;
            }
            if (m_length < 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tool length.");
                return;
            }

            var mt = new MachineTool()
            {
                Name = m_name,
                Diameter = m_diameter,
                Number = m_number_id,
                OffsetNumber = m_offset_id,
                Length = m_length,
                FeedRate = m_feedrate,
                SpindleSpeed = m_speed,
                PlungeRate = m_plunge,
                StepDown = m_stepdown,
                StepOver = m_stepover
            };

            // Output machine tool
            DA.SetData("MachineTool", new GH_MachineTool(mt));

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.tas_icons_CreateMachineTool_24x24;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9a9eea42-d54d-477b-8d32-0064a386372a"); }
        }
    }
}