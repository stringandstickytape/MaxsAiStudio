using EnvDTE;
using Microsoft.CSharp.RuntimeBinder;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using System.Windows.Input;
using System;
using System.Runtime.InteropServices;

namespace VSIXTest
{
    public partial class ChatWindowControl : UserControl
    {
        public VsixChat VsixChatInstance { get; private set; }

        public ChatWindowControl(VsixChat vsixChat)
        {
            VsixChatInstance = vsixChat;
            Content = VsixChatInstance;
            //InitializeComponent();
            PreviewKeyDown += ChatWindowControl_PreviewKeyDown;
            
            
        }

        private void ChatWindowControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Home || e.Key == Key.End)
            {
                e.Handled = true;

                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                bool isAltPressed = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

                if (e.Key == Key.Home)
                {
                    VsixChatInstance.SendHomeAsync(isCtrlPressed, isShiftPressed, isAltPressed);
                }
                else if (e.Key == Key.End)
                {
                    VsixChatInstance.SendEndAsync(isCtrlPressed, isShiftPressed, isAltPressed);
                }

            }
        }

    }

}