using System;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiTool3.UI;

namespace AiTool3.ExtensionMethods
{


    public static class ControlExtensions
    {
        public static void InvokeIfNeeded(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static T InvokeIfNeeded<T>(this Control control, Func<T> func)
        {
            if (control.InvokeRequired)
            {
                return (T)control.Invoke(func);
            }
            else
            {
                return func();
            }
        }

            private const string OverlayTag = "WorkingOverlay";

            public static void ShowWorking(this Control control)
            {
                if (control.Controls.OfType<WorkingOverlay>().Any())
                    return;

                var overlay = new WorkingOverlay
                {
                    Dock = DockStyle.Fill,
                    Tag = OverlayTag,
                    IsWorking = true
                };

                control.Controls.Add(overlay);
                overlay.BringToFront();
                control.Enabled = false;
            }

            public static void HideWorking(this Control control)
            {
                var overlay = control.Controls.OfType<WorkingOverlay>().FirstOrDefault(c => c.Tag as string == OverlayTag);
                if (overlay != null)
                {
                    control.Controls.Remove(overlay);
                    overlay.Dispose();
                }
                control.Enabled = true;
            }

            public static bool IsWorking(this Control control)
            {
                return control.Controls.OfType<WorkingOverlay>().Any(c => c.Tag as string == OverlayTag);
            }

    }
}
