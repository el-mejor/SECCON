namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    partial class SelectOnlineDB
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectOnlineDB));
            this.button1 = new System.Windows.Forms.Button();
            this.listViewDBs = new System.Windows.Forms.ListView();
            this.columnHeader0 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.button2 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxShowUnknown = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(416, 254);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Download";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // listViewDBs
            // 
            this.listViewDBs.BackColor = System.Drawing.Color.White;
            this.listViewDBs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader0,
            this.columnHeader1});
            this.listViewDBs.FullRowSelect = true;
            this.listViewDBs.Location = new System.Drawing.Point(12, 35);
            this.listViewDBs.MultiSelect = false;
            this.listViewDBs.Name = "listViewDBs";
            this.listViewDBs.Size = new System.Drawing.Size(560, 186);
            this.listViewDBs.SmallImageList = this.imageList1;
            this.listViewDBs.TabIndex = 1;
            this.listViewDBs.UseCompatibleStateImageBehavior = false;
            this.listViewDBs.View = System.Windows.Forms.View.Details;
            this.listViewDBs.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewDBs_ColumnClick);
            this.listViewDBs.SelectedIndexChanged += new System.EventHandler(this.listViewDBs_SelectedIndexChanged);
            // 
            // columnHeader0
            // 
            this.columnHeader0.Text = "Database filename";
            this.columnHeader0.Width = 250;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Last modification";
            this.columnHeader1.Width = 250;
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "key-64.ico");
            this.imageList1.Images.SetKeyName(1, "key-64.ico");
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(497, 254);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 232);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(361, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Only databases in the SEC²ON application path of your dropbox are shown.";
            // 
            // checkBoxShowUnknown
            // 
            this.checkBoxShowUnknown.AutoSize = true;
            this.checkBoxShowUnknown.Location = new System.Drawing.Point(416, 231);
            this.checkBoxShowUnknown.Name = "checkBoxShowUnknown";
            this.checkBoxShowUnknown.Size = new System.Drawing.Size(87, 17);
            this.checkBoxShowUnknown.TabIndex = 4;
            this.checkBoxShowUnknown.Text = "Show all files";
            this.checkBoxShowUnknown.UseVisualStyleBackColor = true;
            this.checkBoxShowUnknown.CheckedChanged += new System.EventHandler(this.checkBoxShowUnknown_CheckedChanged);
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.Location = new System.Drawing.Point(0, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(589, 22);
            this.label10.TabIndex = 32;
            this.label10.Text = "CurrentAction";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SelectOnlineDB
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 289);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.checkBoxShowUnknown);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.listViewDBs);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelectOnlineDB";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select database to download...";
            this.Load += new System.EventHandler(this.SelectOnlineDB_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ListView listViewDBs;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.ColumnHeader columnHeader0;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxShowUnknown;
        private System.Windows.Forms.Label label10;
    }
}