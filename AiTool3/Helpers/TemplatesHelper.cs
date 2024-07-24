using AiTool3.Topics;
using Microsoft.CodeAnalysis;
using System.Data;

namespace AiTool3.Helpers
{
    public static class TemplatesHelper
    {
        private static void AddLabelAndTextBox(TableLayoutPanel panel, string labelText, string textBoxContent, int row, bool Multiline = true)
        {
            var label = new Label
            {
                Text = labelText,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(label, 0, row);

            var textBox = new RichTextBox
            {
                Text = textBoxContent,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5),
                Multiline = Multiline,
                Height = Multiline ? 200 : 30,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            panel.Controls.Add(textBox, 1, row);
        }

        public static void UpdateTemplates(ConversationTemplate template, bool add, string? category, Form form, TopicSet topicSet)
        {
            TableLayoutPanel tableLayoutPanel = CreateTemplatesForm(template, form);
            form.ShowDialog();

            // if ok returned, update the template with the new values...
            if (form.DialogResult == DialogResult.OK)
            {
                template.TemplateName = tableLayoutPanel.Controls[1].Text;
                template.SystemPrompt = tableLayoutPanel.Controls[3].Text;
                template.InitialPrompt = tableLayoutPanel.Controls[5].Text;

                if (add)
                {
                    var categoryTopic = topicSet.Categories.FirstOrDefault(t => t.Name == category);

                    if (categoryTopic == null)
                    {
                        categoryTopic = new Topic(Guid.NewGuid().ToString(), category);
                        topicSet.Categories.Add(categoryTopic);
                    }

                    categoryTopic.Templates.Add(template);
                }

                topicSet.Save();
            }
        }

        private static TableLayoutPanel CreateTemplatesForm(ConversationTemplate template, Form form)
        {
            form.Text = "Edit Template";
            form.Size = new Size(800, 600);
            form.Padding = new Padding(10);

            var tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.RowCount = 4;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));

            // Template Name
            AddLabelAndTextBox(tableLayoutPanel, "Template Name:", template.TemplateName, 0, Multiline: false);

            // System Prompt
            AddLabelAndTextBox(tableLayoutPanel, "System Prompt:", template.SystemPrompt, 1);

            // User Prompt
            AddLabelAndTextBox(tableLayoutPanel, "User Prompt:", template.InitialPrompt, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Dock = DockStyle.Fill;

            var btnCancel = new Button { Text = "Cancel" };
            btnCancel.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };
            btnCancel.AutoSize = true;
            buttonPanel.Controls.Add(btnCancel);

            var btnOk = new Button { Text = "OK" };
            btnOk.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };
            btnOk.AutoSize = true;

            buttonPanel.Controls.Add(btnOk);

            tableLayoutPanel.Controls.Add(buttonPanel, 1, 3);

            form.Controls.Add(tableLayoutPanel);
            return tableLayoutPanel;
        }
    }
}