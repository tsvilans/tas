using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;

namespace tas.Machine.GH
{
    public class ToolSeetingsForm: Form
    {
        CheckBox box1 = new CheckBox();
        Spinner spinnerStepDown = new Spinner();
        Spinner spinnerStepOver = new Spinner();
        Spinner spinnerToolDiameter = new Spinner();

        GH_Component m_component;
        ToolSettings m_toolsettings;

        public ToolSeetingsForm(GH_Component component, ToolSettings settings)
        {
            // sets the client (inner) size of the window for your content
            this.ClientSize = new Eto.Drawing.Size(100, 200);
            this.Title = "Tool Settings";

            m_component = component;
            m_toolsettings = settings;


        }
    }
}
