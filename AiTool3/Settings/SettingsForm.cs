using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.Settings
{
    public partial class SettingsForm : Form
    {
        public Settings NewSettings;
        public SettingsForm(Settings settings)
        {
            InitializeComponent();

            NewSettings = CloneSettings(settings);

            InitializeDgvModels();
            CreateDgvColumns();
            CreateDgvRows(settings);

            var ypos = 0;

            // for every public bool property on settings...
            foreach (var prop in settings.GetType().GetProperties().Where(p => p.PropertyType == typeof(bool)))
            {
                var displayNameAttr = prop.GetCustomAttribute<MyDisplayNameAttrAttribute>();

                // ... create a new checkbox control
                var cb = new CheckBox
                {
                    Text = displayNameAttr.DisplayName,
                    Checked = (bool)prop.GetValue(settings),
                    AutoSize = true
                };

                cb.Click += (s, e) =>
                {
                    prop.SetValue(NewSettings, cb.Checked);
                };

                // add the control to panelToggles
                panelToggles.Controls.Add(cb);

                // add a matching label to the right of the checkbox
                var lbl = new Label
                {
                    Text = prop.Name,
                    AutoSize = true,
                    Location = new Point(cb.Width + 10, ypos)
                };


                // increment ypos
                ypos += 30;

            }

        }

        private Settings CloneSettings(Settings settings)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            return System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
        }

        private void InitializeDgvModels()
        {
            dgvModels.AllowUserToAddRows = false;
            dgvModels.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvModels.CellValueChanged += DgvModels_CellValueChanged;
        }

        private void CreateDgvColumns()
        {
            var columns = new[]
            {
        new { Name = "ModelName", HeaderText = "Model Name", ReadOnly = true },
        new { Name = "ModelUrl", HeaderText = "Model Url", ReadOnly = true },
        new { Name = "ModelKey", HeaderText = "Model Key", ReadOnly = false },
        new { Name = "ModelInputPrice", HeaderText = "Input 1MToken Price", ReadOnly = false },
        new { Name = "ModelOutputPrice", HeaderText = "Output 1MToken Price", ReadOnly = false }

            };

            foreach (var col in columns)
            {
                dgvModels.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.HeaderText,
                    DataPropertyName = col.Name,
                    ReadOnly = col.ReadOnly
                });
            }
        }

        private void CreateDgvRows(Settings settings)
        {
            dgvModels.Rows.Clear();
            // for each model in the settings, add a row to the dgv
            foreach (var api in settings.ApiList)
            {
                foreach (var model in api.Models)
                {
                    // populate the dgv with the model data
                    dgvModels.Rows.Add(model.ModelName, model.Url, model.Key, model.input1MTokenPrice, model.output1MTokenPrice);
                }
            }
        }

        private void DgvModels_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // get the name of the column changed
            var columnName = dgvModels.Columns[e.ColumnIndex].Name; // ModelKey

            // get the row changed
            var rowIndex = e.RowIndex;

            // get the row
            var row = dgvModels.Rows[rowIndex];

            // get the first item from the row
            var modelName = row.Cells[0].Value;

            // get the new value
            var newValue = row.Cells[e.ColumnIndex].Value;

            if (e.ColumnIndex > 2)
            {
                // get the model from settings
                var models = NewSettings.ApiList.SelectMany(a => a.Models);
                var model = models.Where(x => x.ModelName == modelName.ToString()).First();
                switch (e.ColumnIndex)
                {
                    case 2:
                        model.Key = (string)newValue;
                        break;
                    case 3:
                        model.input1MTokenPrice = decimal.Parse(newValue.ToString());
                        break;
                    case 4:
                        model.output1MTokenPrice = decimal.Parse(newValue.ToString());
                        break;
                }
            }
        }

        private void btnSettingsCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnSettingsOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
