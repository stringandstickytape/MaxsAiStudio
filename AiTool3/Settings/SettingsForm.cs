using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.Settings
{
    public partial class SettingsForm : Form
    {
        public SettingsManager NewSettings;
        public SettingsForm(SettingsManager settings)
        {
            InitializeComponent();

            // convert settings to json
            var json = System.Text.Json.JsonSerializer.Serialize(settings);

            // deserialise to clone
            var clone = System.Text.Json.JsonSerializer.Deserialize<SettingsManager>(json);


            NewSettings = clone;

            // prevent dgv from creating new rows
            dgvModels.AllowUserToAddRows = false;

            // add dgv column for model name
            var modelNameColumn = new DataGridViewTextBoxColumn
            {
                Name = "ModelName",
                HeaderText = "Model Name",
                DataPropertyName = "ModelName",
                ReadOnly = true
            };
            dgvModels.Columns.Add(modelNameColumn);

            // add dgv column for model url
            var modelUrlColumn = new DataGridViewTextBoxColumn
            {
                Name = "ModelUrl",
                HeaderText = "Model Url",
                DataPropertyName = "ModelUrl",
                ReadOnly = true
            };
            dgvModels.Columns.Add(modelUrlColumn);

            // add dgv column for model key
            var modelKeyColumn = new DataGridViewTextBoxColumn
            {
                Name = "ModelKey",
                HeaderText = "Model Key",
                DataPropertyName = "ModelKey",
            };
            dgvModels.Columns.Add(modelKeyColumn);

            // fit all the columns automatically
            dgvModels.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // add handler to capture when key is changed
            dgvModels.CellValueChanged += DgvModels_CellValueChanged;

            CreateDgvRows(settings);

        }

        private void CreateDgvRows(SettingsManager settings)
        {
            dgvModels.Rows.Clear();
            // for each model in the settings, add a row to the dgv
            foreach (var api in settings.ApiList)
            {
                foreach (var model in api.Models)
                {
                    // populate the dgv with the model data
                    dgvModels.Rows.Add(model.ModelName, model.Url, model.Key);
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

            if (columnName == "ModelKey")
            {
                // get the model from settings
                var models = NewSettings.ApiList.SelectMany(a => a.Models);
                var model = models.Where(x => x.ModelName == modelName.ToString()).First();
                model.Key = (string)newValue;

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
