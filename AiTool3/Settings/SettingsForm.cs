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
        public SettingsSet NewSettings;
        public SettingsForm(SettingsSet settings)
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
                if (displayNameAttr == null) continue;
                // ... create a new checkbox control
                var cb = new CheckBox
                {
                    Text = displayNameAttr.DisplayName,
                    Checked = (bool)prop.GetValue(settings),
                    AutoSize = true,
                    Location = new Point(0, ypos)
                };

                cb.Click += (s, e) =>
                {
                    prop.SetValue(NewSettings, cb.Checked);
                };

                // add the control to panelToggles
                panelToggles.Controls.Add(cb);



                // increment ypos
                ypos += 30;

            }

            // for every public string property on settings...
            foreach (var prop in settings.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
                var displayNameAttr = prop.GetCustomAttribute<MyDisplayNameAttrAttribute>();

                var isPathAttr = prop.GetCustomAttribute<IsPathAttribute>();
                var isFileAttr = prop.GetCustomAttribute<IsFileAttribute>();
                var takesBrowserDialog = isPathAttr != null || isFileAttr != null;

                if (displayNameAttr == null) continue;
                // create a new textbox control
                var tb = new TextBox
                {
                    Text = (string)prop.GetValue(settings),
                    Location = new Point(0, ypos),
                    Width = 600
                };

                tb.TextChanged += (s, e) =>
                {
                    prop.SetValue(NewSettings, tb.Text);
                };

                // add to panel
                panelToggles.Controls.Add(tb);

                // add matching label to the right
                var lbl = new Label
                {
                    Text = prop.Name,
                    AutoSize = true,
                    Location = new Point(tb.Width + 5 + (takesBrowserDialog != null ? 60 : 0), ypos)
                };
                panelToggles.Controls.Add(lbl);

                // does the prop have an IsPathAttribute?
                
                if (takesBrowserDialog != null)
                {
                    var btn = new Button
                    {
                        Text = "...",
                        Location = new Point(tb.Width +10, ypos),
                        Width = 50,
                        Height = 30
                    };

                    btn.Click += (s, e) =>
                    {
                        if(isPathAttr != null)
                        {
                            var dialog = new FolderBrowserDialog();
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                tb.Text = dialog.SelectedPath;
                            }
                        }
                        else if(isFileAttr != null)
                        {
                            var ext = isFileAttr.Extension;

                            var dialog = new OpenFileDialog();
                            dialog.Filter = $"{ext} files (*{ext})|*{ext}|All files (*.*)|*.*";
                            if (tb.Text != "")
                            {
                                dialog.InitialDirectory = tb.Text;
                            }
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                tb.Text = dialog.FileName;
                            }
                        }
                    };

                    panelToggles.Controls.Add(btn);
                }


                ypos += 30;
            }

            // for every public int property on settings...
            foreach (var prop in settings.GetType().GetProperties().Where(p => p.PropertyType == typeof(int)))
            {
                var displayNameAttr = prop.GetCustomAttribute<MyDisplayNameAttrAttribute>();
                if (displayNameAttr == null) continue;


                // create a new numeric up down control
                var nud = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 65535,
                    Location = new Point(0, ypos-1)
                };

                nud.Value = (int)prop.GetValue(settings);

                // click
                nud.ValueChanged += (s, e) =>
                {
                    prop.SetValue(NewSettings, (int)(nud.Value));
                };
                
                // add to panel
                panelToggles.Controls.Add(nud);
                // add matching label to the right

                var lbl = new Label
                {
                    Text = prop.Name,
                    AutoSize = true,
                    Location = new Point(nud.Width + 5, ypos)
                };
                panelToggles.Controls.Add(lbl);


                ypos += 30;
            }



            // for every float - use a textbox and convvert to float
            foreach(var prop in settings.GetType().GetProperties().Where(p => p.PropertyType == typeof(float)))
            {
                var displayNameAttr = prop.GetCustomAttribute<MyDisplayNameAttrAttribute>();
                if (displayNameAttr == null) continue;
                // create a new textbox control
                var tb = new TextBox
                {
                    Text = (string)(prop.GetValue(settings).ToString()),
                    Location = new Point(0, ypos),
                    Width = 600
                };

                tb.TextChanged += (s, e) =>
                {
                    prop.SetValue(NewSettings, float.Parse(tb.Text));
                };

                // add to panel
                panelToggles.Controls.Add(tb);

                // add matching label to the right
                var lbl = new Label
                {
                    Text = prop.Name,
                    AutoSize = true,
                    Location = new Point(tb.Width + 5, ypos)
                };
                panelToggles.Controls.Add(lbl);

                ypos += 30;
            }


        }

        private SettingsSet CloneSettings(SettingsSet settings)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(settings);
            return System.Text.Json.JsonSerializer.Deserialize<SettingsSet>(json);
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

        private void CreateDgvRows(SettingsSet settings)
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
