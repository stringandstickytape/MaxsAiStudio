namespace AiTool3.UI
{
    public partial class AutoSuggestUserInput : Form
    {
        public AutoSuggestUserInput()
        {
            InitializeComponent();
        }

        private void btnAutoSuggestCancel_Click(object sender, EventArgs e)
        {
            // set cancel result
            DialogResult = DialogResult.Cancel;
            Close();

        }

        private void btnAutoSuggestOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
