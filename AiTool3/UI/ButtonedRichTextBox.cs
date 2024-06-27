using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AiTool3.UI
{
    public class ButtonBasics
    {
        public string Text { get; set; }
        public EventHandler OnClick { get; set; }
    }

    [Designer(typeof(System.Windows.Forms.Design.ControlDesigner))]
    public class ButtonedRichTextBox : RichTextBox
    {
        private List<ButtonInfo> buttons = new List<ButtonInfo>();
        private System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer();
        private bool mouseOverControl = false;
        private Point mousePosition;
        private bool buttonsVisible = false;

        public new void Clear()
        {
            base.Clear();
            buttons.Clear();
            Invalidate();
        }

        public ButtonedRichTextBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            scrollTimer.Interval = 50;
            scrollTimer.Tick += ScrollTimer_Tick;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            mouseOverControl = true;
            UpdateButtonVisibility();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            mouseOverControl = false;
            UpdateButtonVisibility();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            mousePosition = e.Location;
            UpdateButtonVisibility();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (buttonsVisible)
            {
                foreach (var button in buttons)
                {
                    if (button.Visible)
                    {
                        ButtonRenderer.DrawButton(e.Graphics, button.Bounds, button.Text, Font, false,
                            button.Pressed ? System.Windows.Forms.VisualStyles.PushButtonState.Pressed
                                           : System.Windows.Forms.VisualStyles.PushButtonState.Normal);
                    }
                }
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

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (buttonsVisible)
            {
                foreach (var button in buttons)
                {
                    if (button.Bounds.Contains(e.Location))
                    {
                        button.Pressed = true;
                        Invalidate(button.Bounds);
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (buttonsVisible)
            {
                foreach (var button in buttons)
                {
                    if (button.Pressed)
                    {
                        button.Pressed = false;
                        Invalidate(button.Bounds);
                        if (button.Bounds.Contains(e.Location))
                        {
                            button.OnClick?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        protected override void OnVScroll(EventArgs e)
        {
            base.OnVScroll(e);
            UpdateButtonPositions();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            UpdateButtonPositions();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateButtonPositions();
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            UpdateButtonPositions();
        }

        public void AddButton(int startIndex, int length, string text, EventHandler onClick)
        {
            var button = new ButtonInfo
            {
                StartIndex = startIndex,
                Length = length,
                Text = text,
                OnClick = onClick
            };
            buttons.Add(button);
            UpdateButtonPositions();
        }

        public void AddButtons(int startIndex, int length, ButtonBasics[] buttonBasics)
        {
            int currentIndex = startIndex;
            foreach (var buttonBasic in buttonBasics)
            {
                var button = new ButtonInfo
                {
                    StartIndex = currentIndex,
                    Length = length,
                    Text = buttonBasic.Text,
                    OnClick = buttonBasic.OnClick
                };
                buttons.Add(button);
                currentIndex += length;
            }
            UpdateButtonPositions();
        }

        public void RemoveButton(int startIndex, int length)
        {
            buttons.RemoveAll(b => b.StartIndex == startIndex && b.Length == length);
            Invalidate();
        }

        private void UpdateButtonPositions()
        {
            SuspendLayout();
            foreach (var button in buttons)
            {
                int startIndex = button.StartIndex;
                int endIndex = startIndex + button.Length;

                if (startIndex < 0 || endIndex > Text.Length)
                {
                    button.Visible = false;
                    continue;
                }

                Point startPoint = GetPositionFromCharIndex(startIndex);
                Point endPoint = GetPositionFromCharIndex(endIndex);

                Size buttonSize = TextRenderer.MeasureText(button.Text, Font);
                buttonSize.Width += 10;
                buttonSize.Height += 4;

                if (startPoint.Y == endPoint.Y)
                {
                    button.Bounds = new Rectangle(endPoint.X, startPoint.Y, buttonSize.Width, buttonSize.Height);
                }
                else
                {
                    button.Bounds = new Rectangle(endPoint.X, endPoint.Y, buttonSize.Width, buttonSize.Height);
                }

                button.Visible = button.Bounds.Y + button.Bounds.Height >= 0 && button.Bounds.Y <= ClientSize.Height;
            }
            ResumeLayout();
            UpdateButtonVisibility();
        }

        private void UpdateButtonVisibility()
        {
            bool shouldBeVisible = mouseOverControl || IsMouseOverAnyButton();
            if (buttonsVisible != shouldBeVisible)
            {
                buttonsVisible = shouldBeVisible;
                Invalidate();
            }
        }

        private bool IsMouseOverAnyButton()
        {
            return buttons.Any(b => b.Visible && b.Bounds.Contains(mousePosition));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && scrollTimer != null)
            {
                scrollTimer.Dispose();
                scrollTimer = null;
            }
            base.Dispose(disposing);
        }

        private class ButtonInfo
        {
            public int StartIndex { get; set; }
            public int Length { get; set; }
            public string Text { get; set; }
            public Rectangle Bounds { get; set; }
            public bool Pressed { get; set; }
            public bool Visible { get; set; }
            public EventHandler OnClick { get; set; }
        }
    }
}