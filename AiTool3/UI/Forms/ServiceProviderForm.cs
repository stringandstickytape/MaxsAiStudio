using AiTool3.AiServices;
using AiTool3.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.UI.Forms
{
    public partial class ServiceProviderForm : Form
    {
        private bool isInitializing = true;
        public List<ServiceProvider> ServiceProviders;
        private List<string> _aiServiceNames; // Add this field
        public ServiceProviderForm(List<ServiceProvider> serviceProviders)
        {
            isInitializing = true;
            InitializeComponent();
            _aiServiceNames = SettingsForm.GetAiServiceNames(); // Add this line
            // Clone the list to work with a copy, so that changes can be cancelled later
            ServiceProviders = CloneServiceProviders(serviceProviders);

            InitializeDgvServiceProviders();
            CreateDgvColumns();
            CreateDgvRows(serviceProviders);
        }

        private List<ServiceProvider> CloneServiceProviders(List<ServiceProvider> providers)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(providers);
            return System.Text.Json.JsonSerializer.Deserialize<List<ServiceProvider>>(json);
        }

        private void InitializeDgvServiceProviders()
        {
            dgvServiceProviders.AllowUserToAddRows = true;
            dgvServiceProviders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvServiceProviders.EditMode = DataGridViewEditMode.EditOnEnter; 
            dgvServiceProviders.CellClick += DgvServiceProviders_CellClick;
            dgvServiceProviders.CellValueChanged += DgvServiceProviders_CellValueChanged;
            dgvServiceProviders.DefaultValuesNeeded += DgvServiceProviders_DefaultValuesNeeded;
            dgvServiceProviders.RowValidating += DgvServiceProviders_RowValidating;
            dgvServiceProviders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvServiceProviders.MultiSelect = false;
            dgvServiceProviders.RowHeadersVisible = true;
            dgvServiceProviders.RowHeadersWidth = 60;
            dgvServiceProviders.RowHeadersDefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
            dgvServiceProviders.EnableHeadersVisualStyles = false;
            dgvServiceProviders.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvServiceProviders.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        }

        private void CreateDgvColumns()
        {
            var columns = new[]
            {
                new { Name = "FriendlyName", HeaderText = "Friendly Name", ReadOnly = false },
                new { Name = "ServiceName", HeaderText = "API Protocol", ReadOnly = false },
                new { Name = "Url", HeaderText = "API URL", ReadOnly = false },
                new { Name = "ApiKey", HeaderText = "API Key", ReadOnly = false }
            };

            foreach (var col in columns)
            {
                if (col.Name == "ServiceName") // Add this condition
                {
                    var comboBoxColumn = new DataGridViewComboBoxColumn
                    {
                        Name = col.Name,
                        HeaderText = col.HeaderText,
                        DataPropertyName = col.Name,
                        ReadOnly = col.ReadOnly,
                        DataSource = _aiServiceNames // Use the AI service names
                    };
                    dgvServiceProviders.Columns.Add(comboBoxColumn);
                    continue;
                }

                var newCol = new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.HeaderText,
                    DataPropertyName = col.Name,
                    ReadOnly = col.ReadOnly,
                };

                if (col.Name == "Url")
                    newCol.Width = 300;
                else if (col.Name == "FriendlyName")
                    newCol.Width = 200;

                dgvServiceProviders.Columns.Add(newCol);
            }

            dgvServiceProviders.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "DeleteButton",
                HeaderText = "Delete",
                Text = "Delete",
                UseColumnTextForButtonValue = true
            });
        }

        private void CreateDgvRows(List<ServiceProvider> providers)
        {
            dgvServiceProviders.Rows.Clear();

            // Sort the providers by FriendlyName
            var sortedProviders = providers.OrderBy(p => p.FriendlyName).ToList();

            foreach (var provider in sortedProviders)
            {
                var index = dgvServiceProviders.Rows.Add(
                    provider.FriendlyName,
                    provider.ServiceName,
                    provider.Url,
                    provider.ApiKey);

                dgvServiceProviders.Rows[index].Cells["DeleteButton"].Value = "Delete";
                dgvServiceProviders.Rows[index].Tag = provider; 
            }
        }
        private void DgvServiceProviders_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            // Set default values for new rows
            e.Row.Cells["FriendlyName"].Value = "New Provider";
            e.Row.Cells["ServiceName"].Value = "";
            e.Row.Cells["Url"].Value = "https://api.example.com";
            e.Row.Cells["ApiKey"].Value = "";
        }

        private void DgvServiceProviders_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dgvServiceProviders.Rows[e.RowIndex];
            if (row.IsNewRow) return; // Skip validation for the new row template

            var friendlyName = row.Cells["FriendlyName"].Value?.ToString();
            var serviceName = row.Cells["ServiceName"].Value?.ToString();
            var url = row.Cells["Url"].Value?.ToString();
            var apiKey = row.Cells["ApiKey"].Value?.ToString();

            // Validate required fields
            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                MessageBox.Show("Friendly Name is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            // Check for duplicate friendly names (excluding the current row)
            if (ServiceProviders.Any(p => p.FriendlyName.Equals(friendlyName, StringComparison.OrdinalIgnoreCase)
                && !p.Url.Equals(url, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A provider with this name already exists.", "Duplicate Name",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            // If this is a newly added row (not the new row template)
            if (row.Tag == null)
            {
                var newProvider = new ServiceProvider
                {
                    FriendlyName = friendlyName,
                    ServiceName = serviceName,
                    Url = url,
                    ApiKey = apiKey
                };
                ServiceProviders.Add(newProvider);
                row.Tag = newProvider; // Mark the row as processed
            }
        }

        private void DgvServiceProviders_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var row = dgvServiceProviders.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            var friendlyName = row.Cells["FriendlyName"].Value?.ToString();
            var serviceName = row.Cells["ServiceName"].Value?.ToString();
            var url = row.Cells["Url"].Value?.ToString();
            var apiKey = row.Cells["ApiKey"].Value?.ToString();

            // Update existing provider
            var existingProvider = ServiceProviders.FirstOrDefault(p =>
                p.FriendlyName.Equals(row.Cells["FriendlyName"].Value?.ToString(), StringComparison.OrdinalIgnoreCase) ||
                p.Url.Equals(row.Cells["Url"].Value?.ToString(), StringComparison.OrdinalIgnoreCase));

            if (existingProvider != null)
            {
                existingProvider.FriendlyName = friendlyName ?? existingProvider.FriendlyName;
                existingProvider.ServiceName = serviceName ?? existingProvider.ServiceName;
                existingProvider.Url = url ?? existingProvider.Url;
                existingProvider.ApiKey = apiKey ?? existingProvider.ApiKey;
            }
        }

        private void DgvServiceProviders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Handle Delete button click
            if (e.ColumnIndex == dgvServiceProviders.Columns["DeleteButton"].Index)
            {
                if (MessageBox.Show("Are you sure you want to delete this provider?",
                    "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var row = dgvServiceProviders.Rows[e.RowIndex];
                    var friendlyName = row.Cells["FriendlyName"].Value?.ToString();

                    if (string.IsNullOrEmpty(friendlyName)) return;
                    var provider = ServiceProviders.FirstOrDefault(p =>
                        p.FriendlyName.Equals(friendlyName, StringComparison.OrdinalIgnoreCase));

                    if (provider != null)
                    {
                        ServiceProviders.Remove(provider);
                        dgvServiceProviders.Rows.RemoveAt(e.RowIndex);
                    }
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
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