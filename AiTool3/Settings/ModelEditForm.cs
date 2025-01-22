using AiTool3.DataModels;
using AiTool3.AiServices;
using System.Data;
using System.Reflection;

namespace AiTool3.Settings
{
    public partial class ModelEditForm : Form
    {
        public Model Model { get; private set; }

        public ModelEditForm(Model model, List<string> aiServiceNames)
        {
            InitializeComponent();
            this.Model = model;

            // Initialize ComboBox for AI Services
            cboAiService.DataSource = aiServiceNames;
            cboAiService.SelectedItem = model.ServiceName;

            // Initialize other fields
            txtFriendlyName.Text = model.FriendlyName;
            txtModelName.Text = model.ModelName;
            txtModelUrl.Text = model.Url;
            txtModelKey.Text = model.Key;
            txtInputPrice.Text = model.input1MTokenPrice.ToString("N2");
            txtOutputPrice.Text = model.output1MTokenPrice.ToString("N2");
            txtColor.Text = ColorTranslator.ToHtml(model.Color);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate and update model properties
            Model.FriendlyName = txtFriendlyName.Text;
            Model.ModelName = txtModelName.Text;
            Model.ServiceName = cboAiService.SelectedItem?.ToString() ?? "";
            Model.Url = txtModelUrl.Text;
            Model.Key = txtModelKey.Text;
            if (decimal.TryParse(txtInputPrice.Text, out decimal inputPrice))
            {
                Model.input1MTokenPrice = inputPrice;
            }
            if (decimal.TryParse(txtOutputPrice.Text, out decimal outputPrice))
            {
                Model.output1MTokenPrice = outputPrice;
            }
            Model.Color = ColorTranslator.FromHtml(txtColor.Text);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
