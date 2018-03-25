using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Grasshopper.GUI;
using Grasshopper.GUI.Base;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

namespace tas.Core.GH
{
    public class Cmpt_Bitmask : GH_Param<GH_Integer>
    {
        private int NumBits = 3;

        public Cmpt_Bitmask()
          : base(new GH_InstanceDescription("Bitmask 3", "Bits3",
              "Bitmask composed of 3 bits.",
              "tasTools", "Utility"))
        {
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "3 bits", SettingsClicked, true, true);
            menu.Items.Add("3 bits", null);
            menu.Items.Add("6 bits", null);
            menu.Items.Add("8 bits", null);
            menu.Items.Add("16 bits", null);
            menu.Items.Add("31 bits", null);
            menu.ItemClicked += SettingsClicked;

            //var scroller = Menu_AppendDigitScrollerItem(menu, 1, 8, 3, 0);
            //scroller.ValueChanged += NumBitsChanged;

            //Control bit_control = new Control("Bits.");
            ////bit_control.
            //Menu_AppendCustomItem(menu, bit_control);
        }

        private void NumBitsChanged(object sender, GH_DigitScrollerEventArgs e)
        {
            NumBits = (int)e.Value;
            (m_attributes as BitMask3ObjectAttributes).SetBits(NumBits);
        }

        private void SettingsClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "3 bits" || e.ClickedItem.Text == "3 bits")
                NumBits = 3;
            else if (e.ClickedItem.Name == "6 bits" || e.ClickedItem.Text == "6 bits")
                NumBits = 6;
            else if (e.ClickedItem.Name == "8 bits" || e.ClickedItem.Text == "8 bits")
                NumBits = 8;
            else if (e.ClickedItem.Name == "16 bits" || e.ClickedItem.Text == "16 bits")
                NumBits = 16;
            else if (e.ClickedItem.Name == "31 bits" || e.ClickedItem.Text == "31 bits")
                NumBits = 31;
            (m_attributes as BitMask3ObjectAttributes).SetBits(NumBits);
            ExpireSolution(true);

        }

        public override void CreateAttributes()
        {
            m_attributes = new BitMask3ObjectAttributes(this);
            (m_attributes as BitMask3ObjectAttributes).SetBits(NumBits);
        }

        protected override Bitmap Icon
        {
            get
            {
                return Properties.Resources.icon_bitmask3_component_24x24;
            }
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{a0aa1443-f885-45dc-98f1-0a6255d01f1c}"); }
        }

        private int m_value = 0;
        public int Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        protected override void CollectVolatileData_Custom()
        {
            VolatileData.Clear();
            AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, new GH_Integer(Value & ((1 << NumBits) - 1)));
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Bitmask3", m_value);
            writer.SetInt32("NumBits", NumBits);
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            m_value = 0;
            reader.TryGetInt32("Bitmask3", ref m_value);
            if (!reader.TryGetInt32("NumBits", ref NumBits)) NumBits = 3;

            (m_attributes as BitMask3ObjectAttributes).SetBits(NumBits);
            (m_attributes as BitMask3ObjectAttributes).SetBitmask(m_value);

            return base.Read(reader);
        }
    }

    public class BitMask3ObjectAttributes : GH_Attributes<Cmpt_Bitmask>
    {

        GH_Palette palWhite = GH_Palette.White;
        GH_Palette palBlack = GH_Palette.Black;

        private int NumBits;
        private bool[] m_bits;

        public BitMask3ObjectAttributes(Cmpt_Bitmask owner)
            : base(owner)
        {
            SetBits(3);
        }

        public void SetBits(int n)
        {
            NumBits = n;
            m_bits = new bool[NumBits];
            SetBitmask(Owner.Value);
        }

        public override bool HasInputGrip { get { return false; } }
        public override bool HasOutputGrip { get { return true; } }

        private const int ButtonSize = 30;

        //Our object is always the same size, but it needs to be anchored to the pivot.
        protected override void Layout()
        {
            Pivot = GH_Convert.ToPoint(Pivot);
            int rows = NumBits / 8;
            if (((NumBits ^ 8) >= 0) && (NumBits % 8 != 0))
                rows++;

            int width = NumBits > 8 ? 8 : NumBits;
            Bounds = new RectangleF(Pivot, new SizeF(width * ButtonSize, rows * ButtonSize));
        }

        private Rectangle Button(int bit)
        {
            int x = Convert.ToInt32(Pivot.X);
            int y = Convert.ToInt32(Pivot.Y);
            int row = bit / 8;
            int rem = bit % 8;
            return new Rectangle(x + rem * ButtonSize, y + row * ButtonSize, ButtonSize, ButtonSize);
        }

        private bool Value(int bit)
        {
            return m_bits[bit];
        }

        public int GetBitmask()
        {
            int r = 0;
            for (int i = 0; i < NumBits; ++i)
                r |= (m_bits[NumBits - i - 1] ? 1 << i : 0);
            return r;
        }

        public void SetBitmask(int value)
        {
            for (int i = 0; i < NumBits; ++i)
                m_bits[NumBits - i - 1] = ((value & 1 << i) > 0 ? true : false);
        }

        public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            //On a double click we'll set the owner value.
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                for (int bit = 0; bit < NumBits; bit++)
                {
                    RectangleF button = Button(bit);
                    if (button.Contains(e.CanvasLocation))
                    {
                        m_bits[bit] = !m_bits[bit];
                        int value = GetBitmask();
                        Owner.RecordUndoEvent("Bit Change");
                        Owner.Value = value & ((1 << NumBits) - 1);
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
            }

            return base.RespondToMouseDoubleClick(sender, e);
        }

        public override void SetupTooltip(PointF point, GH_TooltipDisplayEventArgs e)
        {
            base.SetupTooltip(point, e);
            e.Description = "Double click to set a new integer";
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Objects)
            {
                //Render output grip.
                GH_CapsuleRenderEngine.RenderOutputGrip(graphics, canvas.Viewport.Zoom, OutputGrip, true);

                //Render capsules.
                for (int bit = 0; bit < NumBits; bit++)
                {
                    Rectangle button = Button(bit);
                    GH_Capsule capsule = Value(bit) ?
                        GH_Capsule.CreateTextCapsule(button, button, palWhite, "1", 0, 0)
                        :
                        GH_Capsule.CreateTextCapsule(button, button, palBlack, "0", 0, 0);

                    capsule.Render(graphics, Selected, Owner.Locked, false);
                    capsule.Dispose();

                }
            }
        }
    }
}
