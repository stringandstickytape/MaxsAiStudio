using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AiTool3.UI
{
    public partial class AutoSuggestUserInput : Form
    {
        public AutoSuggestUserInput()
        {
            InitializeComponent();
        }

        private void btnAutoSuggestCancel_Click(object sender, EventArgs e)
        {
            // set cancel result
            DialogResult = DialogResult.Cancel;
            Close();

        }

        private void btnAutoSuggestOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
