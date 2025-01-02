using AiTool3.Conversations;

namespace AiTool3.UI.Forms
{
    internal class ConversationDataGridView : DataGridView

        
    {
        public string SelectedConversationGuid { get; set; }

        internal void InitialiseDataGridView(EventHandler RegenerateSummary, EventHandler DeleteConversation)
        {
            var conversationCacheManager = new ConversationCacheManager();

            ColumnHeadersVisible = false;

            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            cellStyle.BackColor = Color.Black;
            cellStyle.ForeColor = Color.White;
            cellStyle.WrapMode = DataGridViewTriState.True;

            DefaultCellStyle = cellStyle;

            Columns.Add("ConvGuid", "ConvGuid");
            Columns.Add("Content", "Content");
            Columns.Add("Engine", "Engine");
            Columns.Add("Title", "Title");
            Columns[0].Visible = false;
            Columns[0].ReadOnly = true;
            Columns[1].Visible = false;
            Columns[1].ReadOnly = true;
            Columns[2].Visible = false;
            Columns[2].ReadOnly = true;
            Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Columns[3].ReadOnly = true;
            Columns[3].DefaultCellStyle.WrapMode = DataGridViewTriState.True;

            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            RowHeadersWidth = 10;

            // populate dgv with the conversation files in the current directory, ordered by date desc
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), BranchedConversation.GetFilename("*")).OrderByDescending(f => new FileInfo(f).LastWriteTime);

            // populate dgv
            foreach (var file in files)
            {
                var fileSummary = conversationCacheManager.GetSummary(file);

                var summary = fileSummary.Summary.Length > 200 ? fileSummary.Summary.Substring(0, 200) + "..." : fileSummary.Summary;
                int rowIndex = Rows.Add(fileSummary.ConvGuid, "", "", summary);

                if (fileSummary.HighlightColour.HasValue)
                {
                    var dCS = Rows[rowIndex].DefaultCellStyle;
                    dCS.BackColor = fileSummary.HighlightColour.Value;
                    dCS.ForeColor = Color.Black;
                }
            }


            ContextMenuStrip contextMenu = new ContextMenuStrip { Renderer = new CustomToolStripRenderer(new CustomProfessionalColorTable()) };

            contextMenu.Items.Add("Regenerate Summary", null, RegenerateSummary);

            contextMenu.Items.Add(new ToolStripSeparator());
            var noHighlightItem = new ToolStripMenuItem("Clear Highlight");

            foreach (var colour in new Color[] { Color.LightBlue, Color.LightGreen, Color.LightPink, Color.LightYellow, Color.LightCoral, Color.LightCyan })
            {
                var item = new ToolStripMenuItem(colour.ToString().Replace("Color [", "").Replace("]", ""));

                // add a colour swatch to the item (!)
                var bmp = new System.Drawing.Bitmap(16, 16);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(colour);

                    // add 1px solid black border
                    g.DrawRectangle(System.Drawing.Pens.Black, 0, 0, bmp.Width - 1, bmp.Height - 1);

                }

                item.Image = bmp;


                item.ToolTipText = "Change the highlight color of this conversation";

                item.Click += (s, e) =>
                {
                    var conv = BranchedConversation.LoadConversation(SelectedConversationGuid);
                    conv.HighlightColour = colour;
                    conv.SaveConversation();

                    // find the dgv row
                    foreach (DataGridViewRow row in Rows)
                    {
                        if (row.Cells[0].Value.ToString() == SelectedConversationGuid)
                        {
                            row.DefaultCellStyle.BackColor = colour;
                            row.DefaultCellStyle.ForeColor = Color.Black;
                            break;
                        }
                    }
                };
                contextMenu.Items.Add(item);

            }

            // add a split and no-highlight option which sets conv.highlightcolour to null and updates the row

            noHighlightItem.Click += (s, e) =>
            {
                var conv = BranchedConversation.LoadConversation(SelectedConversationGuid);
                conv.HighlightColour = null;
                conv.SaveConversation();

                // find the dgv row
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.Cells[0].Value.ToString() == SelectedConversationGuid)
                    {
                        row.DefaultCellStyle.BackColor = Color.Black;
                        row.DefaultCellStyle.ForeColor = Color.White;
                        break;
                    }
                }
            };

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(noHighlightItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete conversation", null, DeleteConversation);

            ContextMenuStrip = contextMenu;

        }

        internal void InitialiseRightClickMenu()
        {
            throw new NotImplementedException();
        }

        internal void RemoveConversation(string selectedConversationGuid)
        {
            foreach (DataGridViewRow row in Rows)
            {
                if (row.Cells[0].Value.ToString() == selectedConversationGuid)
                {
                    Rows.Remove(row);
                    break;
                }
            }
        }

        internal void SetConversationForDgvClick(ref string selectedConversationGuid, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = HitTest(e.X, e.Y);
                if (hti.RowIndex >= 0)
                {
                    if (!ModifierKeys.HasFlag(Keys.Control))
                    {
                        ClearSelection();
                    }
                    Rows[hti.RowIndex].Selected = true;
                    try
                    {
                        SelectedConversationGuid = Rows[hti.RowIndex].Cells[0].Value.ToString();
                        selectedConversationGuid = SelectedConversationGuid;
                    }
                    catch { }
                }
            }
        }
        
        // this method needed to ensure that the selected row is also the CurrentRow.
        // otherwise, the clicked row will not be the same as the selected one.
        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                CurrentCell = this[e.ColumnIndex, e.RowIndex];
                base.OnCellMouseDown(e);
            }
        }
    }
}