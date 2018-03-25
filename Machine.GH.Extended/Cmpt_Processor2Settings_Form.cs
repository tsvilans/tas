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


namespace tas.Machine.GH.Extended
{
    public partial class ABBPostComponent_Form : Form
    {
        ToolpathSettings settings;
        GH_Component component;

        public ABBPostComponent_Form(
            GH_Component _component,
            ToolpathSettings _settings)
        {
            settings = _settings;
            component = _component;

            InitializeComponent();
            this.CancelButton = button_cancel;
            
        }

        private void tasTP_Processor2Settings_Form_Load(object sender, EventArgs e)
        {
            this.num_safez.Value = (decimal)settings.SafeZ;
            this.num_rapidz.Value = (decimal)settings.RapidZ;
            this.num_feedrate.Value = (decimal)settings.FeedRate;
            this.num_rapidrate.Value = (decimal)settings.RapidRate;
            this.ext_axis_checkbox.Checked = settings.UseExternalAxis;
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            settings.SafeZ = (double)this.num_safez.Value;
            settings.RapidZ = (double)this.num_rapidz.Value;
            if (settings.RapidZ < settings.SafeZ) settings.RapidZ = settings.SafeZ;

            settings.FeedRate = (double)this.num_feedrate.Value;
            settings.RapidRate = (double)this.num_rapidrate.Value;
            settings.PlungeRate = settings.FeedRate / 3;
            settings.UseExternalAxis = this.ext_axis_checkbox.Checked;
            component.ExpireSolution(true);
            this.Close();
            this.Dispose();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void num_rapidz_ValueChanged(object sender, EventArgs e)
        {
            if (num_rapidz.Value < num_safez.Value) num_rapidz.Value = num_safez.Value;
        }

    }
}
