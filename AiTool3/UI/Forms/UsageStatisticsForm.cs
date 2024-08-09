using AiTool3;
using AiTool3.ApiManagement;
using AiTool3.Conversations;

internal partial class ModelUsageManager
{
    public class UsageStatisticsForm : Form
    {
        private SettingsSet settings;

        public UsageStatisticsForm(SettingsSet settings)
        {
            this.settings = settings;
            List<Model> models = settings.ModelList;

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
                             (new ModelUsageManager(model).TokensUsed.OutputTokens * model.output1MTokenPrice)) / 1000000,
                StartDate = File.Exists(GetTokenUsageFilenameFromModel(model))
                    ? new FileInfo(GetTokenUsageFilenameFromModel(model)).CreationTime
                    : DateTime.MinValue
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();

            foreach (var item in sortedModels)
            {
                Color boxColor = CompletionMessage.GetColorForEngine(item.Model.ModelName);

                Panel modelPanel = new Panel
                {
                    Width = 350,
                    Height = 230,
                    Margin = new Padding(10),
                    BackColor = boxColor
                };

                Label modelLabel = CreateCenteredLabel($"Model: {item.Model.ModelName}", new Font(this.Font, FontStyle.Bold), 10, modelPanel.Width);
                Label tokensInLabel = CreateCenteredLabel($"Total Tokens In: {item.Manager.TokensUsed.InputTokens}", this.Font, 40, modelPanel.Width);
                Label tokensOutLabel = CreateCenteredLabel($"Total Tokens Out: {item.Manager.TokensUsed.OutputTokens}", this.Font, 60, modelPanel.Width);
                Label costInLabel = CreateCenteredLabel($"Total Cost In: ${item.Manager.TokensUsed.InputTokens * item.Model.input1MTokenPrice / 1000000:F4}", this.Font, 80, modelPanel.Width);
                Label costOutLabel = CreateCenteredLabel($"Total Cost Out: ${item.Manager.TokensUsed.OutputTokens * item.Model.output1MTokenPrice / 1000000:F4}", this.Font, 100, modelPanel.Width);
                Label totalCostLabel = CreateCenteredLabel($"${item.TotalCost:F4}", new Font(this.Font.FontFamily, this.Font.Size * 1.5f, FontStyle.Bold), 140, modelPanel.Width);

                Label startDateLabel = new Label
                {
                    Text = $"{(item.StartDate != DateTime.MinValue ? item.StartDate.ToString("yyyy-MM-dd HH:mm:ss") : "N/A")}",
                    AutoSize = true,
                    Location = new Point(5, modelPanel.Height - 40),
                    ForeColor = Color.Black,
                };

                Button resetButton = new Button
                {
                    Text = "Reset",
                    Location = new Point(modelPanel.Width - 80, modelPanel.Height - 50),
                    Size = new Size(70, 40),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.White,
                    FlatAppearance =
                    {
                        BorderColor = Color.White,
                        BorderSize = 1
                    }
                };

                resetButton.Click += (sender, e) => ResetModel(item.Model, panel);

                modelPanel.Controls.Add(modelLabel);
                modelPanel.Controls.Add(tokensInLabel);
                modelPanel.Controls.Add(tokensOutLabel);
                modelPanel.Controls.Add(costInLabel);
                modelPanel.Controls.Add(costOutLabel);
                modelPanel.Controls.Add(totalCostLabel);
                modelPanel.Controls.Add(startDateLabel);
                modelPanel.Controls.Add(resetButton);
                panel.Controls.Add(modelPanel);
            }

            this.Controls.Add(panel);
        }

        private Label CreateCenteredLabel(string text, Font font, int yPosition, int panelWidth)
        {
            Label label = new Label
            {
                Text = text,
                Font = font,
                AutoSize = true,
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = panelWidth
            };
            label.Location = new Point(0, yPosition);
            return label;
        }

        private void ResetModel(Model model, FlowLayoutPanel panel)
        {
            DialogResult result = MessageBox.Show($"Are you sure you want to reset the usage statistics for {model.ModelName}?",
                                                  "Confirm Reset",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string filePath = GetTokenUsageFilenameFromModel(model);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Refresh the form
                panel.Controls.Clear();
                InitializeComponent();
            }
        }

        private void InitializeComponent()
        {
            // Re-initialize the form with updated data
            UsageStatisticsForm newForm = new UsageStatisticsForm(this.settings);
            this.Controls.Clear();
            foreach (Control control in newForm.Controls)
            {
                this.Controls.Add(control);
            }
        }
    }

    public class ModelCostPerOutputTokenForm : Form
    {
        private SettingsSet settings;

        public ModelCostPerOutputTokenForm(SettingsSet settings)
        {
            this.settings = settings;
            List<Model> models = settings.ModelList;

            this.Text = "Model Cost Per Output MToken";
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
                CostPerOutputToken = model.output1MTokenPrice
            })
            .OrderByDescending(x => x.CostPerOutputToken)
            .ToList();

            foreach (var item in sortedModels)
            {
                Color boxColor = CompletionMessage.GetColorForEngine(item.Model.ModelName);

                Panel modelPanel = new Panel
                {
                    Width = 350,
                    Height = 150,
                    Margin = new Padding(10),
                    BackColor = boxColor
                };

                Label modelLabel = CreateCenteredLabel($"Model: {item.Model.ModelName}", new Font(this.Font, FontStyle.Bold), 10, modelPanel.Width);
                Label costOutLabel = CreateCenteredLabel($"Cost Per Output MToken: ${item.CostPerOutputToken:F6}", this.Font, 40, modelPanel.Width);
                Label outputTokensLabel = CreateCenteredLabel($"Output Tokens: {item.Manager.TokensUsed.OutputTokens}", this.Font, 60, modelPanel.Width);

                modelPanel.Controls.Add(modelLabel);
                modelPanel.Controls.Add(costOutLabel);
                modelPanel.Controls.Add(outputTokensLabel);
                panel.Controls.Add(modelPanel);
            }

            this.Controls.Add(panel);
        }

        private Label CreateCenteredLabel(string text, Font font, int yPosition, int panelWidth)
        {
            Label label = new Label
            {
                Text = text,
                Font = font,
                AutoSize = true,
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter,
                Width = panelWidth
            };
            label.Location = new Point(0, yPosition);
            return label;
        }
    }
}