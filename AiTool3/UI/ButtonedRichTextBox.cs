using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AiTool3.UI
{
    [Designer(typeof(System.Windows.Forms.Design.ControlDesigner))]
    public class ButtonedRichTextBox : RichTextBox
    {
        private List<MegaBar> megaBars = new List<MegaBar>();
        private bool isMouseOver = false;
        private Point mousePosition;

        public class MegaBarItem
        {
            public string Title { get; set; }
            public Action Callback { get; set; }
            public bool IsMouseOver { get; set; }
        }

        private class MegaBar
        {
            public int StartIndex { get; set; }
            public MegaBarItem[] Items { get; set; }
        }

        public ButtonedRichTextBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        public new void Clear()
        {
            base.Clear();
            megaBars.Clear();
            Invalidate();
        }

        public void AddMegaBar(int startIndex, MegaBarItem[] items)
        {
            megaBars.Add(new MegaBar { StartIndex = startIndex, Items = items });
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isMouseOver)
            {
                foreach (var megaBar in megaBars)
                {
                    DrawMegaBar(e.Graphics, megaBar);
                }
            }
        }

        private void DrawMegaBar(Graphics g, MegaBar megaBar)
        {
            int x = GetPositionFromCharIndex(megaBar.StartIndex).X + 5;
            int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

            Pen buttonBorder = Pens.Yellow;
            Brush highlightColour = Brushes.Gray;
            Brush backgroundColour = Brushes.Black;

            foreach (var item in megaBar.Items)
            {
                Rectangle rectangle = new Rectangle(x, y, 100, 22);
                int radius = 10;
                int diameter = radius * 2;
                Rectangle arc = new Rectangle(rectangle.Location, new Size(diameter, diameter));

                Brush backgroundBrush = item.IsMouseOver ? highlightColour : backgroundColour;
                g.FillRectangle(backgroundBrush, rectangle);

                g.DrawArc(buttonBorder, arc, 180, 90);
                arc.X = rectangle.Right - diameter;
                g.DrawArc(buttonBorder, arc, 270, 90);
                arc.Y = rectangle.Bottom - diameter;
                g.DrawArc(buttonBorder, arc, 0, 90);
                arc.X = rectangle.Left;
                g.DrawArc(buttonBorder, arc, 90, 90);

                g.DrawLine(buttonBorder, rectangle.Left + radius, rectangle.Top, rectangle.Right - radius, rectangle.Top);
                g.DrawLine(buttonBorder, rectangle.Right, rectangle.Top + radius, rectangle.Right, rectangle.Bottom - radius);
                g.DrawLine(buttonBorder, rectangle.Left + radius, rectangle.Bottom, rectangle.Right - radius, rectangle.Bottom);
                g.DrawLine(buttonBorder, rectangle.Left, rectangle.Top + radius, rectangle.Left, rectangle.Bottom - radius);

                g.DrawString(item.Title, Font, Brushes.White, rectangle, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                x += 105;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (isMouseOver)
            {
                foreach (var megaBar in megaBars)
                {
                    int x = GetPositionFromCharIndex(megaBar.StartIndex).X;
                    int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

                    for (int i = 0; i < megaBar.Items.Length; i++)
                    {
                        Rectangle buttonRect = new Rectangle(x + (i * 105), y, 100, 25);
                        if (buttonRect.Contains(e.Location))
                        {
                            megaBar.Items[i].Callback?.Invoke();
                            return;
                        }
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isMouseOver = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isMouseOver = false;
            foreach (var megaBar in megaBars)
            {
                foreach (var item in megaBar.Items)
                {
                    item.IsMouseOver = false;
                }
            }
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mousePosition = e.Location;
            bool needsRedraw = false;

            foreach (var megaBar in megaBars)
            {
                int x = GetPositionFromCharIndex(megaBar.StartIndex).X;
                int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

                for (int i = 0; i < megaBar.Items.Length; i++)
                {
                    Rectangle buttonRect = new Rectangle(x + (i * 105), y, 100, 25);
                    bool isOver = buttonRect.Contains(e.Location);
                    if (isOver != megaBar.Items[i].IsMouseOver)
                    {
                        megaBar.Items[i].IsMouseOver = isOver;
                        needsRedraw = true;
                    }
                }
            }

            if (needsRedraw)
            {
                Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x00f || m.Msg == 0x0085) // WM_PAINT or WM_NCPAINT
            {
                using (var g = Graphics.FromHwnd(Handle))
                {
                    OnPaint(new PaintEventArgs(g, ClientRectangle));
                }
            }
        }

        // Other overridden methods remain unchanged
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); }
        protected override void OnVScroll(EventArgs e) { base.OnVScroll(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); }
    }
}