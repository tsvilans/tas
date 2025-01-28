using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace tas.Machine.GH.Components
{
    public class Cmpt_CreateMachineTool : GH_Component, IGH_VariableParameterComponent
    {
        public Cmpt_CreateMachineTool()
          : base("Create Machine Tool", "Machine Tool",
              "Create a machine tool from tool data.",
              "tasMachine", UiNames.ToolSection)
        {
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_CreateTool;
        public override Guid ComponentGuid => new Guid("9a9eea42-d54d-477b-8d32-0064a386372a");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        readonly Dictionary<string, string> ParamNickNames = new Dictionary<string, string>{
            { "Length", "L" },
            { "Plunge", "P" },
            { "Tool ID", "Tid" },
            { "Height ID", "Hid" },
        };

        readonly IGH_Param[] parameters = new IGH_Param[4]
        {
            new Param_Number() { Name = "Length", NickName = "L", Description = "Tool length.", Optional = true, MutableNickName = false },
            new Param_Integer() { Name = "Plunge", NickName = "P", Description = "Plunge speed.", Optional = true, MutableNickName = false  },
            new Param_Integer() { Name = "Tool ID", NickName = "Tid", Description = "Tool index number.", Optional = true, MutableNickName = false  },
            new Param_Integer() { Name = "Height ID", NickName = "Hid", Description = "Height offset number.", Optional = true, MutableNickName = false  },
        };

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Length", AddLengthParam, true, Params.Input.Any(x => x.Name == "Length"));
            Menu_AppendItem(menu, "Plunge", AddPlungeParam, true, Params.Input.Any(x => x.Name == "Plunge"));
            Menu_AppendItem(menu, "Tool ID", AddToolNumberParam, true, Params.Input.Any(x => x.Name == "Tool ID"));
            Menu_AppendItem(menu, "Height ID", AddHeightOffsetParam, true, Params.Input.Any(x => x.Name == "Height ID"));
        }

        private void AddLengthParam(object sender, EventArgs e)
        {
            AddParam(0);
        }

        private void AddPlungeParam(object sender, EventArgs e)
        {
            AddParam(1);
        }

        private void AddToolNumberParam(object sender, EventArgs e)
        {
            AddParam(2);
        }
        private void AddHeightOffsetParam(object sender, EventArgs e)
        {
            AddParam(3);
        }

        private void OnCanvasFullNamesChanged() => UpdateInputNames();

        public void UpdateInputNames()
        {
            foreach (var parameter in parameters)
            {
                parameter.NickName = Grasshopper.CentralSettings.CanvasFullNames ? parameter.Name : ParamNickNames[parameter.Name];
            }
        }

        private void AddParam(int index)
        {
            IGH_Param parameter = parameters[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
            {
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            }
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(parameters, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddParameter(new Param_String() { Name = "Name", NickName = "N", Description = "Name of tool.", Access = GH_ParamAccess.item, });
            pManager.AddTextParameter(
                "Name", "N", "Name of tool.", GH_ParamAccess.item, "MachineTool");
            pManager.AddNumberParameter(
                "Diameter", "D", "Tool diameter.", GH_ParamAccess.item, 12.0);
            pManager.AddNumberParameter(
                "StepOver", "SO", "Stepover.", GH_ParamAccess.item, 6.0);
            pManager.AddNumberParameter(
                "StepDown", "SD", "Stepdown.", GH_ParamAccess.item, 6.0);
            //pManager.AddIntegerParameter(
            //"Number ID", "N", "Tool number.", GH_ParamAccess.item, 1);
            //pManager.AddIntegerParameter(
            //"Height ID", "H", "Tool height offset Id.", GH_ParamAccess.item, 1);
            //pManager.AddNumberParameter(
            //"Length", "L", "Tool length.", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter(
                "Speed", "S", "Tool spindle speed.", GH_ParamAccess.item, 10000);
            pManager.AddIntegerParameter(
                "Feedrate", "F", "Tool cutting rate.", GH_ParamAccess.item, 200);
            //            pManager.AddIntegerParameter(
            //                "Plunge", "P", "Tool plunge rate.", GH_ParamAccess.item, 100);

            Grasshopper.CentralSettings.CanvasFullNamesChanged += OnCanvasFullNamesChanged;
            UpdateInputNames();
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
            DA.GetData("Feedrate", ref m_feedrate);
            DA.GetData("Speed", ref m_speed);

            if (Params.Input.Any(x => x.Name == "Length")) DA.GetData("Length", ref m_length);
            if (Params.Input.Any(x => x.Name == "Plunge")) DA.GetData("Plunge", ref m_plunge);
            else m_plunge = (int)(m_feedrate * 0.75);
            if (Params.Input.Any(x => x.Name == "Tool ID")) DA.GetData("Tool ID", ref m_number_id);
            if (Params.Input.Any(x => x.Name == "Height ID")) DA.GetData("Height ID", ref m_offset_id);

            // Check for invalid inputs
            if (string.IsNullOrEmpty(m_name))
                m_name = "DefaultMachineTool";
            if (m_diameter <= 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tool diameter.");
                return;
            }
            /*
            if (m_length < 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid tool length.");
                return;
            }
            */
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

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
}