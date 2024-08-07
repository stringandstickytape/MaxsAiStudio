namespace AiTool3
{
    public partial class AutoSuggestForm : Form
    {
        private readonly DataGridView _suggestionsGrid;
        private System.Windows.Forms.Timer fadeTimer;
        private double opacity = 0;

        public event Action<string> StringSelected;

        public delegate void StringSelectedEventHandler(string selectedString);
        public AutoSuggestForm(string[] suggestions)
        {
            InitializeComponent();
            _suggestionsGrid = CreateSuggestionsGrid(suggestions);
            Controls.Add(_suggestionsGrid);

            ConfigureForm();
            SetupFadeInAnimation();
        }

        private DataGridView CreateSuggestionsGrid(string[] suggestions)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                BackgroundColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 60),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular,
                    GraphicsUnit.Point, 0),
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };

            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = "Suggestions",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    WrapMode = DataGridViewTriState.True,
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(70, 70, 70),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(10, 5, 10, 5)
                }
            };
            grid.Columns.Add(column);

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 35);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            grid.ColumnHeadersHeight = 40;

            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 50);

            foreach (var suggestion in suggestions)
            {
                grid.Rows.Add(suggestion);
            }

            grid.CellClick += (sender, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var selectedString = grid.Rows[e.RowIndex].Cells[0].Value
                        .ToString();
                    StringSelected?.Invoke(selectedString);
                    //FadeOutAndClose();
                }
            };

            return grid;
        }

        private void ConfigureForm()
        {
            Text = "Auto Suggestions";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(500, 700);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            Opacity = 0;
        }

        private void SetupFadeInAnimation()
        {
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 10;
            fadeTimer.Tick += FadeIn;
            fadeTimer.Start();
        }

        private void FadeIn(object sender, EventArgs e)
        {
            if (opacity < 1)
            {
                opacity += 0.05;
                this.Opacity = opacity;
            }
            else
            {
                fadeTimer.Stop();
            }
        }

        private void FadeOutAndClose()
        {
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 10;
            fadeTimer.Tick += FadeOut;
            fadeTimer.Start();
        }

        private void FadeOut(object sender, EventArgs e)
        {
            if (opacity > 0)
            {
                opacity -= 0.05;
                this.Opacity = opacity;
            }
            else
            {
                fadeTimer.Stop();
                this.Close();
            }
        }
    }
}