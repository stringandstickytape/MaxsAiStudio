using System.Windows.Forms;

namespace FormsToAiToolPrototype
{
    public partial class Form1 : Form
    {
        private TextBox promptTextBox;
        private Button testButton;
        private TextBox responseTextBox;
        private AiConversationStarter conversationStarter;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            SetupAiConversation();
        }

        private void InitializeCustomComponents()
        {
            // Initialize prompt TextBox
            promptTextBox = new TextBox
            {
                Location = new Point(12, 12),
                Multiline = true,
                Size = new Size(460, 100),
                ScrollBars = ScrollBars.Vertical
            };

            // Initialize test Button
            testButton = new Button
            {
                Location = new Point(12, 118),
                Size = new Size(460, 30),
                Text = "Run Test Completion"
            };
            testButton.Click += TestButton_Click;

            // Initialize response TextBox
            responseTextBox = new TextBox
            {
                Location = new Point(12, 154),
                Multiline = true,
                Size = new Size(460, 200),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };

            // Add controls to form
            Controls.AddRange(new Control[] { promptTextBox, testButton, responseTextBox });

            // Set form size
            ClientSize = new Size(484, 366);
        }

        private void SetupAiConversation()
        {
            conversationStarter = new AiConversationStarter(this);
            conversationStarter.ResponseReceived += ConversationStarter_ResponseReceived;
        }

        private async void TestButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(promptTextBox.Text))
            {
                MessageBox.Show("Please enter a prompt first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            testButton.Enabled = false;
            responseTextBox.Text = "Waiting for response...";

            try
            {
                await conversationStarter.StartConversationAsync(promptTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                responseTextBox.Text = string.Empty;
            }
            finally
            {
                testButton.Enabled = true;
            }
        }

        private void ConversationStarter_ResponseReceived(object sender, string response)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => responseTextBox.Text = response));
            }
            else
            {
                responseTextBox.Text = response;
            }
        }
    }
}