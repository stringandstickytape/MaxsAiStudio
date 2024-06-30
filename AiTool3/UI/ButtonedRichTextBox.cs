using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;

namespace AiTool3.UI
{
    [Designer(typeof(System.Windows.Forms.Design.ControlDesigner))]
    public class ButtonedRichTextBox : RichTextBox
    {
        private List<MegaBar> megaBars = new List<MegaBar>();
        private bool isMouseOver = false;
        private Point mousePosition;

        private Bitmap buttonLayer;
        private System.Timers.Timer flashTimer;
        private int flashCount = 0;

        private Color currentBackColor;
        private int fadeDuration = 2000; // Total fade duration in milliseconds
        private int fadeInterval = 100; // Interval between color updates in milliseconds
        private int fadeProgress = 0;

        [Category("Behavior")]
        [Description("Determines whether the control should flash when text is updated.")]
        public bool FlashOnUpdate { get; set; }


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
            this.QueryContinueDrag += ButtonedRichTextBox_QueryContinueDrag;
            flashTimer = new System.Timers.Timer(fadeInterval);
            flashTimer.Elapsed += FadeTimer_Elapsed;
            flashTimer.AutoReset = true;
            buttonLayer = new Bitmap(1, 1);

        }

        private void FadeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateFade()));
            }
            else
            {
                UpdateFade();
            }
        }

        private void UpdateFade()
        {
            fadeProgress += fadeInterval;
            if (fadeProgress >= fadeDuration)
            {
                BackColor = Color.Black;
                flashTimer.Stop();
            }
            else
            {
                float progress = (float)fadeProgress / fadeDuration;
                BackColor = InterpolateColor(backColorHighlight, Color.Black, progress);
                Invalidate();
            }
        }

        private Color InterpolateColor(Color start, Color end, float progress)
        {
            int r = (int)(start.R + (end.R - start.R) * progress);
            int g = (int)(start.G + (end.G - start.G) * progress);
            int b = (int)(start.B + (end.B - start.B) * progress);
            return Color.FromArgb(r, g, b);
        }


        public override string Text
        {
            get { return base.Text; }
            set
            {
                if (base.Text != value)
                {
                    base.Text = value;
                    if (FlashOnUpdate)
                    {
                        FlashBackground();
                    }
                }
            }
        }

        private Color backColorHighlight = Color.FromArgb(20, 90, 30);

        private void FlashBackground()
        {
            if (BackColor == Color.Black)
            {
                BackColor = backColorHighlight;
                fadeProgress = 0;
                flashTimer.Interval = fadeInterval;
                flashTimer.Start();
            }
            else fadeProgress = 0;
        }


        private void ButtonedRichTextBox_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            if (e.Action == DragAction.Drop && ModifierKeys == Keys.Control)
            {
                e.Action = DragAction.Cancel;
                PasteWithoutFormatting();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.Control && e.KeyCode == Keys.V)
                ||
                (e.Shift && e.KeyCode == Keys.Insert))
            {
                e.Handled = true;
                PasteWithoutFormatting();
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        private void PasteWithoutFormatting()
        {
            if (Clipboard.ContainsText())
            {
                SuspendLayout();
                int selectionStart = SelectionStart;
                string text = Clipboard.GetText(TextDataFormat.Text);
                SelectedText = text;
                SelectionStart = selectionStart + text.Length;
                SelectionLength = 0;
                ResumeLayout();
            }
        }

        public new void Clear()
        {
            base.Clear();
            megaBars.Clear();
            UpdateButtonLayer();
            Invalidate();
        }

        public void AddMegaBar(int startIndex, MegaBarItem[] items)
        {
            megaBars.Add(new MegaBar { StartIndex = startIndex, Items = items });
            Invalidate();
            UpdateButtonLayer();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isMouseOver && buttonLayer != null)
            {
                e.Graphics.DrawImage(buttonLayer, 0, 0);
            }
        }

        private void UpdateButtonLayer()
        {
            if (Width <= 0 || Height <= 0)
                return;

            buttonLayer?.Dispose();
            buttonLayer = new Bitmap(Width, Height);

            using (Graphics g = Graphics.FromImage(buttonLayer))
            {
                g.Clear(Color.Transparent);
                foreach (var megaBar in megaBars)
                {
                    DrawMegaBar(g, megaBar);
                }
            }
        }

        private void DrawMegaBar(Graphics g, MegaBar megaBar)
        {
            Pen buttonBorder = Pens.Yellow;
            Brush highlightColour = Brushes.Gray;
            Brush backgroundColour = Brushes.Black;

            int x = GetPositionFromCharIndex(megaBar.StartIndex).X + 5;
            int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

            foreach (var item in megaBar.Items)
            {
                int buttonWidth = GetStringWidth(item.Title, Font) + 20; // Add padding
                Rectangle rectangle = new Rectangle(x, y, buttonWidth, 22);
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
                x += buttonWidth + 5; // Add spacing between buttons
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (isMouseOver)
            {
                foreach (var megaBar in megaBars)
                {
                    int x = GetPositionFromCharIndex(megaBar.StartIndex).X + 5;
                    int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

                    foreach (var item in megaBar.Items)
                    {
                        int buttonWidth = GetStringWidth(item.Title, Font) + 20;
                        Rectangle buttonRect = new Rectangle(x, y, buttonWidth, 22);
                        if (buttonRect.Contains(e.Location))
                        {
                            item.Callback?.Invoke();
                            return;
                        }
                        x += buttonWidth + 5;
                    }
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isMouseOver = true;
            UpdateButtonLayer();
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
            UpdateButtonLayer();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mousePosition = e.Location;
            bool needsRedraw = false;

            foreach (var megaBar in megaBars)
            {
                int x = GetPositionFromCharIndex(megaBar.StartIndex).X + 5;
                int y = GetPositionFromCharIndex(megaBar.StartIndex).Y;

                foreach (var item in megaBar.Items)
                {
                    int buttonWidth = GetStringWidth(item.Title, Font) + 20;
                    Rectangle buttonRect = new Rectangle(x, y, buttonWidth, 22);
                    bool isOver = buttonRect.Contains(e.Location);
                    if (isOver != item.IsMouseOver)
                    {
                        item.IsMouseOver = isOver;
                        needsRedraw = true;
                    }
                    x += buttonWidth + 5;
                }
            }

            if (needsRedraw)
            {
                UpdateButtonLayer();
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

        private int GetStringWidth(string text, Font font)
        {
            using (var g = CreateGraphics())
            {
                return (int)Math.Ceiling(g.MeasureString(text, font, int.MaxValue, StringFormat.GenericTypographic).Width);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateButtonLayer();
        }

        protected override void Dispose(bool disposing)
        {
            buttonLayer?.Dispose();
            base.Dispose(disposing);
        }

// Other overridden methods remain unchanged
protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); }
        protected override void OnVScroll(EventArgs e) { base.OnVScroll(e); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); }
        protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); }
    }
}