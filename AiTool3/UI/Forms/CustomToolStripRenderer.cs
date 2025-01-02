using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.UI.Forms
{
    public class CustomProfessionalColorTable : ProfessionalColorTable
    {
        public Color SelectedMenuItemBackColor { get; set; }
        public CustomProfessionalColorTable()
        {
            SelectedMenuItemBackColor = Color.LightBlue;
        }
        public override Color MenuItemSelected
        {
            get { return SelectedMenuItemBackColor; }
        }
        public override Color MenuItemSelectedGradientBegin
        {
            get { return SelectedMenuItemBackColor; }
        }
        public override Color MenuItemSelectedGradientEnd
        {
            get { return SelectedMenuItemBackColor; }
        }
        public override Color MenuItemBorder
        {
            get { return Color.Red; }
        }
    }

    public class CustomToolStripRenderer : ToolStripProfessionalRenderer
    {
        public CustomToolStripRenderer(CustomProfessionalColorTable customColorTable)
            : base(customColorTable)
        {
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                var customColorTable = (CustomProfessionalColorTable)ColorTable;
                e.Graphics.FillRectangle(new SolidBrush(customColorTable.SelectedMenuItemBackColor), new Rectangle(Point.Empty, e.Item.Size));

                // check if item is a TemplateMenuItem
                if (e.Item is TemplateMenuItem tmi)
                {
                    if (tmi.IsSelected)
                    {
                        using (Pen p = new Pen(Color.Red))
                        {
                            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            // draw a border around the menu item
                            e.Graphics.DrawRectangle(p, new Rectangle(1, 1, e.Item.Width - 3, e.Item.Height - 3));
                        }
                    }
                }

                
            }
            else
            {
                base.OnRenderMenuItemBackground(e);
            }
        }
    }
}
