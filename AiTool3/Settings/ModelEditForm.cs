using AiTool3.DataModels;
using AiTool3.AiServices;
using System.Data;
using System.Reflection;
using SharedClasses.Providers;

namespace AiTool3.Settings
{
    public partial class ModelEditForm : Form
    {
        public Model Model { get; private set; }

        public ModelEditForm(Model model, List<ServiceProvider> serviceProviders)
        {
            InitializeComponent();
            this.Model = model;

            // Initialize ComboBox for Service Providers
            cboServiceProvider.DataSource = serviceProviders;
            cboServiceProvider.DisplayMember = "FriendlyName";
            cboServiceProvider.SelectedItem = ServiceProvider.GetProviderForGuid(serviceProviders, model.ProviderGuid);

            // Initialize other fields
            txtFriendlyName.Text = model.FriendlyName;
            txtModelName.Text = model.ModelName;
            txtInputPrice.Text = model.input1MTokenPrice.ToString("N2");
            txtOutputPrice.Text = model.output1MTokenPrice.ToString("N2");
            txtAdditionalParams.Text = model.AdditionalParams;
            UpdateColorButton(model.Color);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Validate and update model properties
            Model.FriendlyName = txtFriendlyName.Text;
            Model.ModelName = txtModelName.Text;

            Model.ProviderGuid= ((ServiceProvider)cboServiceProvider.SelectedItem).Guid;

            Model.AdditionalParams = txtAdditionalParams.Text;
                 

            if (decimal.TryParse(txtInputPrice.Text, out decimal inputPrice))
            {
                Model.input1MTokenPrice = inputPrice;
            }
            if (decimal.TryParse(txtOutputPrice.Text, out decimal outputPrice))
            {
                Model.output1MTokenPrice = outputPrice;
            }
            Model.Color = btnColorPicker.BackColor;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void UpdateColorButton(Color color)
        {
            btnColorPicker.BackColor = color;
            btnColorPicker.Text = ColorToHex(color);
        }

        private void btnColorPicker_Click(object sender, EventArgs e)
        {
            colorDialog.Color = btnColorPicker.BackColor;
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                UpdateColorButton(colorDialog.Color);
            }
        }
    }
}