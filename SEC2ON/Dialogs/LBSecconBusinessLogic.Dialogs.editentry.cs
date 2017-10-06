using LBPasswordAndCryptoServices;
using SEC2ON.LBSecconBusinessLogic.Classes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class Editentry : Form
    {
        PWHandler m_PWManager = null;
        public PWHandler PWManager { set { m_PWManager = value; } }

        WatchChanges m_DBwatcher = null;
        public WatchChanges DBwatcher { set { m_DBwatcher = value; } }

        String m_filename = "";
        public String Filename { set { m_filename = value; } }

        List<String> m_groups = new List<String>();
        public List<String> GroupsList { set { m_groups = value; } }

        List<SecItem> m_DBEntries = null;
        public List<SecItem> DBEntries { set { m_DBEntries = value; } }

        int m_index = 0;
        public int Index { set { m_index = value; } }

        bool m_newitem = false;
        public bool NewItem { set { m_newitem = value; } }

        SecItem.ItemType m_itemtype = SecItem.ItemType.Item;
        public SecItem.ItemType Itemtype { set { m_itemtype = value; } }

        string m_oldname = "";
        bool m_renamed = false;
        bool m_pwchanged = false;
        bool m_datachanged = false;
        String m_pwhash = "";

        public ImageList Symbols { set { imageList1 = value; } }

        public bool Renamed { get{return m_renamed;} }
        public String Oldname { get{return m_oldname;} }

        public Editentry()
        {
            InitializeComponent();            
        }
        public Editentry(SecItem.ItemType ItemType, bool newitem, String filename, List<String> groupslist, List<SecItem> DBEntries, Int16 index, PWHandler pwmanager, WatchChanges DBwatcher)
        {
            InitializeComponent();

            m_filename = filename;
            m_PWManager = pwmanager;
            m_DBwatcher = DBwatcher;
            m_pwhash = m_PWManager.PWHash;
            m_groups = groupslist;
            m_newitem = newitem;

            m_DBEntries = DBEntries;
            m_index = index;

            m_itemtype = ItemType;            
        }

        //Unlock the PWHandler
        private bool UnlockDB(bool check = false)
        {   
            if (!m_PWManager.Locked) return true;

            enterpassword enternewpassword = new enterpassword(m_filename);
            enternewpassword.ShowDialog();
            if (enternewpassword.cancel) return false;

            m_PWManager.UnlockPW(enternewpassword.password);
            enternewpassword.Dispose();

            if (check && m_pwhash != m_PWManager.PWHash)
            {
                m_PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);                
            }

            return true;
        }

        //call loadentrydata
        private void editentry_Load(object sender, EventArgs e)
        {
            if (m_itemtype == SecItem.ItemType.Item)
            {
                label10.Text = "You are editing the item: ";
                if (m_newitem) label10.Text = "You are adding a new item.";
            }

            if (m_itemtype == SecItem.ItemType.Group)
            {
                label10.Text = "You are editing the group: ";
                if (m_newitem) label10.Text = "You are adding a new group.";

                //modify dialog to edit the group item
                //hide superfluous controls
                foreach (Control hideall in this.Controls) hideall.Visible = false;

                pictureBox1.Visible = true;
                label1.Visible = true;
                label1.Text = "Group:";
                textBoxName.Visible = true;
                label8.Visible = true;
                label10.Visible = true;
                listView1.Visible = true;
                button1.Visible = true;
                button2.Visible = true;

                this.Text = "Edit group...";
            }

            this.loadentrydata();
        }

        //load and display the items information
        private void loadentrydata()
        {
            //show the available icons
            for (int imgindex = 0; imgindex < imageList1.Images.Count; imgindex++)
            {
                ListViewItem newitem = new ListViewItem();
                newitem.Name = imgindex.ToString();
                newitem.Text = imgindex.ToString();
                newitem.ImageIndex = imgindex;
                listView1.Items.Add(newitem);
            }
            listView1.SmallImageList = this.imageList1;

            //fill the form with the items information            

            //load image - or if not available show default image
            if (Convert.ToInt16(m_DBEntries[m_index].ImageIndex) <= imageList1.Images.Count)
            {
                pictureBox1.Image = imageList1.Images[m_DBEntries[m_index].ImageIndex];
                listView1.Items[m_DBEntries[m_index].ImageIndex].Selected = true;
            }
            else
            {
                pictureBox1.Image = imageList1.Images[5];
                listView1.Items[5].Selected = true;
            }

            if (m_itemtype == SecItem.ItemType.Item) //fill in all information for the item
            {
                //add the groups to the combobox
                foreach (string group in m_groups)
                    comboBoxGroup.Items.Add(group);

                textBoxName.Text = m_DBEntries[m_index].Name;
                if (!m_newitem) label10.Text += m_DBEntries[m_index].Name;

                textBoxURL.Text = m_DBEntries[m_index].Url;
                comboBoxGroup.Text = m_DBEntries[m_index].Group;
                textBoxLogin.Text = Encoding.Unicode.GetString(cryptoservice.decrypt(m_DBEntries[m_index].Username, m_PWManager));
                textBoxPassword.Text = Encoding.Unicode.GetString(cryptoservice.decrypt(m_DBEntries[m_index].Password, m_PWManager));
                textBoxNotes.Text = Encoding.Unicode.GetString(cryptoservice.decrypt(m_DBEntries[m_index].Notes, m_PWManager));

                if (m_DBEntries[m_index].ExpirationDate.ToLocalTime().Year != 9999)
                {
                    dateTimePicker1.Value = m_DBEntries[m_index].ExpirationDate.ToLocalTime();
                    checkBox2.Checked = true;
                }
                else checkBox2.Checked = false;
            }
            else if (m_itemtype == SecItem.ItemType.Group) //fill in only group relevant information
            {
                textBoxName.Text = m_DBEntries[m_index].Group;
                if (!m_newitem) label10.Text += m_DBEntries[m_index].Group;
                if (textBoxName.Text.Length == 0) textBoxName.Text = "new group";
            }
            
            //nothing changed so far
            m_datachanged = false;
            m_pwchanged = false;            
        }

        //switch between showing and hiding the password
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                textBoxPassword.PasswordChar = '*';
            }
            else
            {
                if(!this.UnlockDB(check: true)) return;
                
                textBoxPassword.PasswordChar = '\0';
            }
        }

        //something changed
        private void textBoxName_TextChanged(object sender, EventArgs e)
        {   
            this.ChangesMade();

            if (m_itemtype == SecItem.ItemType.Item)
            {
                if (m_DBEntries[m_index].Name == textBoxName.Text || textBoxName.Text == SecconBL.searchUniqueName(textBoxName.Text, m_DBEntries)) ItemNameUniqueImage.Visible = true;
                else ItemNameUniqueImage.Visible = false;
            }
            if (m_itemtype == SecItem.ItemType.Group)
            {
                if (!textBoxName.Text.Contains("*") && 
                    SecconBL.IsUniqueGroupName(textBoxName.Text, m_groups, m_DBEntries[m_index].Group, !m_newitem)) ItemNameUniqueImage.Visible = true;                    
                else ItemNameUniqueImage.Visible = false;                
            }
        }

        //save button, take over changes and close form
        private void button1_Click(object sender, EventArgs e)
        {
            //Check that name is unique when the name of the item was changed
            //fill the form with the items information            

            if (!this.UnlockDB(check: true)) return;

            if (m_itemtype == SecItem.ItemType.Item) //store information
            {
                //check if group name contains "*" and return in this case
                if (comboBoxGroup.Text.Contains("*"))
                {                    
                    DialogResult grpexists = MessageBox.Show(string.Format("The group name must not contain a \"*\". Please adapt group name."),
                            "Edit item...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //label10.BackColor = Color.OrangeRed;
                    //label10.Text = string.Format("The group \"{0}\" already exists. Please enter another name.", textBoxName.Text);
                    return;
                }

                string newname = textBoxName.Text;

                //check if the item was renamed
                if (m_DBEntries[m_index].Name != textBoxName.Text)
                {
                    m_renamed = true;
                    m_oldname = m_DBEntries[m_index].Name;
                }

                //check if name already exists and giving a unique name in this case
                if (m_renamed) newname = SecconBL.searchUniqueName(textBoxName.Text, m_DBEntries);

                //store new information
                m_DBEntries[m_index].Name = newname;
                m_DBEntries[m_index].Group = comboBoxGroup.Text;
                if (listView1.SelectedItems.Count >= 1) m_DBEntries[m_index].ImageIndex = Convert.ToInt16(listView1.SelectedItems[0].Name);
                m_DBEntries[m_index].Url = textBoxURL.Text;
                m_DBEntries[m_index].Username = cryptoservice.encrypt(Encoding.Unicode.GetBytes(textBoxLogin.Text), m_PWManager);
                m_DBEntries[m_index].Password = cryptoservice.encrypt(Encoding.Unicode.GetBytes(textBoxPassword.Text), m_PWManager);
                m_DBEntries[m_index].Notes = cryptoservice.encrypt(Encoding.Unicode.GetBytes(textBoxNotes.Text), m_PWManager);

                if (checkBox2.Checked)
                {
                    m_DBEntries[m_index].ExpirationDate = dateTimePicker1.Value.ToUniversalTime();
                }
                else m_DBEntries[m_index].ExpirationDate = (DateTime.Now.AddYears(9999 - DateTime.Now.Year));

                
            }
            else if (m_itemtype == SecItem.ItemType.Group) //store icon and group only.
            {
                //check if group name contains "*" and return in this case
                if (textBoxName.Text.Contains("*"))
                {
                    DialogResult grpexists = MessageBox.Show(string.Format("The group name must not contain a \"*\". Please adapt name."),
                            "Edit group...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //label10.BackColor = Color.OrangeRed;
                    //label10.Text = string.Format("The group \"{0}\" already exists. Please enter another name.", textBoxName.Text);
                    return;
                }

                //check if new or renamed group already exists and return in this case
                if (!SecconBL.IsUniqueGroupName(textBoxName.Text, m_groups, m_DBEntries[m_index].Group, !m_newitem))
                {
                    DialogResult grpexists = MessageBox.Show(string.Format("The group \"{0}\" already exists. Please choose another one.", textBoxName.Text),
                            "Edit group...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                m_DBEntries[m_index].Group = textBoxName.Text;
                m_DBEntries[m_index].Name = SecconBL.searchUniqueName("Group: " + textBoxName.Text, m_DBEntries);
                if (listView1.SelectedItems.Count >= 1) m_DBEntries[m_index].ImageIndex = Convert.ToInt16(listView1.SelectedItems[0].Name);
            }

            if (m_pwchanged) m_DBEntries[m_index].LastModification = DateTime.Now.ToUniversalTime();
            m_DBEntries[m_index].Latest = DateTime.UtcNow;

            if (m_datachanged) m_DBwatcher.Changed = true;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        //DETERMINE PASSWORD QUALITY
        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
            m_pwchanged = true;

            double pseudobit = PWGen.pwStrength(textBoxPassword.Text);

            progressBar1.MainText = string.Format("{0} bits", Convert.ToInt32(pseudobit));
            if (pseudobit > 128) pseudobit = 128;

            progressBar1.FillDegree = Convert.ToInt32((pseudobit / 128) * 100); ;
            progressBar1.Refresh();
        }

        //something changed
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
            if(listView1.SelectedItems.Count >= 1) pictureBox1.Image = imageList1.Images[Convert.ToInt16(listView1.SelectedItems[0].Name)];
        }

        //something changed
        private void textBoxURL_TextChanged(object sender, EventArgs e)
        {
            this.ChangesMade();

            if (Uri.IsWellFormedUriString(textBoxURL.Text, UriKind.Absolute)) URLWellFormedImage.Visible = true;
            else if (System.IO.File.Exists(textBoxURL.Text)) URLWellFormedImage.Visible = true;
            else URLWellFormedImage.Visible = false;
        }

        //something changed
        private void comboBoxGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
        }

        //something changed
        private void textBoxLogin_TextChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
        }

        //something changed
        private void textBoxNotes_TextChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
        }

        //not supported so far...
        private void button3_Click(object sender, EventArgs e)
        {
            m_index = 0;
            this.loadentrydata();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            m_index = m_DBEntries.Count-1;
            this.loadentrydata();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            m_index--;
            if (m_index < 0) m_index = m_DBEntries.Count - 1;
            this.loadentrydata();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            m_index++;
            if (m_index >= m_DBEntries.Count - 1) m_index = 0;
            this.loadentrydata();
        }

        //Expiration date enabled / disabled
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.ChangesMade();

            dateTimePicker1.Enabled = checkBox2.Checked;
            if (checkBox2.Checked)
            {
                if (dateTimePicker1.Value.Year == 9999) dateTimePicker1.Value = DateTime.Now;
            }
        }

        //Expiration date changed
        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            this.ChangesMade();
        }

        //open the password generator form
        private void buttonGeneratePW1_Click(object sender, EventArgs e)
        {
            PWGen pwg = new PWGen();

            pwg.ShowDialog();

            if (pwg.DialogResult != DialogResult.Cancel && pwg.Password.Length != 0) textBoxPassword.Text = Encoding.Unicode.GetString(pwg.Password);

            pwg.deletepassword();
            pwg.Dispose();
        }

        //cancel button, close form
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        //changes in form
        private void ChangesMade()
        {
            //reset pwhandler timer
            m_PWManager.ResetTimeout();

            //set changed flag
            m_datachanged = true;
        }





    }
}
