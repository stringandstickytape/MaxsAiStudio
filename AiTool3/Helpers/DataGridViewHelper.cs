using AiTool3.Conversations;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Data;
using System.Windows.Forms;

namespace AiTool3.Helpers
{
    public static class DataGridViewHelper
    {

        public static async Task InitialiseDataGridView(DataGridView dgv, ConversationCacheManager conversationCacheManager)
        {
            // hide dgv headers
            dgv.ColumnHeadersVisible = false;
            // Setting the default cell style for the DataGridView
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            cellStyle.BackColor = Color.Black;
            cellStyle.ForeColor = Color.White;
            cellStyle.WrapMode = DataGridViewTriState.True;

            dgv.DefaultCellStyle = cellStyle;

            // add cols to dgv
            dgv.Columns.Add("ConvGuid", "ConvGuid");
            dgv.Columns.Add("Content", "Content");
            dgv.Columns.Add("Engine", "Engine");
            dgv.Columns.Add("Title", "Title");
            dgv.Columns[0].Visible = false;
            dgv.Columns[0].ReadOnly = true;
            dgv.Columns[1].Visible = false;
            dgv.Columns[1].ReadOnly = true;
            dgv.Columns[2].Visible = false;
            dgv.Columns[2].ReadOnly = true;
            // make the last column fill the parent
            dgv.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns[3].ReadOnly = true;

            // make the columns wrap text
            //dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // make the selection column thin
            dgv.RowHeadersWidth = 10;


            // populate dgv with the conversation files in the current directory, ordered by date desc
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), BranchedConversation.GetFilename("*")).OrderByDescending(f => new FileInfo(f).LastWriteTime);

            

            // populate dgv
            foreach (var file in files)
            {
                var fileSummary = conversationCacheManager.GetSummary(file);

                int rowIndex = dgv.Rows.Add(fileSummary.ConvGuid, fileSummary.Content, fileSummary.Engine, fileSummary.Summary);

                if (fileSummary.HighlightColour.HasValue)
                {
                    var dCS = dgv.Rows[rowIndex].DefaultCellStyle;
                    dCS.BackColor = fileSummary.HighlightColour.Value;
                    dCS.ForeColor = Color.Black;
                }
            }




            // Handle the CellMouseClick event


        }

 


    }
}