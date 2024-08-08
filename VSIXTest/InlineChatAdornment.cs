using System;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace VSIXTest
{
    internal sealed class InlineChatAdornment
    {
        private readonly IAdornmentLayer layer;
        private readonly IWpfTextView view;
        private ChatControl chatControl;

        public InlineChatAdornment(IWpfTextView view)
        {
            this.view = view;
            this.layer = view.GetAdornmentLayer("InlineChatAdornment");

            this.view.LayoutChanged += OnLayoutChanged;

            // Create and add the chat control
            this.chatControl = new ChatControl();
            Canvas.SetLeft(this.chatControl, this.view.ViewportLeft);
            Canvas.SetTop(this.chatControl, this.view.ViewportTop);
            this.layer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, this.chatControl, null);
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // Update the position of the chat control when the layout changes
            Canvas.SetLeft(this.chatControl, this.view.ViewportLeft);
            Canvas.SetTop(this.chatControl, this.view.ViewportTop);
        }
    }
}