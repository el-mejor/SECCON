using SEC2ON.LBSecconBusinessLogic.Dialogs.Controlls;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    partial class PWGen
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PWGen));
            this.checkBoxLowercase = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxUppercase = new System.Windows.Forms.CheckBox();
            this.checkBoxIntegers = new System.Windows.Forms.CheckBox();
            this.checkBoxSigns = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownLength = new System.Windows.Forms.NumericUpDown();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.buttonGen = new System.Windows.Forms.Button();
            this.checkBoxShowPW = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelPWState = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBoxLength = new System.Windows.Forms.PictureBox();
            this.pictureBoxSigns = new System.Windows.Forms.PictureBox();
            this.pictureBoxIntegers = new System.Windows.Forms.PictureBox();
            this.pictureBoxLower = new System.Windows.Forms.PictureBox();
            this.pictureBoxUpper = new System.Windows.Forms.PictureBox();
            this.checkBoxExactLength = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar1 = new SEC2ON.LBSecconBusinessLogic.Dialogs.Controlls.HarrProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLength)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSigns)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIntegers)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLower)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpper)).BeginInit();
            this.SuspendLayout();
            // 
            // checkBoxLowercase
            // 
            this.checkBoxLowercase.AutoSize = true;
            this.checkBoxLowercase.Checked = true;
            this.checkBoxLowercase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxLowercase.Location = new System.Drawing.Point(32, 30);
            this.checkBoxLowercase.Name = "checkBoxLowercase";
            this.checkBoxLowercase.Size = new System.Drawing.Size(195, 17);
            this.checkBoxLowercase.TabIndex = 0;
            this.checkBoxLowercase.Text = "Use lower case letters (a, b, c, ..., z)";
            this.checkBoxLowercase.UseVisualStyleBackColor = true;
            this.checkBoxLowercase.CheckedChanged += new System.EventHandler(this.checkBoxLowercase_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(426, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "1. Select the characters and symbols which must be part of the password:";
            // 
            // checkBoxUppercase
            // 
            this.checkBoxUppercase.AutoSize = true;
            this.checkBoxUppercase.Checked = true;
            this.checkBoxUppercase.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUppercase.Location = new System.Drawing.Point(32, 53);
            this.checkBoxUppercase.Name = "checkBoxUppercase";
            this.checkBoxUppercase.Size = new System.Drawing.Size(202, 17);
            this.checkBoxUppercase.TabIndex = 2;
            this.checkBoxUppercase.Text = "Use upper case letters (A, B, C, ..., Z)";
            this.checkBoxUppercase.UseVisualStyleBackColor = true;
            this.checkBoxUppercase.CheckedChanged += new System.EventHandler(this.checkBoxUppercase_CheckedChanged);
            // 
            // checkBoxIntegers
            // 
            this.checkBoxIntegers.AutoSize = true;
            this.checkBoxIntegers.Checked = true;
            this.checkBoxIntegers.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIntegers.Location = new System.Drawing.Point(32, 76);
            this.checkBoxIntegers.Name = "checkBoxIntegers";
            this.checkBoxIntegers.Size = new System.Drawing.Size(151, 17);
            this.checkBoxIntegers.TabIndex = 3;
            this.checkBoxIntegers.Text = "Use integers (0, 1, 2, ..., 9)";
            this.checkBoxIntegers.UseVisualStyleBackColor = true;
            this.checkBoxIntegers.CheckedChanged += new System.EventHandler(this.checkBoxIntegers_CheckedChanged);
            // 
            // checkBoxSigns
            // 
            this.checkBoxSigns.AutoSize = true;
            this.checkBoxSigns.Checked = true;
            this.checkBoxSigns.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSigns.Location = new System.Drawing.Point(32, 99);
            this.checkBoxSigns.Name = "checkBoxSigns";
            this.checkBoxSigns.Size = new System.Drawing.Size(123, 17);
            this.checkBoxSigns.TabIndex = 4;
            this.checkBoxSigns.Text = "Use signs (!, ?, $, ...)";
            this.checkBoxSigns.UseVisualStyleBackColor = true;
            this.checkBoxSigns.CheckedChanged += new System.EventHandler(this.checkBoxSigns_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(256, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "2. Define how long the password should be:";
            // 
            // numericUpDownLength
            // 
            this.numericUpDownLength.Location = new System.Drawing.Point(32, 151);
            this.numericUpDownLength.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.numericUpDownLength.Name = "numericUpDownLength";
            this.numericUpDownLength.Size = new System.Drawing.Size(57, 20);
            this.numericUpDownLength.TabIndex = 6;
            this.numericUpDownLength.Value = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.numericUpDownLength.ValueChanged += new System.EventHandler(this.numericUpDownLength_ValueChanged);
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Location = new System.Drawing.Point(93, 203);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(415, 20);
            this.textBoxPassword.TabIndex = 7;
            this.textBoxPassword.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // buttonGen
            // 
            this.buttonGen.Location = new System.Drawing.Point(12, 201);
            this.buttonGen.Name = "buttonGen";
            this.buttonGen.Size = new System.Drawing.Size(75, 23);
            this.buttonGen.TabIndex = 8;
            this.buttonGen.Text = "Generate";
            this.buttonGen.UseVisualStyleBackColor = true;
            this.buttonGen.Click += new System.EventHandler(this.buttonGen_Click);
            // 
            // checkBoxShowPW
            // 
            this.checkBoxShowPW.AutoSize = true;
            this.checkBoxShowPW.Location = new System.Drawing.Point(32, 230);
            this.checkBoxShowPW.Name = "checkBoxShowPW";
            this.checkBoxShowPW.Size = new System.Drawing.Size(101, 17);
            this.checkBoxShowPW.TabIndex = 9;
            this.checkBoxShowPW.Text = "Show password";
            this.checkBoxShowPW.UseVisualStyleBackColor = true;
            this.checkBoxShowPW.CheckedChanged += new System.EventHandler(this.checkBoxShowPW_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 256);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(109, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "Est. password quality:";
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(457, 330);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 22;
            this.buttonOK.Text = "Use";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(376, 330);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 23;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // labelPWState
            // 
            this.labelPWState.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPWState.ForeColor = System.Drawing.Color.Red;
            this.labelPWState.Location = new System.Drawing.Point(325, 231);
            this.labelPWState.Name = "labelPWState";
            this.labelPWState.Size = new System.Drawing.Size(207, 16);
            this.labelPWState.TabIndex = 27;
            this.labelPWState.Text = "Password criterias fullfilled:  NO";
            this.labelPWState.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(514, 201);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(18, 22);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 28;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Visible = false;
            // 
            // pictureBoxLength
            // 
            this.pictureBoxLength.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxLength.Image")));
            this.pictureBoxLength.Location = new System.Drawing.Point(514, 149);
            this.pictureBoxLength.Name = "pictureBoxLength";
            this.pictureBoxLength.Size = new System.Drawing.Size(18, 22);
            this.pictureBoxLength.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxLength.TabIndex = 29;
            this.pictureBoxLength.TabStop = false;
            this.pictureBoxLength.Visible = false;
            // 
            // pictureBoxSigns
            // 
            this.pictureBoxSigns.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxSigns.Image")));
            this.pictureBoxSigns.Location = new System.Drawing.Point(514, 94);
            this.pictureBoxSigns.Name = "pictureBoxSigns";
            this.pictureBoxSigns.Size = new System.Drawing.Size(18, 22);
            this.pictureBoxSigns.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxSigns.TabIndex = 30;
            this.pictureBoxSigns.TabStop = false;
            this.pictureBoxSigns.Visible = false;
            // 
            // pictureBoxIntegers
            // 
            this.pictureBoxIntegers.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxIntegers.Image")));
            this.pictureBoxIntegers.Location = new System.Drawing.Point(514, 71);
            this.pictureBoxIntegers.Name = "pictureBoxIntegers";
            this.pictureBoxIntegers.Size = new System.Drawing.Size(18, 22);
            this.pictureBoxIntegers.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxIntegers.TabIndex = 31;
            this.pictureBoxIntegers.TabStop = false;
            this.pictureBoxIntegers.Visible = false;
            // 
            // pictureBoxLower
            // 
            this.pictureBoxLower.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxLower.Image")));
            this.pictureBoxLower.Location = new System.Drawing.Point(514, 25);
            this.pictureBoxLower.Name = "pictureBoxLower";
            this.pictureBoxLower.Size = new System.Drawing.Size(18, 22);
            this.pictureBoxLower.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxLower.TabIndex = 32;
            this.pictureBoxLower.TabStop = false;
            this.pictureBoxLower.Visible = false;
            // 
            // pictureBoxUpper
            // 
            this.pictureBoxUpper.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxUpper.Image")));
            this.pictureBoxUpper.Location = new System.Drawing.Point(514, 48);
            this.pictureBoxUpper.Name = "pictureBoxUpper";
            this.pictureBoxUpper.Size = new System.Drawing.Size(18, 22);
            this.pictureBoxUpper.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxUpper.TabIndex = 33;
            this.pictureBoxUpper.TabStop = false;
            this.pictureBoxUpper.Visible = false;
            // 
            // checkBoxExactLength
            // 
            this.checkBoxExactLength.AutoSize = true;
            this.checkBoxExactLength.Location = new System.Drawing.Point(95, 152);
            this.checkBoxExactLength.Name = "checkBoxExactLength";
            this.checkBoxExactLength.Size = new System.Drawing.Size(85, 17);
            this.checkBoxExactLength.TabIndex = 34;
            this.checkBoxExactLength.Text = "Exact length";
            this.checkBoxExactLength.UseVisualStyleBackColor = true;
            this.checkBoxExactLength.CheckedChanged += new System.EventHandler(this.checkBoxExactLength_CheckedChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 185);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(376, 13);
            this.label3.TabIndex = 35;
            this.label3.Text = "3. Enter password or hit \'Generate\' to create a random password:";
            // 
            // progressBar1
            // 
            this.progressBar1.AllowDrag = false;
            this.progressBar1.FillDegree = 1;
            this.progressBar1.LeftBarSize = 45;
            this.progressBar1.LeftText = "poor";
            this.progressBar1.Location = new System.Drawing.Point(32, 273);
            this.progressBar1.MainText = "0 bits";
            this.progressBar1.Margin = new System.Windows.Forms.Padding(0);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.RightBarSize = 45;
            this.progressBar1.RightText = "good";
            this.progressBar1.RoundedCornerAngle = 10;
            this.progressBar1.Size = new System.Drawing.Size(500, 25);
            this.progressBar1.StatusBarColor = 0;
            this.progressBar1.StatusBarSize = 0;
            this.progressBar1.StatusText = "";
            this.progressBar1.TabIndex = 26;
            // 
            // PWGen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(544, 367);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.checkBoxExactLength);
            this.Controls.Add(this.pictureBoxUpper);
            this.Controls.Add(this.pictureBoxLower);
            this.Controls.Add(this.pictureBoxIntegers);
            this.Controls.Add(this.pictureBoxSigns);
            this.Controls.Add(this.pictureBoxLength);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.labelPWState);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.checkBoxShowPW);
            this.Controls.Add(this.buttonGen);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.numericUpDownLength);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.checkBoxSigns);
            this.Controls.Add(this.checkBoxIntegers);
            this.Controls.Add(this.checkBoxUppercase);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxLowercase);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PWGen";
            this.Text = "Password wizard";
            this.Load += new System.EventHandler(this.PWGen_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLength)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSigns)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIntegers)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLower)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUpper)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxLowercase;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxUppercase;
        private System.Windows.Forms.CheckBox checkBoxIntegers;
        private System.Windows.Forms.CheckBox checkBoxSigns;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDownLength;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Button buttonGen;
        private System.Windows.Forms.CheckBox checkBoxShowPW;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private HarrProgressBar progressBar1;
        private System.Windows.Forms.Label labelPWState;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBoxLength;
        private System.Windows.Forms.PictureBox pictureBoxSigns;
        private System.Windows.Forms.PictureBox pictureBoxIntegers;
        private System.Windows.Forms.PictureBox pictureBoxLower;
        private System.Windows.Forms.PictureBox pictureBoxUpper;
        private System.Windows.Forms.CheckBox checkBoxExactLength;
        private System.Windows.Forms.Label label3;
    }
}