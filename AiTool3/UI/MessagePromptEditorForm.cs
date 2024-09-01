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
        private TextBox categoryTextBox, buttonLabelTextBox, messageTypeTextBox, promptTextBox;
        private Button addButton, removeButton, saveButton;

        public MessagePromptEditorForm(MessagePrompt[] initialPrompts)
        {
            messagePrompts = new List<MessagePrompt>(initialPrompts);
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(600, 500);
            this.Text = "Message Prompt Editor";

            listBox = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(200, 400),
                SelectionMode = SelectionMode.One
            };
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            this.Controls.Add(listBox);

            categoryTextBox = CreateTextBox("Category", 220, 10);
            buttonLabelTextBox = CreateTextBox("Button Label", 220, 70);
            messageTypeTextBox = CreateTextBox("Message Type", 220, 130);
            promptTextBox = CreateTextBox("Prompt", 220, 190, 150);

            addButton = CreateButton("Add", 220, 380, AddButton_Click);
            removeButton = CreateButton("Remove", 330, 380, RemoveButton_Click);
            saveButton = CreateButton("Save", 440, 380, SaveButton_Click);

            RefreshListBox();
        }

        private TextBox CreateTextBox(string label, int x, int y, int height = 60)
        {
            this.Controls.Add(new Label { Text = label, Location = new Point(x, y), AutoSize = true });
            TextBox textBox = new TextBox
            {
                Location = new Point(x, y + 20),
                Size = new Size(350, height),
                Multiline = true
            };
            this.Controls.Add(textBox);
            return textBox;
        }

        private Button CreateButton(string text, int x, int y, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 30)
            };
            button.Click += clickHandler;
            this.Controls.Add(button);
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
