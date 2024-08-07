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

        public static void ShowWorking(this Control control, string message, bool softwareToysMode)
        {
            if (control.Controls.OfType<WorkingOverlay>().Any())
                return;

            var overlay = new WorkingOverlay(message, softwareToysMode)
            {
                Dock = DockStyle.Fill,
                Tag = OverlayTag,
                IsWorking = true,
                IsOnForm = false,  // Set this to false for controls
            };
            overlay.BackColor = Color.FromArgb(0, control.BackColor);  // Semi-transparent background
            control.Controls.Add(overlay);

            overlay.BringToFront();
            control.Enabled = false;
        }

        public static void HideWorking(this Control control)
        {
            var overlay = control.Controls.OfType<WorkingOverlay>().FirstOrDefault();
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
