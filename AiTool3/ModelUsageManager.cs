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

internal partial class ModelUsageManager
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

    public static void ShowUsageStatistics(SettingsSet settings)
    {
        UsageStatisticsForm form = new UsageStatisticsForm(settings);
        form.Show();
    }
}