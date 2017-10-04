using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class PWGen : Form
    {
        private static char[] m_pwgen_lower_letters = new char[] {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r',
            's', 't', 'u', 'v', 'w', 'x', 'y', 'z'};
        private static char[] m_pwgen_upper_letters = new char[] {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R',
            'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};
        private static char[] m_pwgen_integers = new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        private static char[] m_pwgen_signs = new char[] { '!', '$' , '%', '&', '/', '(', ')', '[', ']', '{', '}', '=', '?', '\'', '+', '*', '#', '-', '_', 
            ',', '.', '<', '>', '@', '|', '~', ':' };

        byte[] m_password = null;

        public byte[] Password
        {
            get { return m_password; }
        }

        //static functions for password generating and evaluation
        #region static functions
        
        //generate a random string by defining the length and the symbols to use
        public static string pwGenerator(int length, bool upper, bool lower, bool integers, bool signs)
        {
            if (!upper && !lower && !integers && !signs) return ""; //return nothing if no symbols are selected

            char[] charforpw = new char[] { };
            if (lower) charforpw = m_pwgen_lower_letters.Concat(charforpw).ToArray();
            if (upper) charforpw = m_pwgen_upper_letters.Concat(charforpw).ToArray();
            if (integers) charforpw = m_pwgen_integers.Concat(charforpw).ToArray();
            if (signs) charforpw = m_pwgen_signs.Concat(charforpw).ToArray();

            Random rnd = new Random();
            string newpw = "";
            for (int c = 1; c <= length; c++)
            {
                int rndc = rnd.Next(0, charforpw.Length);
                newpw += string.Format("{0}", charforpw[rndc]);
            }

            return newpw;
        }

        //check length and if the password contains at least on character of each selected characterset is used
        public static bool pwCheckPassword(string password, int length, bool exactlength, bool upper, bool lower, bool integers, bool signs)
        {
            if (exactlength && password.Length != length) return false;
            else if (!exactlength && password.Length < length) return false;
            if (upper && !checkStringForCharset(password, m_pwgen_upper_letters)) return false;
            if (lower && !checkStringForCharset(password, m_pwgen_lower_letters)) return false;
            if (integers && !checkStringForCharset(password, m_pwgen_integers)) return false;
            if (signs && !checkStringForCharset(password, m_pwgen_signs)) return false;
            return true;
        }

        //check string if it contains at least on character of an given charset
        private static bool checkStringForCharset(string password, char[] charset)
        {
            bool result = false;
            foreach (char x in charset)
            {
                if (password.Contains(x)) result = true;
            }
            return result;
        }
        
        //calculate the pw strength and returns the strength in a bit like value
        public static double pwStrength(string pw)
        {
            double pseudobit = 0;

            if (pw.IndexOfAny(m_pwgen_lower_letters) != -1)
            {
                pseudobit += m_pwgen_lower_letters.Length;                    
            }
            if (pw.IndexOfAny(m_pwgen_upper_letters) != -1)
            {
                pseudobit += m_pwgen_upper_letters.Length;
            }
            if (pw.IndexOfAny(m_pwgen_integers) != -1)
            {
                pseudobit += m_pwgen_integers.Length;
            }
            if (pw.IndexOfAny(m_pwgen_signs) != -1)
            {
                pseudobit += m_pwgen_signs.Length;
            }

            pseudobit = Math.Pow(pseudobit, pw.Length);
            pseudobit = Math.Log(pseudobit, 2);

            if (pseudobit >= 256) pseudobit = 256;
            if (pw.Length == 0) pseudobit = 0;
            if (double.IsInfinity(pseudobit)) pseudobit = 0;

            return pseudobit;
        }
        
        #endregion

        public PWGen()
        {                
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        //Handle the text changed event of the password textbox
        //calculate new password strength and display it and state if the password criterias are fullfilled
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            double pseudobit = PWGen.pwStrength(textBoxPassword.Text);

            progressBar1.MainText = string.Format("{0} bits", Convert.ToInt32(pseudobit));
            if (pseudobit > 128) pseudobit = 128;

            progressBar1.FillDegree = Convert.ToInt32((pseudobit / 128) * 100);
            progressBar1.Refresh();

            this.feedbackPasswordCriterias();
        }

        //cancel form
        private void buttonCancel_Click(object sender, EventArgs e)
        {            
            this.Close();
        }

        //leave form with new password
        private void buttonOK_Click(object sender, EventArgs e)
        {            
            m_password = System.Text.Encoding.Unicode.GetBytes(textBoxPassword.Text);
            textBoxPassword.Text = "";

            this.DialogResult = DialogResult.OK;
            
            this.Close();
        }

        //show/hide password
        private void checkBoxShowPW_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShowPW.Checked) textBoxPassword.PasswordChar = '\0';
            else textBoxPassword.PasswordChar = '*';
        }

        //generate password
        private void buttonGen_Click(object sender, EventArgs e)
        {
            if (checkBoxUppercase.Checked || checkBoxLowercase.Checked || checkBoxIntegers.Checked || checkBoxSigns.Checked)
            {
                bool goodpassword = false; //switches to true if at least one character of each selected is used
                string newpw = "";
                
                while (!goodpassword)
                {
                    //generate new password
                    newpw = PWGen.pwGenerator(Convert.ToInt16(numericUpDownLength.Value),
                        checkBoxUppercase.Checked,
                        checkBoxLowercase.Checked,
                        checkBoxIntegers.Checked,
                        checkBoxSigns.Checked);

                    //check new password
                    goodpassword = pwCheckPassword(newpw, Convert.ToInt16(numericUpDownLength.Value), checkBoxExactLength.Checked,
                        checkBoxUppercase.Checked,
                        checkBoxLowercase.Checked,
                        checkBoxIntegers.Checked,
                        checkBoxSigns.Checked);
                }
                textBoxPassword.Text = newpw;
            }
            else
            {
                //at least one set of characters must be activated, claim and return
                DialogResult charselection = MessageBox.Show("Please select at least one set of characters to generate a password!", "Generate password...", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
        }

        public void deletepassword()
        {
            LBPasswordAndCryptoServices.PWHandler.ClearByte(m_password);            
        }

        //State if the password criterias are fullfilled
        private void feedbackPasswordCriterias()
        {
            //All criteria feedback
            bool passwordcriteriasfullfilled = pwCheckPassword(textBoxPassword.Text, Convert.ToInt16(numericUpDownLength.Value), checkBoxExactLength.Checked,
                        checkBoxUppercase.Checked,
                        checkBoxLowercase.Checked,
                        checkBoxIntegers.Checked,
                        checkBoxSigns.Checked);

            if (passwordcriteriasfullfilled)
            {
                labelPWState.Text = "Password criterias fullfilled: YES";
                labelPWState.ForeColor = Color.Green;
                pictureBox2.Visible = true;
            }
            else
            {
                labelPWState.Text = "Password criterias fullfilled: NO";
                labelPWState.ForeColor = Color.Red;
                pictureBox2.Visible = false;
            }

            //Single criteria feedback
            pictureBoxUpper.Visible = checkStringForCharset(textBoxPassword.Text, m_pwgen_upper_letters) && checkBoxUppercase.Checked;
            pictureBoxLower.Visible = checkStringForCharset(textBoxPassword.Text, m_pwgen_lower_letters) && checkBoxLowercase.Checked;
            pictureBoxIntegers.Visible = checkStringForCharset(textBoxPassword.Text, m_pwgen_integers) && checkBoxIntegers.Checked;
            pictureBoxSigns.Visible = checkStringForCharset(textBoxPassword.Text, m_pwgen_signs) && checkBoxSigns.Checked;

            if (checkBoxExactLength.Checked && textBoxPassword.Text.Length == Convert.ToInt16(numericUpDownLength.Value)) pictureBoxLength.Visible = true;
            else if (!checkBoxExactLength.Checked && textBoxPassword.Text.Length >= Convert.ToInt16(numericUpDownLength.Value)) pictureBoxLength.Visible = true;
            else pictureBoxLength.Visible = false;
        }

        private void checkBoxLowercase_CheckedChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void checkBoxUppercase_CheckedChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void checkBoxIntegers_CheckedChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void checkBoxSigns_CheckedChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void numericUpDownLength_ValueChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void checkBoxExactLength_CheckedChanged(object sender, EventArgs e)
        {
            this.feedbackPasswordCriterias();
        }

        private void PWGen_Load(object sender, EventArgs e)
        {

        }
    }
}
