using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class enterpassword : Form
    {
        bool m_verification = false;
        bool m_cancel = false;
        bool m_sessionpin = false;

        byte[] m_password = null;
        byte[] m_pin = null;

        public byte[] password
        { get { return m_password; } }

        public byte[] pin
        { get { return m_pin; } }

        public bool cancel
        { get { return m_cancel; } }

        public enterpassword(string filename, bool verified = false, bool file = false, bool pin = false)
        {
            InitializeComponent();

            m_verification = verified;
            m_sessionpin = pin;

            if (!verified) 
            {
                textBox2.Visible = false;
                label2.Visible = false;
                label3.Visible = false;
                label4.Visible = false;
                label6.Visible = false;
                progressBar1.Visible = false;
                
                this.Height = 120;
            }

            if (file)
            {
                this.Text = "Enter key to encrypt file...";
                label1.Text = "Enter key for: " + Path.GetFileName(filename);
            }
            else if (pin)
            {
                this.Text = "Enter session pin ....";
                label1.Text = "Enter session pin for: " + Path.GetFileName(filename);
                keygreen.Visible = true;                
            }
            else if (!verified) label1.Text = "Enter masterkey for: " + Path.GetFileName(filename);
            else if (verified) label1.Text = "Enter new masterkey...";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.TextLength < 4) return;
            this.DialogResult = DialogResult.OK;
            if(!m_sessionpin) m_password = Encoding.ASCII.GetBytes(textBox1.Text);
            if (m_sessionpin) m_pin = Encoding.ASCII.GetBytes(textBox1.Text);

            textBox1.Text = "";
            textBox2.Text = "";
            
            this.Close();
        }

        private void verification()
        {
            if(textBox1.TextLength >= 4) button1.Enabled = true;

            if (m_verification)
            {
                if (textBox1.Text != textBox2.Text || textBox1.TextLength < 4) button1.Enabled = false;                
            }

            if (!m_verification && textBox1.Text == "") button1.Enabled = false;
            if (button1.Enabled && m_verification) pictureBox3.Visible = true;
            else pictureBox3.Visible = false;
        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            if (textBox1.TextLength >= 4 && m_verification) pictureBox2.Visible = true;
            else pictureBox2.Visible = false;

            double pseudobit = PWGen.pwStrength(textBox1.Text);

            if (double.IsInfinity(pseudobit)) pseudobit = 0; 

            progressBar1.MainText = string.Format("{0} bits", Convert.ToInt32(pseudobit));
            if (pseudobit > 128) pseudobit = 128;       

            progressBar1.FillDegree = Convert.ToInt32((pseudobit / 128) * 100); ;
            progressBar1.Refresh();
            this.verification();
        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
            this.verification();
        }

        private void enterpassword_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            m_cancel = true;
            this.Close();
        }

        public void deletepassword()
        {
            LBPasswordAndCryptoServices.PWHandler.ClearByte(m_password);
            LBPasswordAndCryptoServices.PWHandler.ClearByte(m_pin);
        }
    }
}
