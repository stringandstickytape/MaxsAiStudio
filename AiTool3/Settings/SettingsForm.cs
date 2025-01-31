using AiTool3.DataModels;
using AiTool3.AiServices;
using System.Data;
using System.Reflection;
using System.Windows.Forms;
using AiTool3.UI.Forms;
using Newtonsoft.Json;

namespace AiTool3.Settings
{
    public partial class SettingsForm : Form
    {
        private bool isInitializing = true;  // Add this field
        public int yInc = 32;

        public SettingsSet NewSettings;
        public SettingsForm(SettingsSet settings)
        {
            isInitializing = true;

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
                ypos += yInc;
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
                    Text = displayNameAttr.DisplayName,
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
                        Location = new Point(tb.Width + 10, ypos),
                        Width = 50,
                        Height = 30
                    };

                    btn.Click += (s, e) =>
                    {
                        if (isPathAttr != null)
                        {
                            var dialog = new FolderBrowserDialog();
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                tb.Text = dialog.SelectedPath;
                            }
                        }
                        else if (isFileAttr != null)
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

                ypos += yInc;
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
                    Location = new Point(0, ypos - 1)
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

                ypos += yInc;
            }



            // for every float - use a textbox and convvert to float
            foreach (var prop in settings.GetType().GetProperties().Where(p => p.PropertyType == typeof(float)))
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

                ypos += yInc;
            }

            //this.BeginInvoke(new Action(() => dgvModels.ClearSelection()));//

        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            dgvModels.ClearSelection();
        }


        private SettingsSet CloneSettings(SettingsSet settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var retVal = JsonConvert.DeserializeObject<SettingsSet>(json);

            return retVal;
        }

        private void InitializeDgvModels()
        {
            dgvModels.AllowUserToAddRows = true;
            dgvModels.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvModels.CellClick += DgvModels_CellClick;
            dgvModels.SelectionChanged += DgvModels_SelectionChanged;
            dgvModels.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvModels.MultiSelect = false;
            dgvModels.RowHeadersVisible = true;
            dgvModels.RowHeadersWidth = 60;
            dgvModels.RowHeadersDefaultCellStyle.Padding = new Padding(5, 0, 0, 0);
            dgvModels.EnableHeadersVisualStyles = false;
            dgvModels.RowHeadersDefaultCellStyle.SelectionBackColor = dgvModels.RowHeadersDefaultCellStyle.BackColor;
            dgvModels.CellPainting += DgvModels_CellPainting;
            dgvModels.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            dgvModels.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        }

        private void DgvModels_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)  // Row header cell
            {
                e.PaintBackground(e.ClipBounds, true);
                using (SolidBrush b = new SolidBrush(Color.Black))  // or whatever color you want
                {
                    e.Graphics.DrawString("Edit",
                        dgvModels.Font,
                        b,
                        e.CellBounds.X + 5,
                        e.CellBounds.Y + 4);
                }
                e.Handled = true;
            }
        }

        public static List<string> GetAiServiceNames()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AiServiceBase)))
                .Select(t => t.Name)
                .OrderBy(x => x)
                .ToList();
        }

        private void CreateDgvColumns()
        {
            var columns = new[]
            {
                new { Name = "CopyButton", HeaderText = "Copy", ReadOnly = false },
                new { Name = "FriendlyName", HeaderText = "Friendly Name", ReadOnly = false },
                new { Name = "ModelName", HeaderText = "Model Name", ReadOnly = false },
                new { Name = "Guid", HeaderText = "Guid", ReadOnly = false },
            };

            foreach (var col in columns)
            {
                if (col.Name == "AiServ")
                {
                    var comboBoxColumn = new DataGridViewComboBoxColumn
                    {
                        Name = col.Name,
                        HeaderText = col.HeaderText,
                        DataPropertyName = col.Name,
                        ReadOnly = col.ReadOnly,
                        DataSource = GetAiServiceNames()
                    };
                    dgvModels.Columns.Add(comboBoxColumn);
                    continue;
                }

                var newCol = new DataGridViewTextBoxColumn
                {
                    Name = col.Name,
                    HeaderText = col.HeaderText,
                    DataPropertyName = col.Name,
                    ReadOnly = col.ReadOnly,
                };

                switch (newCol.Name)
                {
                    case "FriendlyName":
                    case "Guid":
                    case "ModelName":
                        newCol.Width = 200;
                        break;
                    case "ModelInputPrice":
                    case "ModelOutputPrice":
                        newCol.DefaultCellStyle.Format = "N2";
                        newCol.Width = 100;
                        break;
                }
                dgvModels.Columns.Add(newCol);
            }

            dgvModels.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "CopyButton",
                HeaderText = "Copy",
                Text = "Copy",
                UseColumnTextForButtonValue = true
            });

            dgvModels.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "DeleteButton",
                HeaderText = "Delete",
                Text = "Delete",
                UseColumnTextForButtonValue = true
            });
        }

        private void CreateDgvRows(SettingsSet settings)
        {
            dgvModels.Rows.Clear();

            // Sort the ModelList by ServiceName and then by FriendlyName
            var sortedModelList = settings.ModelList
                .OrderBy(model => ServiceProvider.GetProviderForGuid(settings.ServiceProviders, model.ProviderGuid)?.ServiceName ?? "unknown")
                .ThenBy(model => model.FriendlyName)
                .ToList();

            foreach (var model in sortedModelList)
            {
                var index = dgvModels.Rows.Add(
                    "Copy",
                    model.FriendlyName,
                    model.ModelName,
                    model.Guid,
                    model.input1MTokenPrice,
                    model.output1MTokenPrice,
                    ColorTranslator.ToHtml(model.Color));

                // Set the text for both buttons in the new row
                dgvModels.Rows[index].Cells["DeleteButton"].Value = "Delete";
            }
        }

        private void DgvModels_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvModels.CurrentCell?.OwningColumn?.Name == "DeleteButton" || 
                dgvModels.CurrentCell?.OwningColumn?.Name == "CopyButton") return;

            if (isInitializing)
            {
                isInitializing = false;
                return;
            }


            if (dgvModels.SelectedRows.Count > 0)
            {
                var row = dgvModels.SelectedRows[0];
                if (row.Index == dgvModels.NewRowIndex) return; // Skip if it's the new row

                var modelGuid = row.Cells["Guid"].Value?.ToString();

                var model = NewSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);
                if (model != null)
                {
                    var aiServiceNames = GetAiServiceNames();
                    using (var editForm = new ModelEditForm(model, NewSettings.ServiceProviders))
                    {
                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            // Update the grid with the edited model
                            row.Cells["FriendlyName"].Value = model.FriendlyName;
                            row.Cells["ModelName"].Value = model.ModelName;
                            row.Cells["Guid"].Value = model.Guid;
                        }
                    }
                }
            }

            dgvModels.ClearSelection();
        }

        private void DgvModels_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Handle Copy button click
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvModels.Columns["CopyButton"].Index)
            {
                var row = dgvModels.Rows[e.RowIndex];
                var modelGuid = row.Cells["Guid"].Value?.ToString();
                var originalModel = NewSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);

                if (originalModel != null)
                {
                    // Create a deep copy of the model
                    var json = JsonConvert.SerializeObject(originalModel);
                    var newModel = JsonConvert.DeserializeObject<Model>(json);

                    // Update properties for the copy
                    newModel.FriendlyName = "Copy of " + originalModel.FriendlyName;
                    newModel.Guid = Guid.NewGuid().ToString(); // Generate new GUID

                    // Add to settings
                    NewSettings.ModelList.Add(newModel);

                    // Add new row to grid
                    var index = dgvModels.Rows.Add(
                        "Copy",
                        newModel.FriendlyName,
                        newModel.ModelName,
                        newModel.Guid,
                        newModel.input1MTokenPrice,
                        newModel.output1MTokenPrice,
                        ColorTranslator.ToHtml(newModel.Color));

                    dgvModels.Rows[index].Cells["DeleteButton"].Value = "Delete";
                    dgvModels.FirstDisplayedScrollingRowIndex = index;

                    // Open edit dialog for the copied model
                    using (var editForm = new ModelEditForm(newModel, NewSettings.ServiceProviders))
                    {
                        if (editForm.ShowDialog() == DialogResult.OK)
                        {
                            // Update the grid with edited model details
                            row.Cells["FriendlyName"].Value = newModel.FriendlyName;
                            row.Cells["ModelName"].Value = newModel.ModelName;
                            row.Cells["Guid"].Value = newModel.Guid;
                        }
                        else
                        {
                            // If canceled, remove the copy
                            NewSettings.ModelList.Remove(newModel);
                            dgvModels.Rows.RemoveAt(index);
                        }
                    }
                }
                return;
            }
            if (e.RowIndex < 0) return;

            // Handle click on the new row button (the "+" button)
            if (e.RowIndex == dgvModels.NewRowIndex)
            {
                // Create a new Model instance with default values
                var newModel = new Model
                {
                    FriendlyName = "New Model",
                    ModelName = "New Model",
                    input1MTokenPrice = 0,
                    output1MTokenPrice = 0,
                    Color = Color.White
                };

                // Open the ModelEditForm for the user to fill in details
                using (var editForm = new ModelEditForm(newModel, NewSettings.ServiceProviders))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        // Add the new model to your settings
                        NewSettings.ModelList.Add(newModel);

                        // Directly add the new row to the DataGridView
                        var index = dgvModels.Rows.Add(
                            "Copy",
                            newModel.FriendlyName,
                            newModel.ModelName,
                            newModel.Guid,
                            newModel.input1MTokenPrice,
                            newModel.output1MTokenPrice,
                            ColorTranslator.ToHtml(newModel.Color));

                        // Set the text for both buttons in the new row
                        dgvModels.Rows[index].Cells["DeleteButton"].Value = "Delete";

                        // Ensure the row is visible
                        dgvModels.FirstDisplayedScrollingRowIndex = index;
                    }
                }
                return;
            }

            // Handle Delete button click
            if (e.ColumnIndex == dgvModels.Columns["DeleteButton"].Index)
            {
                if (MessageBox.Show("Are you sure you want to delete this model?", "Confirm Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    var row = dgvModels.Rows[e.RowIndex];
                    var modelGuid = row.Cells["Guid"].Value?.ToString();

                    if (string.IsNullOrEmpty(modelGuid)) return;
                    var model = NewSettings.ModelList.FirstOrDefault(a => a.Guid == modelGuid);
                    NewSettings.ModelList.Remove(model);

                    dgvModels.Rows.RemoveAt(e.RowIndex);
                }
            }
        }

        private void DgvModels_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            var newModel = new Model
            {
                FriendlyName = "New Model",
                ModelName = "New Model",
                input1MTokenPrice = 0,
                output1MTokenPrice = 0,
                Color = Color.White
            };

            using (var editForm = new ModelEditForm(newModel, NewSettings.ServiceProviders))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    NewSettings.ModelList.Add(newModel);

                    var index = dgvModels.Rows.Add(
                        "Copy",
                        newModel.FriendlyName,
                        newModel.ModelName,
                        newModel.Guid,
                        newModel.input1MTokenPrice,
                        newModel.output1MTokenPrice,
                        ColorTranslator.ToHtml(newModel.Color));

                    dgvModels.Rows[index].Cells["DeleteButton"].Value = "Delete";
                    dgvModels.Rows[index].Cells["EditButton"].Value = "Edit";

                    dgvModels.FirstDisplayedScrollingRowIndex = index;

                }
                else
                {
                    for (int i = dgvModels.Rows.Count - 1; i >= 0; i--)
                    {
                        DataGridViewRow row = dgvModels.Rows[i];
                        if (row.Cells["ModelName"].Value == null || row.Cells["ModelName"].Value.ToString() == "")
                        {
                            dgvModels.Rows.RemoveAt(i);
                            break;
                        }
                    }
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

        private void btnEditServiceProviders_Click(object sender, EventArgs e)
        {
            var serviceProviderForm = new ServiceProviderForm(NewSettings.ServiceProviders);
            var x =serviceProviderForm.ShowDialog();

            if(x == DialogResult.OK)
            {
                NewSettings.ServiceProviders = serviceProviderForm.ServiceProviders.OrderBy(x => x.FriendlyName).ToList(); ;
            }
        }
    }
}