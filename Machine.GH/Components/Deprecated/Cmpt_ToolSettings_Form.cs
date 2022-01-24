using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

using Grasshopper.Kernel;

namespace tas.Machine.GH
{
    public partial class tasTP_ToolSettings_Form : Form
    {
        ToolSettings settings;
        GH_Component component;

        public tasTP_ToolSettings_Form(
            GH_Component _component,
            ToolSettings _settings)
        {
            settings = _settings;
            component = _component;

            InitializeComponent();
            this.CancelButton = button_cancel;
        }

        private void tasTP_Processor2Settings_Form_Load(object sender, EventArgs e)
        {
            this.num_tooldiameter.Value = (decimal)settings.ToolDiameter;
            this.num_stepdown.Value = (decimal)settings.StepDown;
            this.num_stepover.Value = (decimal)settings.StepOver;
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            settings.ToolDiameter = (double)this.num_tooldiameter.Value;
            settings.StepDown = (double)this.num_stepdown.Value;
            settings.StepOver = (double)this.num_stepover.Value;
            component.ExpireSolution(true);
            this.Close();
            this.Dispose();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
