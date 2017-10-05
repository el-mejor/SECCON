using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class AboutDlg : Form
    {
        #region constructor
        public AboutDlg()
        {
            InitializeComponent();

            labelVersion.Text = $"V{Assembly.GetEntryAssembly().GetName().Version.Major}.{Assembly.GetEntryAssembly().GetName().Version.Minor}.{Assembly.GetEntryAssembly().GetName().Version.Build}";
            if (Assembly.GetEntryAssembly().GetName().Version.Revision > 0)
                labelVersion.Text += $"-beta{Assembly.GetEntryAssembly().GetName().Version.Revision}";

            using (StreamReader s = new StreamReader("Src\\License.md"))
            {
                while(!s.EndOfStream)
                    textBoxLicense.Text += s.ReadLine() + Environment.NewLine;
            }
        }
        #endregion

        #region eventhandler
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        private void label5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(label5.Text);
        }

        private void label8_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(label8.Text);
        }
    }
}
