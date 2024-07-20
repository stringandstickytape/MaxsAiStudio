using System;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AiTool3.ApiManagement;
using AiTool3.Providers;
using AiTool3;
using AiTool3.Conversations;

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
        public UsageStatisticsForm(SettingsSet settings)
        {
            List<Api> apis = settings.ApiList;
            List<Model> models = apis.SelectMany(x => x.Models).ToList();

            this.Text = "Model Usage Statistics";
            this.Size = new Size(800, 600);
            this.AutoScroll = true;

            FlowLayoutPanel panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            var sortedModels = models.Select(model => new
            {
                Model = model,
                Manager = new ModelUsageManager(model),
                TotalCost = ((new ModelUsageManager(model).TokensUsed.InputTokens * model.input1MTokenPrice) +
                             (new ModelUsageManager(model).TokensUsed.OutputTokens * model.output1MTokenPrice)) / 1000000
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();

            foreach (var item in sortedModels)
            {
                Color boxColor = CompletionMessage.GetColorForEngine(item.Model.ModelName);

                Panel modelPanel = new Panel
                {
                    Width = 350,
                    Height = 200,
                    Margin = new Padding(10),
                    BackColor = boxColor
                };

                Label label = new Label
                {
                    Text = $"Model: {item.Model.ModelName}\n" +
                           $"Total Tokens In: {item.Manager.TokensUsed.InputTokens}\n" +
                           $"Total Tokens Out: {item.Manager.TokensUsed.OutputTokens}\n" +
                           $"Total Cost In: ${item.Manager.TokensUsed.InputTokens * item.Model.input1MTokenPrice / 1000000:F4}\n" +
                           $"Total Cost Out: ${item.Manager.TokensUsed.OutputTokens * item.Model.output1MTokenPrice / 1000000:F4}\n" +
                           $"Total Cost: ${item.TotalCost:F4}",
                    AutoSize = true,
                    Location = new Point(10, 10),
                    ForeColor = Color.Black,
                };

                modelPanel.Controls.Add(label);
                panel.Controls.Add(modelPanel);
            }

            this.Controls.Add(panel);
        }
    }

    public static void ShowUsageStatistics(SettingsSet settings)
    {
        UsageStatisticsForm form = new UsageStatisticsForm(settings);
        form.Show();
    }
}