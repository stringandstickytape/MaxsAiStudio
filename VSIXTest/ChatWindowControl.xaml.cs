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
        private DTE2 _dte;

        public ChatWindowControl()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            InitializeComponent();
            //WebView.Initialise();
            //InitializeWebView();
        }


    }
}