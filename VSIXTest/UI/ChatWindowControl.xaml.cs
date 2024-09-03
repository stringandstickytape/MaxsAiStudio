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
            
            
        }
    }
}