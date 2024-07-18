using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Helpers;
using AiTool3.Providers.Embeddings;
using AiTool3.Providers.Embeddings.Fragmenters;
using AiTool3.Settings;
using AiTool3.Snippets;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3
{
    public partial class MaxsAiStudio
    {
        private void InitialiseMenus()
        {
            var fileMenu = MenuHelper.CreateMenu("File");
            var editMenu = MenuHelper.CreateMenu("Edit");

            new List<ToolStripMenuItem> { fileMenu, editMenu }.ForEach(menu => menuBar.Items.Add(menu));

            MenuHelper.CreateMenuItem("Quit", ref fileMenu).Click += (s, e) => Application.Exit();
                        
            MenuHelper.CreateMenuItem("Settings", ref editMenu).Click += async (s, e) =>
            {
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    SettingsSet.Save(CurrentSettings);
                    cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
                    await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
                }
            };

            MenuHelper.CreateMenuItem("Set Embeddings File", ref editMenu).Click += (s, e) => EmbeddingsHelper.HandleSetEmbeddingsFileClick(CurrentSettings);

            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedAssembly("AiTool3.UI.Licenses.txt")).ShowDialog();

            MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, (Model)cbSummaryEngine.SelectedItem!, chatWebView, snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);
            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, templateManager, CurrentSettings, this);
        }


    }
}
