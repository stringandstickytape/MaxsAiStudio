using SharedClasses.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.UI
{
    public class MessagePromptEditorForm : Form
    {
        private List<MessagePrompt> messagePrompts;
        private ListBox listBox;
        private TextBox categoryTextBox, buttonLabelTextBox, messageTypeTextBox, toolTextBox, promptTextBox;
        private Button addButton, removeButton, saveButton;

        public MessagePromptEditorForm(MessagePrompt[] initialPrompts)
        {
            messagePrompts = new List<MessagePrompt>(initialPrompts);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.MinimumSize = new Size(800, 800);
            //this.Size = new Size(800, 800);
            this.Text = "Message Prompt Editor";

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            this.Controls.Add(mainLayout);

            // Left panel with ListBox
            Panel leftPanel = new Panel { Dock = DockStyle.Fill };
            listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            leftPanel.Controls.Add(listBox);
            mainLayout.Controls.Add(leftPanel, 0, 0);

            // Right panel with input fields and buttons
            TableLayoutPanel rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                Padding = new Padding(10, 0, 0, 0)
            };
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12)); // Category
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12)); // Button Label 
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12)); // Message Type 
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12)); // Tool 
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12));// Prompt 
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 5));  // Buttons 
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); 

            mainLayout.Controls.Add(rightPanel, 1, 0);

            categoryTextBox = CreateTextBox("Category", rightPanel, 0);
            buttonLabelTextBox = CreateTextBox("Button Label", rightPanel, 1);
            messageTypeTextBox = CreateTextBox("Message Type", rightPanel, 2);
            toolTextBox = CreateTextBox("Name of Tool to use (if any)", rightPanel, 3);
            promptTextBox = CreateTextBox("Prompt (leave blank to use user prompt)", rightPanel, 4, true);

            // Button panel
            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };
            rightPanel.Controls.Add(buttonPanel, 0, 6);

            addButton = CreateButton("Add", AddButton_Click);
            removeButton = CreateButton("Remove", RemoveButton_Click);
            saveButton = CreateButton("Save", SaveButton_Click);
            buttonPanel.Controls.AddRange(new Control[] { addButton, removeButton, saveButton });

            RefreshListBox();

        }

        private TextBox CreateTextBox(string label, TableLayoutPanel parent, int row, bool large = false)
        {
            Panel container = new Panel { Dock = DockStyle.Fill };
            Label labelControl = new Label { Text = label, AutoSize = true, Dock = DockStyle.Top };
            TextBox textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true
            };
            container.Controls.Add(textBox);
            container.Controls.Add(labelControl);
            parent.Controls.Add(container, 0, row);
            return textBox;

        }

        private Button CreateButton(string text, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Text = text,
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(10, 5, 10, 5),
                MinimumSize = new Size(100, 30)
            };
            button.Click += clickHandler;
            return button;

        }

        private void RefreshListBox()
        {
            listBox.Items.Clear();
            listBox.Items.AddRange(messagePrompts.Select(p => p.ButtonLabel).ToArray());
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox.SelectedIndex != -1)
            {
                var selectedPrompt = messagePrompts[listBox.SelectedIndex];
                categoryTextBox.Text = selectedPrompt.Category;
                buttonLabelTextBox.Text = selectedPrompt.ButtonLabel;
                messageTypeTextBox.Text = selectedPrompt.MessageType;
                toolTextBox.Text = selectedPrompt.Tool;
                promptTextBox.Text = selectedPrompt.Prompt;
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var newPrompt = new MessagePrompt
            {
                Category = categoryTextBox.Text,
                ButtonLabel = buttonLabelTextBox.Text,
                MessageType = messageTypeTextBox.Text,
                Tool = toolTextBox.Text,
                Prompt = promptTextBox.Text
            };
            messagePrompts.Add(newPrompt);
            RefreshListBox();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (listBox.SelectedIndex != -1)
            {
                messagePrompts.RemoveAt(listBox.SelectedIndex);
                RefreshListBox();
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (listBox.SelectedIndex != -1)
            {
                var selectedPrompt = messagePrompts[listBox.SelectedIndex];
                selectedPrompt.Category = categoryTextBox.Text;
                selectedPrompt.ButtonLabel = buttonLabelTextBox.Text;
                selectedPrompt.MessageType = messageTypeTextBox.Text;
                selectedPrompt.Tool = toolTextBox.Text;
                selectedPrompt.Prompt = promptTextBox.Text;
                RefreshListBox();
            }
        }

        public MessagePrompt[] GetUpdatedPrompts()
        {
            return messagePrompts.ToArray();
        }
    }
}
