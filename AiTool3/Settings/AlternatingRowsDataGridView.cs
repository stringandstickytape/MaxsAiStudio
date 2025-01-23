namespace AiTool3.Settings
{
    public class AlternatingRowsDataGridView : DataGridView
    {
        private Color _evenRowColor = Color.FromArgb(220, 230, 241); // Light Steel Blue
        private Color _oddRowColor = Color.FromArgb(255, 248, 220); // Cornsilk

        public AlternatingRowsDataGridView() : base()
        {
            // Set default row style for consistent appearance
            this.DefaultCellStyle.BackColor = _oddRowColor;
            this.DefaultCellStyle.ForeColor = Color.Black;
            this.DefaultCellStyle.SelectionBackColor = Color.Transparent; // Make selection transparent
            this.DefaultCellStyle.SelectionForeColor = Color.Black;

            // Set alternating row style
            this.AlternatingRowsDefaultCellStyle.BackColor = _evenRowColor;
            this.AlternatingRowsDefaultCellStyle.ForeColor = Color.Black;
            this.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.Transparent; // Make selection transparent
            this.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.Black;
        }

        // Properties for customizing the colors
        public Color EvenRowColor
        {
            get { return _evenRowColor; }
            set
            {
                _evenRowColor = value;
                this.AlternatingRowsDefaultCellStyle.BackColor = _evenRowColor;
                this.Invalidate();
            }
        }

        public Color OddRowColor
        {
            get { return _oddRowColor; }
            set
            {
                _oddRowColor = value;
                this.RowsDefaultCellStyle.BackColor = _oddRowColor;
                this.Invalidate();
            }
        }

        protected override void OnDataSourceChanged(EventArgs e)
        {
            base.OnDataSourceChanged(e);
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);

            if (this.Rows[e.RowIndex].Selected)
            {
                // Manually set the background color for selected cells to match the row color
                if (e.RowIndex % 2 == 0)
                {
                    e.CellStyle.SelectionBackColor = _oddRowColor;
                }
                else
                {
                    e.CellStyle.SelectionBackColor = _evenRowColor;
                }
            }
        }
    }
}