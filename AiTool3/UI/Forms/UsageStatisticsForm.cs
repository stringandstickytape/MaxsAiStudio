using AiTool3;
using AiTool3.ApiManagement;
using AiTool3.Conversations;

internal partial class ModelUsageManager
{
    public abstract class BaseModelStatsForm : Form
    {
        protected SettingsSet settings;

        protected BaseModelStatsForm(SettingsSet settings, string title, Size size)
        {
            this.settings = settings;
            this.Text = title;
            this.Size = size;
            this.AutoScroll = true;

            var panel = CreateFlowLayoutPanel();
            this.Controls.Add(panel);

            PopulatePanel(panel);
        }

        protected abstract void PopulatePanel(FlowLayoutPanel panel);

        protected FlowLayoutPanel CreateFlowLayoutPanel() =>
            new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = Color.FromArgb(30, 30, 30)
            };

        protected Panel CreateModelPanel(Color boxColor, int width, int height) =>
            new Panel
            {
                Width = width,
                Height = height,
                Margin = new Padding(10),
                BackColor = boxColor
            };

        protected Label CreateCenteredLabel(string text, Font font, int yPosition, int panelWidth)
        {
            var label = new Label
            {
                Text = text,
                Font = font,
                AutoSize = true,
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = panelWidth,
                Location = new Point(0, yPosition)
            };
            return label;
        }
    }

    public class UsageStatisticsForm : BaseModelStatsForm
    {
        public UsageStatisticsForm(SettingsSet settings) : base(settings, "Model Usage Statistics", new Size(800, 600)) { }

        protected override void PopulatePanel(FlowLayoutPanel panel)
        {
            var sortedModels = settings.ModelList
                .Select(model => new
                {
                    Model = model,
                    Manager = new ModelUsageManager(model),
                    TotalCost = CalculateTotalCost(model),
                    StartDate = GetStartDate(model)
                })
                .OrderByDescending(x => x.TotalCost)
                .ToList();

            foreach (var item in sortedModels)
            {
                var modelPanel = CreateModelPanel(CompletionMessage.GetColorForEngine(item.Model.ModelName), 350, 230);
                AddLabelsToPanel(modelPanel, item);
                AddResetButtonToPanel(modelPanel, item.Model, panel);
                panel.Controls.Add(modelPanel);
            }
        }

        private void AddLabelsToPanel(Panel modelPanel, dynamic item)
        {
            var labels = new[]
            {
                CreateCenteredLabel($"Model: {item.Model.ModelName}", new Font(this.Font, FontStyle.Bold), 10, modelPanel.Width),
                CreateCenteredLabel($"Total Tokens In: {item.Manager.TokensUsed.InputTokens}", this.Font, 40, modelPanel.Width),
                CreateCenteredLabel($"Total Tokens Out: {item.Manager.TokensUsed.OutputTokens}", this.Font, 60, modelPanel.Width),
                CreateCenteredLabel($"Total Cost In: ${item.Manager.TokensUsed.InputTokens * item.Model.input1MTokenPrice / 1000000:F4}", this.Font, 80, modelPanel.Width),
                CreateCenteredLabel($"Total Cost Out: ${item.Manager.TokensUsed.OutputTokens * item.Model.output1MTokenPrice / 1000000:F4}", this.Font, 100, modelPanel.Width),
                CreateCenteredLabel($"${item.TotalCost:F4}", new Font(this.Font.FontFamily, this.Font.Size * 1.5f, FontStyle.Bold), 140, modelPanel.Width),
                new Label
                {
                    Text = $"{(item.StartDate != DateTime.MinValue ? item.StartDate.ToString("yyyy-MM-dd HH:mm:ss") : "N/A")}",
                    AutoSize = true,
                    Location = new Point(5, modelPanel.Height - 40),
                    ForeColor = Color.Black,
                }
            };

            modelPanel.Controls.AddRange(labels);
        }

        private void AddResetButtonToPanel(Panel modelPanel, Model model, FlowLayoutPanel panel)
        {
            var resetButton = new Button
            {
                Text = "Reset",
                Location = new Point(modelPanel.Width - 80, modelPanel.Height - 50),
                Size = new Size(70, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                FlatAppearance = { BorderColor = Color.White, BorderSize = 1 }
            };
            resetButton.Click += (sender, e) => ResetModel(model, panel);
            modelPanel.Controls.Add(resetButton);
        }

        private void ResetModel(Model model, FlowLayoutPanel panel)
        {
            if (MessageBox.Show($"Are you sure you want to reset the usage statistics for {model.ModelName}?",
                                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string filePath = GetTokenUsageFilenameFromModel(model);
                if (File.Exists(filePath)) File.Delete(filePath);
                panel.Controls.Clear();
                PopulatePanel(panel);
            }
        }

        private static decimal CalculateTotalCost(Model model)
        {
            var manager = new ModelUsageManager(model);
            return ((manager.TokensUsed.InputTokens * model.input1MTokenPrice) +
                    (manager.TokensUsed.OutputTokens * model.output1MTokenPrice)) / 1000000m;
        }

        private static DateTime GetStartDate(Model model)
        {
            string filePath = GetTokenUsageFilenameFromModel(model);
            return File.Exists(filePath) ? new FileInfo(filePath).CreationTime : DateTime.MinValue;
        }
    }

    public class ModelCostPerOutputTokenForm : BaseModelStatsForm
    {
        public ModelCostPerOutputTokenForm(SettingsSet settings) : base(settings, "Model Cost Per Output MToken", new Size(800, 600)) { }

        protected override void PopulatePanel(FlowLayoutPanel panel)
        {
            var sortedModels = settings.ModelList
                .Select(model => new
                {
                    Model = model,
                    Manager = new ModelUsageManager(model),
                    CostPerOutputToken = model.output1MTokenPrice
                })
                .OrderByDescending(x => x.CostPerOutputToken)
                .ToList();

            foreach (var item in sortedModels)
            {
                var modelPanel = CreateModelPanel(CompletionMessage.GetColorForEngine(item.Model.ModelName), 350, 150);
                var labels = new[]
                {
                    CreateCenteredLabel($"Model: {item.Model.ModelName}", new Font(this.Font, FontStyle.Bold), 10, modelPanel.Width),
                    CreateCenteredLabel($"Cost Per Output MToken: ${item.CostPerOutputToken:F6}", this.Font, 40, modelPanel.Width),
                    CreateCenteredLabel($"Output Tokens: {item.Manager.TokensUsed.OutputTokens}", this.Font, 60, modelPanel.Width)
                };
                modelPanel.Controls.AddRange(labels);
                panel.Controls.Add(modelPanel);
            }
        }
    }
}