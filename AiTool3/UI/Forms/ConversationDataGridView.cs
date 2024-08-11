using AiTool3.Conversations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.UI.Forms
{
    internal class ConversationDataGridView : DataGridView
    {
        internal void InitialiseDataGridView(EventHandler RegenerateSummary, EventHandler DeleteConversation, string selectedConversationGuid)
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
            // make the last column fill the parent
            Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            Columns[3].ReadOnly = true;

            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            RowHeadersWidth = 10;

            // populate dgv with the conversation files in the current directory, ordered by date desc
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), BranchedConversation.GetFilename("*")).OrderByDescending(f => new FileInfo(f).LastWriteTime);

            // populate dgv
            foreach (var file in files)
            {
                var fileSummary = conversationCacheManager.GetSummary(file);

                int rowIndex = Rows.Add(fileSummary.ConvGuid, "", "", fileSummary.Summary);

                if (fileSummary.HighlightColour.HasValue)
                {
                    var dCS = Rows[rowIndex].DefaultCellStyle;
                    dCS.BackColor = fileSummary.HighlightColour.Value;
                    dCS.ForeColor = Color.Black;
                }
            }


            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Regenerate Summary", null, RegenerateSummary);

            contextMenu.Items.Add(new ToolStripSeparator());
            var noHighlightItem = new ToolStripMenuItem("Clear Highlight");

            foreach (var colour in new Color[] { Color.LightBlue, Color.LightGreen, Color.LightPink, Color.LightYellow, Color.LightCoral, Color.LightCyan })
            {
                var item = new ToolStripMenuItem(colour.ToString().Replace("Color [", "Highlight in ").Replace("]", ""));

                // add a colour swatch to the item (!)
                var bmp = new System.Drawing.Bitmap(16, 16);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(colour);

                    // add 1px solid black border
                    g.DrawRectangle(System.Drawing.Pens.Black, 0, 0, bmp.Width - 1, bmp.Height - 1);

                }

                item.Image = bmp;



                item.Click += (s, e) =>
                {
                    var conv = BranchedConversation.LoadConversation(selectedConversationGuid);
                    conv.HighlightColour = colour;
                    conv.SaveConversation();

                    // find the dgv row
                    foreach (DataGridViewRow row in Rows)
                    {
                        if (row.Cells[0].Value.ToString() == selectedConversationGuid)
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
                var conv = BranchedConversation.LoadConversation(selectedConversationGuid);
                conv.HighlightColour = null;
                conv.SaveConversation();

                // find the dgv row
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.Cells[0].Value.ToString() == selectedConversationGuid)
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
    }
}
