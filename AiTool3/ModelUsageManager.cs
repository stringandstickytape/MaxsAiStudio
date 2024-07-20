using System;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using AiTool3.ApiManagement;
using AiTool3.Providers;

internal class ModelUsageManager
{
    private Model model;
    public TokenUsage TokensUsed { get; set; }

    public ModelUsageManager(Model model)
    {
        this.model = model;

        if (File.Exists($"TokenUsage-{model.ToString()}.json"))
        {
            var json = File.ReadAllText($"TokenUsage-{model.ToString()}.json");
            TokensUsed = JsonConvert.DeserializeObject<TokenUsage>(json);
        }
        else
        {
            TokensUsed = new TokenUsage("", "");
        }
    }

    internal void AddTokensAndSave(TokenUsage tokenUsage)
    {
        TokensUsed.InputTokens += tokenUsage.InputTokens;
        TokensUsed.OutputTokens += tokenUsage.OutputTokens;

        var json = JsonConvert.SerializeObject(TokensUsed);
        File.WriteAllText($"TokenUsage-{model.ToString()}.json", json);
    }

    public class UsageStatisticsForm : Form
    {
        public UsageStatisticsForm(List<Model> models)
        {
            this.Text = "Model Usage Statistics";
            this.Size = new Size(400, 300);

            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            panel.ColumnCount = 1;
            panel.RowCount = models.Count;

            for (int i = 0; i < models.Count; i++)
            {
                Model model = models[i];
                ModelUsageManager manager = new ModelUsageManager(model);

                Label label = new Label
                {
                    Text = $"Model: {model.ModelName}\n" +
                           $"Total Tokens In: {manager.TokensUsed.InputTokens}\n" +
                           $"Total Tokens Out: {manager.TokensUsed.OutputTokens}\n" +
                           $"Total Cost In: ${manager.TokensUsed.InputTokens * 0.00001:F4}\n" +
                           $"Total Cost Out: ${manager.TokensUsed.OutputTokens * 0.00002:F4}\n" +
                           $"Total Cost: ${(manager.TokensUsed.InputTokens * 0.00001) + (manager.TokensUsed.OutputTokens * 0.00002):F4}",
                    AutoSize = true,
                    Margin = new Padding(10)
                };

                panel.Controls.Add(label, 0, i);
            }

            this.Controls.Add(panel);
        }
    }

    public static void ShowUsageStatistics(List<Model> models)
    {
        UsageStatisticsForm form = new UsageStatisticsForm(models);
        form.ShowDialog();
    }
}