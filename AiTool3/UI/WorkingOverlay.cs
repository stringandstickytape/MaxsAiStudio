using System;
using System.Windows.Forms;
using System.Drawing;

namespace AiTool3.UI
{
    public class WorkingOverlay : Control
    {
        private Control _innerControl;
        private bool _isWorking = false;

        public WorkingOverlay()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.FromArgb(128, Color.White);
        }

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                if (_isWorking != value)
                {
                    _isWorking = value;
                    UpdateInnerControl();
                    Invalidate();
                }
            }
        }

        private void UpdateInnerControl()
        {
            if (_isWorking)
            {
                if (_innerControl == null)
                {
                    _innerControl = new ProgressBar();
                    ((ProgressBar)_innerControl).Style = ProgressBarStyle.Marquee;
                    Controls.Add(_innerControl);
                }
                ResizeInnerControl();
            }
            else
            {
                if (_innerControl != null)
                {
                    Controls.Remove(_innerControl);
                    _innerControl.Dispose();
                    _innerControl = null;
                }
            }
        }

        private void ResizeInnerControl()
        {
            if (_innerControl != null)
            {
                _innerControl.Width = Width / 2;
                _innerControl.Height = Height / 2;
                _innerControl.Left = (Width - _innerControl.Width) / 2;
                _innerControl.Top = (Height - _innerControl.Height) / 2;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ResizeInnerControl();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_isWorking)
            {
                using (var brush = new SolidBrush(ForeColor))
                {
                    var stringFormat = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    e.Graphics.DrawString("Working...", Font, brush, ClientRectangle, stringFormat);
                }
            }
        }
    }
}