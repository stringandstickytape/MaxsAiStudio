using System;
using System.Windows.Forms;
using System.Linq;
using AiTool3.UI;

namespace AiTool3.ExtensionMethods
{
    public static class FormExtensions
    {
        private const string OverlayTag = "FormWorkingOverlay";

        public static void ShowWorking(this Form form, string message, bool softwareToysMode)
        {
            if (form.Controls.OfType<WorkingOverlay>().Any(c => c.Tag as string == OverlayTag))
                return;

            var overlay = new WorkingOverlay(message, softwareToysMode)
            {
                Dock = DockStyle.Fill,
                Tag = OverlayTag,
                IsWorking = true,
                IsOnForm = true,  // Set this to true for forms
            };

            form.Controls.Add(overlay);
            overlay.BringToFront();

            // Store the form's current state
            overlay.Tag = new FormState
            {
                BackColor = form.BackColor,
                Enabled = form.Enabled,
                Cursor = form.Cursor
            };
        }

        public static void HideWorking(this Form form)
        {
            var overlay = form.Controls.OfType<WorkingOverlay>().FirstOrDefault();
            if (overlay != null)
            {
                form.Controls.Remove(overlay);
                overlay.Dispose();

                // Restore the form's previous state
                if (overlay.Tag is FormState state)
                {
                    form.BackColor = state.BackColor;
                    form.Enabled = state.Enabled;
                    form.Cursor = state.Cursor;
                }
            }
        }

        public static bool IsWorking(this Form form)
        {
            return form.Controls.OfType<WorkingOverlay>().Any(c => c.Tag as string == OverlayTag);
        }

        // Helper class to store form state
        private class FormState
        {
            public Color BackColor { get; set; }
            public bool Enabled { get; set; }
            public Cursor Cursor { get; set; }
        }

        // You can include the InvokeIfNeeded methods here as well if needed
        public static void InvokeIfNeeded(this Form form, Action action)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public static T InvokeIfNeeded<T>(this Form form, Func<T> func)
        {
            if (form.InvokeRequired)
            {
                return (T)form.Invoke(func);
            }
            else
            {
                return func();
            }
        }
    }
}