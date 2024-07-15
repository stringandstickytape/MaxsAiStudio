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
    public partial class LicensesForm : Form
    {
        public LicensesForm(string licenses)
        {
            InitializeComponent();
            tbLicenses.Text = licenses;

            // deselect all text
            tbLicenses.SelectionStart = 0;
            tbLicenses.SelectionLength = 0;
        }
    }
}
