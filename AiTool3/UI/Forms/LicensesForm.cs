namespace AiTool3.UI
{
    public partial class LicensesForm : Form
    {
        public LicensesForm(string licenses)
        {
            InitializeComponent();
            tbLicenses.Text = licenses;

            // deselect all text
            tbLicenses.SelectionStart = 0;
            tbLicenses.SelectionLength = 0;
        }
    }
}
