using LBPasswordAndCryptoServices;
using SEC2ON.LBSecconBusinessLogic;
using SEC2ON.LBSecconBusinessLogic.Classes;
using SEC2ON.LBSecconBusinessLogic.Dialogs;
using SEC2ON.LBSecconBusinessLogic.Dialogs.Controlls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SEC2ON
{
    public partial class SECCONFORM : Form
    {
        private dayscale scale = new dayscale();
        private SecconBL secconbl = new SecconBL();
        private sortingOrderItems m_sorting = sortingOrderItems.AscName;
        public bool EnableDebugLog { get; set; }

        public SECCONFORM(string[] args)
        {
            string opendb = "";

            if (args.Length == 1 && args[0] == "/debug") EnableDebugLog = true;
            else if (args.Length == 1)
            {
                if (File.Exists(args[0].ToString())) opendb = args[0];
            }
            else if (args.Length == 2 && args[0] == "/debug")
            {
                EnableDebugLog = true;
                if (File.Exists(args[1].ToString())) opendb = args[1];
            }
            
            else EnableDebugLog = false;

            InitializeComponent();

            //Write title to the form
            this.Text = "SECCON";
            toolStripVersion.Text = "V" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            //Load BL
            secconbl.GUI = this;

            //subscribe to BL events
            secconbl.PWManager.PWHandlerEvent += new PWHandler.PWEventHandler(PWHandler_Event);
            secconbl.WatchChangesDB.DatabaseChanged += new WatchChanges.Databasechanged_Handler(DBChanged_Event);
            secconbl.Killclipboard.Tick += new EventHandler(killclipboard_tick);

            //add scale            
            scale.Dock = DockStyle.Right;
            scale.Orientation = dayscale.ScaleOrientation.Vertical;
            scale.Width = 45;
            scale.Height = splitContainer2.Panel2.Height;
            scale.Maximum = 360;
            scale.ContextMenuStrip = this.contextMenuStripObsoleteSettings;
            scale.Text = "PW Age" + Environment.NewLine + "[days]";
            scale.Unit = "";
            splitContainer2.Panel1.Controls.Add(scale);
            scale.BringToFront();
            listViewAccounts.BringToFront();



            //open db if a valid db name was given
            if (secconbl.SelfTestResult && opendb != "")
            {
                this.updateLog(2, string.Format("{0} - Ready.", secconbl.SelfTestResultString));

                if (Path.GetExtension(opendb).ToLower() == ".sdb")
                {
                    this.opendatabase(opendb);                                      
                }
                if (Path.GetExtension(opendb).ToLower() == ".sef")
                {
                    string[] files = new string[1];
                    files[0] = opendb;
                    secconbl.decryptfile(files, toolStripProgressBar1, openfile: false);
                    this.Close();
                }
            }
            else if (secconbl.SelfTestResult)
            {
                this.updateLog(2, string.Format("{0} - Ready.", secconbl.SelfTestResultString));
            }
            else
            {
                this.updateLog(4, secconbl.SelfTestResultString);
            }            
        }

        #region GUIFucntions and BL Forwarding  
        //Show the items in the listview - sort it into groups and only display items of the selected group
        //filter items if the search group is selected
        public enum sortingOrderItems { AscName, AscGroup, AscLastModification, DesName, DesGroup, DesLastModification };
        public void showlist(string filtergroup)
        {
            //performance check
            DateTime start = DateTime.Now;
            ////////////////////////////////////////////

            //if no group is defined use *All
            if (filtergroup == "") filtergroup = "*All";

            //check if database is unlocked
            if (!secconbl.UnlockDB(check: true)) return;

            //clear listviews
            listViewAccounts.Clear();

            //reset counter to 0
            foreach (ListViewItem groupitem in listViewGroups.Items)
            {
                groupitem.SubItems[1].Text = "0";
            }

            //switch view
            if (checkBoxDetailsView.Checked) listViewAccounts.View = System.Windows.Forms.View.Details;
            else listViewAccounts.View = System.Windows.Forms.View.Tile;

            //define columns
            listViewAccounts.Columns.Add("NAME");
            listViewAccounts.Columns.Add("GROUP");
            listViewAccounts.Columns.Add("URL");
            listViewAccounts.Columns.Add("USERNAME/LOGIN");
            listViewAccounts.Columns.Add("PASSWORD");
            listViewAccounts.Columns.Add("LAST CHANGE (PW)");

            //Add columns to grouplist if they are not added yet
            if (listViewGroups.Columns.Count != 2)
            {
                listViewGroups.Columns.Add("GROUPS");
                listViewGroups.Columns.Add("ITEMS");
                listViewGroups.Columns[0].Width = 100;
                listViewGroups.Columns[1].Width = 50;
            }

            //Add generic groups                        
            this.addgrouptogrouplist("*All", 0);
            this.addgrouptogrouplist("*Expired", 0);
            this.addgrouptogrouplist("*Search results", 10);

            int i = 0;
            int dbindex = 0;

            //First add groups which are definated
            foreach (SecItem entry in secconbl.DBGroups)
            {
                if (!entry.GetDeletedFlag())
                {
                    string group = entry.Group;
                    Int16 groupicon = 0;
                    if (entry.GetGroupDefinitionFlag()) groupicon = entry.ImageIndex;

                    this.addgrouptogrouplist(group, groupicon);
                }
            }

            //Soting the items in the Database         
            if (m_sorting == sortingOrderItems.AscName)
            {
                listViewAccounts.Columns[0].Text = "<> " + listViewAccounts.Columns[0].Text;
                secconbl.DBEntries.Sort((a, b) => a.Name.CompareTo(b.Name));
            }
            if (m_sorting == sortingOrderItems.DesName)
            {
                listViewAccounts.Columns[0].Text = "<> " + listViewAccounts.Columns[0].Text;
                secconbl.DBEntries.Sort((b, a) => a.Name.CompareTo(b.Name));
            }
            if (m_sorting == sortingOrderItems.AscGroup)
            {
                listViewAccounts.Columns[1].Text = "<> " + listViewAccounts.Columns[1].Text;
                secconbl.DBEntries.Sort((a, b) => a.Group.CompareTo(b.Group));
            }
            if (m_sorting == sortingOrderItems.DesGroup)
            {
                listViewAccounts.Columns[1].Text = "<> " + listViewAccounts.Columns[1].Text;
                secconbl.DBEntries.Sort((b, a) => a.Group.CompareTo(b.Group));
            }
            if (m_sorting == sortingOrderItems.AscLastModification)
            {
                listViewAccounts.Columns[5].Text = "<> " + listViewAccounts.Columns[5].Text;
                secconbl.DBEntries.Sort((a, b) => a.LastModification.CompareTo(b.LastModification));
            }
            if (m_sorting == sortingOrderItems.DesLastModification)
            {
                listViewAccounts.Columns[5].Text = "<> " + listViewAccounts.Columns[5].Text;
                secconbl.DBEntries.Sort((b, a) => a.LastModification.CompareTo(b.LastModification));
            }

            //Adding the items to the list
            foreach (SecItem entry in secconbl.DBEntries)
            {
                //skip item if it is deleted
                if (entry.GetDeletedFlag())
                {
                    dbindex++;
                    continue;
                }

                //check if item is good for search results
                bool itemissearchresult = false;
                if (!entry.GetGroupDefinitionFlag() &&
                    (entry.Name.ToLower().Contains(toolStripSearch.Text.ToLower()) ||
                    Encoding.Unicode.GetString(
                    cryptoservice.decrypt(entry.Notes, secconbl.PWManager))
                    .ToLower().Contains(toolStripSearch.Text.ToLower())))
                    itemissearchresult = true;

                //if there are items without group add the *default group
                string group = entry.Group;
                if (group == "") group = "*Default";

                //Add all possible groups to grouplist and count items
                int groupicon = -1;

                this.addgrouptogrouplist(group, groupicon);
                this.countitemsineachgroup(group, itemissearchresult, DateTime.Compare(entry.ExpirationDate, DateTime.UtcNow));

                //Only show items of the selected group except *all is selected - in this case show each item
                //If the *Search results group is selected only show items which fit the search string in the name 
                //and the notes
                if (group.ToUpper() == filtergroup.ToUpper() ||
                    filtergroup == "*All" ||
                    (filtergroup == "*Search results" && itemissearchresult) ||
                    (filtergroup == "*Expired" && DateTime.Compare(entry.ExpirationDate, DateTime.UtcNow) < 0))
                {
                    //add the item to the listview - get all information to fill the columns and the notes textbox
                    listViewAccounts.Items.Add(entry.Name);

                    //load image or - if image not available load default image
                    if (entry.ImageIndex <= imageList1.Images.Count) listViewAccounts.Items[i].ImageIndex = entry.ImageIndex;
                    else listViewAccounts.Items[i].ImageIndex = 5;

                    listViewAccounts.Items[i].Name = dbindex.ToString();
                    listViewAccounts.Items[i].SubItems.Add(entry.Group);
                    listViewAccounts.Items[i].SubItems.Add(entry.Url);
                    //logins only will be displayed if showlogins is true
                    if (secconbl.ShowUserNameIsActive) listViewAccounts.Items[i].SubItems.Add(Encoding.Unicode.GetString(cryptoservice.decrypt(entry.Username, secconbl.PWManager)));
                    else listViewAccounts.Items[i].SubItems.Add("********************");
                    //show hidden password only if a password is stored
                    if (cryptoservice.decrypt(entry.Password, secconbl.PWManager).Length != 0) listViewAccounts.Items[i].SubItems.Add("********************");
                    else listViewAccounts.Items[i].SubItems.Add("");

                    listViewAccounts.Items[i].SubItems.Add(string.Format("{0}", entry.LastModification.ToLocalTime().ToString()));

                    //determine age of the password and highlight the listview item - when enabled
                    if (checkBoxHighlightAge.Checked)
                    {
                        //Highlight listviewitem depending on its passwords age
                        dayscale.highlightpasswordage(listViewAccounts.Items[i], Convert.ToInt16((DateTime.Now - entry.LastModification).Days), secconbl.obsolete);
                    }

                    //if the expiration date is expired highlight the item red
                    if (DateTime.Compare(entry.ExpirationDate, DateTime.UtcNow) < 0)
                    {
                        listViewAccounts.Items[i].BackColor = Color.White;
                        listViewAccounts.Items[i].ForeColor = Color.Red;
                    }

                    i++;
                }
                dbindex++;
            }



            listViewAccounts.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            if (listViewAccounts.Columns[2].Width > 150) listViewAccounts.Columns[2].Width = 150;
            listViewGroups.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listViewGroups.Columns[1].Width = listViewGroups.Width - listViewGroups.Columns[0].Width;

            //highlight every second line
            for (int listindex = 1; listindex < listViewGroups.Items.Count; listindex = listindex + 2)
            {
                listViewGroups.Items[listindex].BackColor = Color.WhiteSmoke;
            }

            if (!checkBoxHighlightAge.Checked && listViewAccounts.View != View.Tile)
            {
                for (int listindex = 1; listindex < listViewAccounts.Items.Count; listindex = listindex + 2)
                {
                    listViewAccounts.Items[listindex].BackColor = Color.WhiteSmoke;
                }
            }

            //activate toolstrip buttons when a database is opened
            toolStripButtonSaveAs.Enabled = true;
            toolStripSave.Enabled = true;
            toolStripSearch.Enabled = true;
            toolStripButton1.Enabled = true;
            saveToDropboxToolStripMenuItem.Enabled = true;
            checkBoxHighlightAge.Enabled = true;
            checkBoxShowUsernames.Enabled = true;
            synchronizeToolStripMenuItem.Enabled = true;
            allowSessionPinToolStripMenuItem.Enabled = true;
            importItemsToolStripMenuItem.Enabled = true;
            contextMenuStripObsoleteSettings.Enabled = true;
            contextMenuStripentry.Enabled = true;

            toolStripMenuItem2.Enabled = true;
            toolStripButton1.Enabled = true;

            ////redraw legend
            scale.Maximum = secconbl.obsolete;
            this.scale.Refresh();

            TimeSpan perf = DateTime.Now - start;

            if(EnableDebugLog) updateLog(2, string.Format("Refresh list in {0} ms.", perf.Milliseconds + perf.Seconds * 1000), true, Color.Magenta, Color.White);
        }

        //Open a database
        public void opendatabase(string filename)
        {
            
            if(!secconbl.open_database(filename)) return;

            secconbl.m_groups.Clear();
            listViewGroups.Clear();

            checkBoxHighlightAge.Checked = secconbl.HighLightPasswordAge;
            checkBoxShowUsernames.Checked = secconbl.ShowUserNameIsActive;
            checkBoxDetailsView.Checked = secconbl.DetailsView;
            allowSessionPinToolStripMenuItem.Checked = secconbl.AllowSessionPin;

            this.showlist("*All");
        }

        //Add groups to grouplist
        private void addgrouptogrouplist(string group, int image = 0)
        {
            ListViewItem newgroupitem = new ListViewItem();
            newgroupitem.Name = group;
            if (group == "*Expired")
            {
                newgroupitem.BackColor = Color.White;
                newgroupitem.ForeColor = Color.Red;
            }
            newgroupitem.Text = group;
            if (image >= 0) newgroupitem.ImageIndex = image;
            //don't add a group twice
            if (!listViewGroups.Items.ContainsKey(newgroupitem.Name))
            {
                listViewGroups.Items.Add(newgroupitem);
                listViewGroups.Items[listViewGroups.Items.Count - 1].SubItems.Add("0");
                secconbl.m_groups.Add(group, listViewGroups.Items.Count - 1);
            }
        }

        //count items in each group
        private void countitemsineachgroup(string group, bool itemissearchresult, int expired = 1)
        {
            //count the members of each group
            int groupindex = -1;
            secconbl.m_groups.TryGetValue(group, out groupindex);
            if (groupindex != -1)
                listViewGroups.Items[groupindex].SubItems[1].Text = (Convert.ToInt16(listViewGroups.Items[groupindex].SubItems[1].Text) + 1).ToString();

            //count *All group
            groupindex = -1;
            secconbl.m_groups.TryGetValue("*All", out groupindex);
            if (groupindex != -1)
                listViewGroups.Items[groupindex].SubItems[1].Text = (Convert.ToInt16(listViewGroups.Items[groupindex].SubItems[1].Text) + 1).ToString();

            //count *Search group items
            groupindex = -1;
            secconbl.m_groups.TryGetValue("*Search results", out groupindex);
            if (groupindex != -1 && itemissearchresult)
                listViewGroups.Items[groupindex].SubItems[1].Text = (Convert.ToInt16(listViewGroups.Items[groupindex].SubItems[1].Text) + 1).ToString();

            //count *Expired group items
            groupindex = -1;
            secconbl.m_groups.TryGetValue("*Expired", out groupindex);
            if (groupindex != -1 && expired < 0)
                listViewGroups.Items[groupindex].SubItems[1].Text = (Convert.ToInt16(listViewGroups.Items[groupindex].SubItems[1].Text) + 1).ToString();
            if (listViewGroups.Items[groupindex].SubItems[1].Text != "0") listViewGroups.Items[groupindex].ForeColor = Color.Red;
            else listViewGroups.Items[groupindex].ForeColor = Color.Black;
        }

        //Logging and Statusbar update
        public void updateLog(int imageindex, string text, bool logonly = false)
        {
            this.updateLog(imageindex, text, logonly, Color.Black, listViewLog.BackColor);
        }
        public void updateLog(int imageindex, string text, Color TextColor)
        {
            this.updateLog(imageindex, text, true, TextColor, listViewLog.BackColor);
        }
        public void updateLog(int imageindex, string text, Color TextColor, Color BackColor)
        {
            this.updateLog(imageindex, text, true, TextColor, BackColor);
        }
        public void updateLog(int imageindex, string text, bool logonly, Color TextColor, Color BackColor)
        {
            if (!logonly)
            {
                toolStripOwner.Text = text;
                toolStripOwner.Image = imageListToolStrip.Images[imageindex];
            }

            listViewLog.Items.Add(DateTime.Now.ToString());
            if (imageindex >= 0) listViewLog.Items[listViewLog.Items.Count - 1].ImageIndex = imageindex;

            listViewLog.Items[listViewLog.Items.Count - 1].SubItems.Add(text);
            listViewLog.Items[listViewLog.Items.Count - 1].ForeColor = TextColor;
            listViewLog.Items[listViewLog.Items.Count - 1].BackColor = BackColor;

            listViewLog.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            listViewLog.Items[listViewLog.Items.Count - 1].EnsureVisible();
        }

        //Close DB
        private Boolean closedb()
        {
            if (!secconbl.closeDatabase()) return false;

            this.Text = "SECCON";

            //clear all information of the database
            listViewAccounts.Clear();
            secconbl.m_groups.Clear();
            listViewGroups.Clear();
            textBoxDetails.Text = "";

            //disable buttons which are only neccessary if database is opened
            toolStripButtonSaveAs.Enabled = false;
            toolStripSave.Enabled = false;
            toolStripSearch.Enabled = false;
            toolStripButton1.Enabled = false;
            checkBoxHighlightAge.Enabled = false;
            checkBoxShowUsernames.Enabled = false;
            synchronizeToolStripMenuItem.Enabled = false;
            allowSessionPinToolStripMenuItem.Enabled = false;
            importItemsToolStripMenuItem.Enabled = false;
            contextMenuStripObsoleteSettings.Enabled = false;
            contextMenuStripentry.Enabled = false;

            toolStripMenuItem2.Enabled = false;
            toolStripButton1.Enabled = false;

            saveToDropboxToolStripMenuItem.Enabled = false;

            toolStripunsaved.Text = "";

            return true;
        }

        //Open the editentry form
        public void editentry()
        {
            if (listViewAccounts.SelectedItems.Count != 1) return;
            int index = Convert.ToInt16(listViewAccounts.SelectedItems[0].Name);

            //check if database is unlocked
            if (!secconbl.UnlockDB(check: true)) return;

            secconbl.editentry(index, imageList2);

            if (listViewGroups.SelectedItems.Count >= 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //BrowseURL
        public void BrowseURL()
        {
            foreach (ListViewItem item in listViewAccounts.SelectedItems)
            {
                SecItem getelementsinfo = secconbl.DBEntries[Convert.ToInt16(item.Name)];
                try { System.Diagnostics.Process.Start(getelementsinfo.Url); }
                catch
                {
                    this.updateLog(4, string.Format("Can not open the URL: {0}", getelementsinfo.Url));
                }
            }
        }

        //Copy Password to clipboard and start timer to delete clipboard
        public void CopyPWToClipboard()
        {
            if (listViewAccounts.SelectedItems.Count != 1) return;
            SecItem getelementsinfo = secconbl.DBEntries[Convert.ToInt16(listViewAccounts.SelectedItems[0].Name)];

            //check if database is unlocked
            if (!secconbl.UnlockDB(check: true)) return;

            try
            {
                Clipboard.SetText(Encoding.Unicode.GetString(cryptoservice.decrypt(getelementsinfo.Password, secconbl.PWManager)));
            }
            catch
            {
                this.updateLog(4, "There's no password, nothing to copy.");
                return;
            }

            secconbl.Killclipboardtimerexpired = Convert.ToInt32(Properties.Resources.timeToDeleteClipboardSeconds); //time to kill clipboard
            secconbl.Killclipboard.Start();

            this.updateLog(3, string.Format("Password of \"{0}\" copied to clipboard. The clipboard will be deleted in 10 seconds.", getelementsinfo.Name), false, Color.Black, Color.Yellow);
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Maximum = secconbl.Killclipboardtimerexpired;
        }

        //Copy Username to clipboard
        public void CopyUNToClipboard()
        {
            if (listViewAccounts.SelectedItems.Count != 1) return;
            SecItem getelementsinfo = secconbl.DBEntries[Convert.ToInt16(listViewAccounts.SelectedItems[0].Name)];

            //check if database is unlocked
            if (!secconbl.UnlockDB(check: true)) return;

            try
            {
                Clipboard.SetText(Encoding.Unicode.GetString(cryptoservice.decrypt(getelementsinfo.Username, secconbl.PWManager)));
            }
            catch
            {
                this.updateLog(4, "There's no username, nothing to copy.");
                return;
            }

            this.updateLog(2, "Username / Login copied to clipboard.");
        }

        //Lock, Unlock Workspace
        public void LockWorkSpace(bool lockws)
        {
            this.toolStrip1.Enabled = !lockws;
            this.toolStrip2.Enabled = !lockws;
            listViewAccounts.Enabled = !lockws;
            listViewGroups.Enabled = !lockws;
        }
        #endregion

        #region Useractions
        //Synchronize with another database
        private void synchronizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            secconbl.SynchronizeWithOtherDB();

            secconbl.m_groups.Clear();
            listViewGroups.Clear(); //redundant?

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //Create new database
        private void toolStripNew_Click_1(object sender, EventArgs e)
        {
            if (!this.closedb()) return;
            secconbl.CreateNewDatabase();
        }

        //Call open database
        private void toolStripOpen_Click_1(object sender, EventArgs e)
        {
            if (!this.closedb()) return;
            this.opendatabase(null);
        }

        //A item was selected in the listview
        private void listViewAccounts_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxDetails.Text = "";

            //Show information of the selected item in details textbox
            if (listViewAccounts.SelectedItems.Count == 1)
            {
                //Check if database is unlocked
                if (!secconbl.UnlockDB(check: true)) return;

                SecItem entry = secconbl.DBEntries[Convert.ToInt16(listViewAccounts.SelectedItems[0].Name)];
                if (entry.GetDeletedFlag()) return;

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText("Name: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(entry.Name);

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText(" Group: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(entry.Group);

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText(" URL: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(entry.Url + Environment.NewLine);

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText("Username: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(Encoding.Unicode.GetString(cryptoservice.decrypt(entry.Username, secconbl.PWManager)));

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText(" Password: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText("***" + Environment.NewLine);

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText("Notes: " + Environment.NewLine);
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(Encoding.Unicode.GetString(cryptoservice.decrypt(entry.Notes, secconbl.PWManager)) + Environment.NewLine);

                if (entry.ExpirationDate.ToLocalTime().Year != 9999)
                {
                    textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                    textBoxDetails.AppendText("Expiration date: ");
                    textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                    textBoxDetails.AppendText(entry.ExpirationDate.ToLocalTime().ToString() + Environment.NewLine);
                }

                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
                textBoxDetails.AppendText("Last change of password: ");
                textBoxDetails.SelectionFont = new Font("Microsoft Sans Serif", 8, FontStyle.Regular);
                textBoxDetails.AppendText(entry.LastModification.ToLocalTime().ToString());
            }
            else textBoxDetails.Text = "";

            scale.Marker.Clear();
            foreach (ListViewItem item in listViewAccounts.SelectedItems)
            {
                SecItem entry = secconbl.DBEntries[Convert.ToInt16(item.Name)];
                scale.Marker.Add(entry.LastModification);
            }
            scale.Refresh();

            //second toolstrip (above listview)
            if (listViewAccounts.SelectedItems.Count == 1)
            {
                toolstrip2ButtonEDIT.Enabled = true;
                toolstrip2ButtonDELETE.Enabled = true;
                toolstrip2ButtonBROWSE.Enabled = true;
                toolstrip2ButtonCOPYPW.Enabled = true;
                toolstrip2ButtonCOPYUN.Enabled = true;
            }
            else if ((listViewAccounts.SelectedItems.Count > 1))
            {
                toolstrip2ButtonEDIT.Enabled = false;
                toolstrip2ButtonDELETE.Enabled = true;
                toolstrip2ButtonBROWSE.Enabled = false;
                toolstrip2ButtonCOPYPW.Enabled = false;
                toolstrip2ButtonCOPYUN.Enabled = false;
            }
            else
            {
                toolstrip2ButtonEDIT.Enabled = false;
                toolstrip2ButtonDELETE.Enabled = false;
                toolstrip2ButtonBROWSE.Enabled = false;
                toolstrip2ButtonCOPYPW.Enabled = false;
                toolstrip2ButtonCOPYUN.Enabled = false;
            }

            //enable copy/paste items if the context menu
            copyToolStripMenuItem.Enabled = true;
            paseToolStripMenuItem.Enabled = true;
            deleteEntryToolStripMenuItem.Enabled = true;
            editEntryToolStripMenuItem.Enabled = true;
        }

        //When the app is going to be closed close the database first. Abort if there are unsaved changes
        private void SECCONFORM_FormClosing(object sender, FormClosingEventArgs e)
        {
            //delete clipboard if kill clipboard timer is running to prevent that a password remains in the clipboard after closing
            if (secconbl.Killclipboard.Enabled)
            {
                secconbl.Killclipboardtimerexpired = 0;
                secconbl.killclipboard_tick(sender, e);
            }

            if (!this.closedb()) e.Cancel = true;
        }

        //Forward to savedatabase
        private void toolStripSave_Click_1(object sender, EventArgs e)
        {
            secconbl.savedatabase();
        }

        //Forward to savedatabase
        private void toolStripButtonSaveAs_Click_1(object sender, EventArgs e)
        {
            secconbl.savedatabase(saveasnewfile: true);
        }

        //Another group is selected, call showlist
        private void listViewGroups_MouseClick(object sender, MouseEventArgs e)
        {
            if (listViewGroups.SelectedItems.Count == 1)
            {
                toolStripButtonEditGroup.Enabled = true;
                toolStripButtonDeleteGroup.Enabled = true;
                toolStripButtonMoveGroupUp.Enabled = true;
                toolStripButtonMoveGroupDown.Enabled = true;

                this.showlist(listViewGroups.SelectedItems[0].Text);

                deleteEntryToolStripMenuItem.Enabled = true;
                editEntryToolStripMenuItem.Enabled = true;
            }
            else
            {

                toolStripButtonEditGroup.Enabled = false;
                toolStripButtonDeleteGroup.Enabled = false;
                toolStripButtonMoveGroupUp.Enabled = false;
                toolStripButtonMoveGroupDown.Enabled = false;
            }
        }

        //copy the username to the clipboard
        private void copyUsernameToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyUNToClipboard();
        }

        //copy the username to the clipboard
        private void toolstrip2ButtonCOPYUN_Click(object sender, EventArgs e)
        {
            CopyUNToClipboard();
        }

        //copy the password to the clipboard, start timer to delete clipboard
        private void copyPasswordToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyPWToClipboard();
        }

        //copy the password to the clipboard, start timer to delete clipboard
        private void toolstrip2ButtonCOPYPW_Click(object sender, EventArgs e)
        {
            CopyPWToClipboard();
        }

        //open browser to surf the URL
        private void toolstrip2ButtonBROWSE_Click(object sender, EventArgs e)
        {
            BrowseURL();
        }

        //open browser to surf the URL
        private void browseURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseURL();
        }

        //lock the database
        private void lockDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!secconbl.PWManager.Locked) secconbl.PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.User);
        }

        //lock the database
        private void toolStripButtonLock_Click(object sender, EventArgs e)
        {
            if (!secconbl.PWManager.Locked) secconbl.PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.User);
        }

        //Lock the database
        private void lockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!secconbl.PWManager.Locked) secconbl.PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.User);
        }

        //Allow session pin menu item
        private void allowSessionPinToolStripMenuItem_Click(object sender, EventArgs e)
        {
            allowSessionPinToolStripMenuItem.Checked = !allowSessionPinToolStripMenuItem.Checked;
            if (allowSessionPinToolStripMenuItem.Checked && !secconbl.Sessionpinactive)
            {
                this.updateLog(2, "You will be asked for a session pin when you are opening the database in future.");
            }
            if (!allowSessionPinToolStripMenuItem.Checked)
            {
                this.updateLog(2, "You will not be asked for a session pin when you are opening the database in future.");
            }
            if (!allowSessionPinToolStripMenuItem.Checked && secconbl.Sessionpinactive)
            {
                secconbl.PWManager.SetPin = null;
                secconbl.Sessionpinactive = false;
                this.updateLog(2, "The current session pin was invalidated. You will not be asked for a session pin when you are opening the database in future.");
            }

        }

        //forward to closedatabase
        private void closeDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.closedb();
        }

        //forward to closedatabase
        private void toolStripButtonClose_Click(object sender, EventArgs e)
        {
            this.closedb();
        }

        //forward to editentry
        private void editEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ToolStripItem menuItem = sender as ToolStripItem;
            //ContextMenuStrip menu = menuItem.Owner as ContextMenuStrip;
            //Control Sender = menu.SourceControl;

            if (listViewAccounts.Focused) editentry();
            if (listViewGroups.Focused) secconbl.EditGroup(listViewGroups.SelectedItems[0].Text, imageList2);

            secconbl.m_groups.Clear();
            listViewGroups.Clear();

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //forward to editentry
        private void listViewAccounts_DoubleClick(object sender, EventArgs e)
        {
            editentry();
        }

        //forward to editentry
        private void toolStrip2ButtonEDIT_Click(object sender, EventArgs e)
        {
            editentry();
        }

        //forward to add new item prepare
        private void addEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripItem menuItem = sender as ToolStripItem;
            ContextMenuStrip menu = menuItem.Owner as ContextMenuStrip;
            Control Sender = menu.SourceControl;

            string selectedgroup = "";
            if (listViewGroups.SelectedItems.Count == 1 && !listViewGroups.SelectedItems[0].Text.Contains("*")) selectedgroup = listViewGroups.SelectedItems[0].Text;

            if (Sender.Name == listViewAccounts.Name) secconbl.addEmptyItem(SecItem.ItemType.Item, secconbl.DBEntries, selectedgroup, imageList2);
            if (Sender.Name == listViewGroups.Name)
            {
                secconbl.addEmptyItem(SecItem.ItemType.Group, secconbl.DBEntries, selectedgroup, imageList2);
                secconbl.m_groups.Clear();
                listViewGroups.Clear(); //redundant?
            }

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //forward to add new item prepare
        private void toolStrip2ButtonADD_Click(object sender, EventArgs e)
        {
            string selectedgroup = "";
            if (listViewGroups.SelectedItems.Count == 1 && !listViewGroups.SelectedItems[0].Text.Contains("*")) selectedgroup = listViewGroups.SelectedItems[0].Text;

            if (secconbl.Filename != "") secconbl.addEmptyItem(SecItem.ItemType.Item, secconbl.DBEntries, selectedgroup, imageList2);

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //forward to delete item
        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewAccounts.Focused)
            {
                List<int> indexes = new List<int>();
                foreach (ListViewItem item in listViewAccounts.SelectedItems)
                {
                    indexes.Add(Convert.ToInt16(item.Name));
                }

                if (listViewAccounts.SelectedItems.Count == 0) return;
                secconbl.deleteItem(indexes);
            }
            if (listViewGroups.Focused)
            {
                secconbl.EditGroup(listViewGroups.SelectedItems[0].Text, imageList2, true);
                secconbl.m_groups.Clear();
                listViewGroups.Clear(); //redundant?                
            }

            

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //forward to delete item
        private void toolstrip2ButtonDELETE_Click(object sender, EventArgs e)
        {
            List<int> indexes = new List<int>();
            foreach (ListViewItem item in listViewAccounts.SelectedItems)
            {
                indexes.Add(Convert.ToInt16(item.Name));
            }

            if (listViewAccounts.SelectedItems.Count == 0) return;
            secconbl.deleteItem(indexes);

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //context menu opening
        private void contextMenuStripentry_Opening(object sender, CancelEventArgs e)
        {
            ContextMenuStrip menu = sender as ContextMenuStrip;
            Control Sender = menu.SourceControl;

            if (Sender.Name == listViewAccounts.Name) this.contextMenuEnableItems(SelectedItemType.Item);
            if (Sender.Name == listViewGroups.Name) this.contextMenuEnableItems(SelectedItemType.Group);
        }

        //refresh list with new search string, handle watermark text
        private void toolStripSearch_TextChanged(object sender, EventArgs e)
        {
            if (toolStripSearch.Text == "Search...") return;
            if (toolStripSearch.Text.Contains("Search...")) toolStripSearch.Text = toolStripSearch.Text.Replace("Search...", "");
            toolStripSearch.Select(toolStripSearch.Text.Length, 0);
            if (toolStripSearch.TextLength == 0)
            {
                toolStripSearch.Text = "Search...";
                toolStripSearch.ForeColor = Color.Gray;
            }
            else toolStripSearch.ForeColor = Color.Black;

            //select the search results if it is not selected
            if(!listViewGroups.Items[2].Selected) listViewGroups.Items[2].Selected = true;
            
            this.showlist(listViewGroups.Items[2].Text);
        }

        //Search text box selection
        private void toolStripSearch_Click(object sender, EventArgs e)
        {
            if (toolStripSearch.Text == "Search...") toolStripSearch.SelectAll();
        }

        //define a new master key for the database
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            updateLog(5, "Going to change masterkey...", false, Color.Black, Color.Yellow);

            //ask for password
            secconbl.PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.Internal);
            if (!secconbl.UnlockDB(check: true, dontUseSessionPin: true)) return; //unlock db again, force asking for PW

            PWHandler pwm2 = new PWHandler();

            //ask for new masterkey and send it to a second pwhandler class
            enterpassword enternewpassword = new enterpassword(secconbl.Filename, verified: true);
            enternewpassword.ShowDialog();

            if (enternewpassword.DialogResult == DialogResult.Cancel)
            {
                updateLog(2, "Action cancelled, masterkey has not been changed.");
                return;
            }

            pwm2.UnlockPW(enternewpassword.password);

            //decrypt each entry with old pw and encrypt with new pw
            //don't keep deleted items
            Int16 countcleanedentries = 0;
            for (int i = secconbl.DBEntries.Count - 1; i >= 0; i--)
            {

                if (secconbl.DBEntries[i].GetDeletedFlag())
                {
                    countcleanedentries++;
                    secconbl.DBEntries.RemoveAt(i);
                }
            }

            //don't keep deleted groups
            for (int i = secconbl.DBGroups.Count - 1; i >= 0; i--)
            {

                if (secconbl.DBGroups[i].GetDeletedFlag())
                {
                    countcleanedentries++;
                    secconbl.DBGroups.RemoveAt(i);
                }
            }

            if (countcleanedentries == 1) updateLog(5, string.Format("MAINTENANCE: It's a good time to clean up. {0} unused item has been removed.", countcleanedentries), logonly: true);
            if (countcleanedentries > 1) updateLog(5, string.Format("MAINTENANCE: It's a good time to clean up. {0} unused items have been removed.", countcleanedentries), logonly: true);

            //decrypt and encrypt values with new master key, skip empty values since they belong to deleted items
            foreach (SecItem entry in secconbl.DBEntries)
            {
                if (entry.Username != "")
                    entry.Username = cryptoservice.encrypt(cryptoservice.decrypt(entry.Username, secconbl.PWManager), pwm2.CurrentPWKey(), pwm2.CurrentPWIV());
                if (entry.Password != "")
                    entry.Password = cryptoservice.encrypt(cryptoservice.decrypt(entry.Password, secconbl.PWManager), pwm2.CurrentPWKey(), pwm2.CurrentPWIV());
                if (entry.Password2 != "")
                    entry.Password2 = cryptoservice.encrypt(cryptoservice.decrypt(entry.Password2, secconbl.PWManager), pwm2.CurrentPWKey(), pwm2.CurrentPWIV());
                if (entry.Notes != "")
                    entry.Notes = cryptoservice.encrypt(cryptoservice.decrypt(entry.Notes, secconbl.PWManager), pwm2.CurrentPWKey(), pwm2.CurrentPWIV());
            }

            //lock both passwordhandler and unlock standard PWManager with new password
            pwm2.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);
            secconbl.PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.Internal);
            secconbl.PWManager.UnlockPW(enternewpassword.password);
            secconbl.Currenthash = secconbl.PWManager.PWHash;
            enternewpassword.deletepassword();
            enternewpassword.Dispose();

            secconbl.WatchChangesDB.Changed = true;

            secconbl.PWManager.SetPin = null;
            secconbl.Sessionpinactive = false;
            updateLog(5, "Masterkey changed. If a session pin was set it has been disabled for this session.", false, Color.Black, Color.Yellow);

            if (countcleanedentries > 0)
            {
                if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
                else this.showlist("*All");
            }
        }

        //forward to encryptfile with the file(s) given by drag and drop
        private void listViewAccounts_DragDrop(object sender, DragEventArgs e)
        {
            //get the list of files which are going to be encrypted
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            LockWorkSpace(true);
            secconbl.encryptfile(files, this.toolStripProgressBar1, imageList2);
            LockWorkSpace(false);

            if (secconbl.Filename != "")
            {
                if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
                else this.showlist("*All");
            }
        }

        //check if a file was dropped to the listview
        private void listViewAccounts_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Move;
            else e.Effect = DragDropEffects.None;
        }

        //forward to encrypt file
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendbname = new OpenFileDialog();
            opendbname.Multiselect = true;
            opendbname.AddExtension = true;
            //opendbname.Filter = "Seccon Database (*.sdb)|*.sdb";
            opendbname.DefaultExt = "SDB";

            if (opendbname.ShowDialog() != DialogResult.OK) return;

            string[] files = opendbname.FileNames;

            LockWorkSpace(true);
            secconbl.encryptfile(files, this.toolStripProgressBar1, imageList2);
            LockWorkSpace(false);

            if (secconbl.Filename != "")
            {
                if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
                else this.showlist("*All");
            }
        }

        //forward to decrypt file
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendbname = new OpenFileDialog();
            opendbname.Multiselect = true;
            opendbname.AddExtension = true;
            opendbname.Filter = "Seccon encrypted file (*.sef)|*.sef";
            opendbname.DefaultExt = "SEF";

            if (opendbname.ShowDialog() != DialogResult.OK) return;

            string[] files = opendbname.FileNames;

            secconbl.decryptfile(files, toolStripProgressBar1, openfile: false);
        }

        //refresh dropbox login
        private void loginToYourDropboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!secconbl.dropboxAuthentification()) return;
        }

        //close the database
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.closedb();
        }

        //Show or hide usernames
        private void checkBoxShowUsernames_Click(object sender, EventArgs e)
        {
            if (checkBoxShowUsernames.Checked) checkBoxShowUsernames.Checked = false;
            else checkBoxShowUsernames.Checked = true;

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");

            secconbl.ShowUserNameIsActive = checkBoxShowUsernames.Checked;
        }

        //Refresh list if option changed
        private void checkBoxHighlightAge_Click(object sender, EventArgs e)
        {
            if (checkBoxHighlightAge.Checked) checkBoxHighlightAge.Checked = false;
            else checkBoxHighlightAge.Checked = true;

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");

            secconbl.HighLightPasswordAge = checkBoxHighlightAge.Checked;
        }

        //Switch between details and tile view
        private void checkBoxDetailsView_Click(object sender, EventArgs e)
        {
            if (checkBoxDetailsView.Checked) checkBoxDetailsView.Checked = false;
            else checkBoxDetailsView.Checked = true;

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");

            secconbl.DetailsView = checkBoxDetailsView.Checked;
        }

        //close app
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //set obsolete value
        private void daysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            secconbl.obsolete = 30.0;
            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //set obsolete value
        private void daysToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            secconbl.obsolete = 90.0;
            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //set obsolete value
        private void daysToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            secconbl.obsolete = 180.0;
            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //set obsolete value
        private void daysToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            secconbl.obsolete = 360.0;
            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //set obsolete value
        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                secconbl.obsolete = Convert.ToDouble(toolStripTextBox1.Text);
                if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
                else this.showlist("*All");
            }
            catch { };
        }

        //click a link in the details field
        private void textBoxDetails_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(e.LinkText); }
            catch
            {
                this.updateLog(4, string.Format("Can not open the URL: {0}", e.LinkText));
            }
        }

        //open manage key dialog
        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            secconbl.PWManager.ResetTimeout();
            ManageKeys managekey = new ManageKeys(secconbl.DBKeys);
            managekey.ShowDialog();
            if (managekey.KeyListChanged) secconbl.WatchChangesDB.Changed = true;
        }

        //Export item unencrypted
        private void exportUnencryptedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewAccounts.SelectedItems.Count == 0)
            {
                this.updateLog(4, string.Format("No item selected."));
                return;
            }

            ArrayList indexes = new ArrayList();
            foreach (ListViewItem i in listViewAccounts.SelectedItems)
            {
                indexes.Add(Convert.ToInt16(i.Name));
            }

            secconbl.exportItem(indexes, SecconBL.exportItemsEncryption.None);
        }

        //Export item encrypted with symmetric file encryption
        private void exportEncryptedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewAccounts.SelectedItems.Count == 0)
            {
                this.updateLog(4, string.Format("No item selected."));
                return;
            }
            ArrayList indexes = new ArrayList();
            foreach (ListViewItem i in listViewAccounts.SelectedItems)
            {
                indexes.Add(Convert.ToInt16(i.Name));
            }
            secconbl.exportItem(indexes, SecconBL.exportItemsEncryption.AES);
        }

        //Export RSA encrypted by giving a public key
        private void exportRSAEncryptedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewAccounts.SelectedItems.Count == 0)
            {
                this.updateLog(4, string.Format("No item selected."));
                return;
            }
            ArrayList indexes = new ArrayList();
            foreach (ListViewItem i in listViewAccounts.SelectedItems)
            {
                indexes.Add(Convert.ToInt16(i.Name));
            }
            secconbl.exportItem(indexes, SecconBL.exportItemsEncryption.PGP);

        }

        //Download database from dropbox (forward to dropbox function)
        private void loadFromDropboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            secconbl.dropboxDownloadDB();
        }

        //Synchronize with dropbox (forward to dropbox function)
        private void synchronizeWithDropboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            secconbl.dropboxSynchronizeDB();

            //refresh the listview
            secconbl.m_groups.Clear();
            listViewGroups.Clear(); //redundant?

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //disconnect dropbox account - this just means to delete the user token / secretfile 
        private void disconnectDropboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ask if the account should really disconnected
            DialogResult deldbuserfile = MessageBox.Show("Would you really disconnect from your dropbox? You can reconnect to your dropbox later again.", "Disconnect from dropbox...", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            try
            {
                if (deldbuserfile == DialogResult.Yes) File.Delete(string.Format("dbuser_{0}", System.Security.Principal.WindowsIdentity.GetCurrent().Name.GetHashCode().ToString()));
                this.updateLog(2, string.Format("Dropbox connection information was deleted."));
            }
            catch
            {
                this.updateLog(4, string.Format("There was an error deleting the dropbox connection information."));
            }
        }

        //change sorting by clicking column header
        private void listViewAccounts_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                if (m_sorting == sortingOrderItems.AscName)
                {
                    m_sorting = sortingOrderItems.DesName;
                }
                else m_sorting = sortingOrderItems.AscName;
            }
            if (e.Column == 1)
            {
                if (m_sorting == sortingOrderItems.AscGroup)
                {
                    m_sorting = sortingOrderItems.DesGroup;
                }
                else m_sorting = sortingOrderItems.AscGroup;
            }
            if (e.Column == 5)
            {
                if (m_sorting == sortingOrderItems.AscLastModification)
                {
                    m_sorting = sortingOrderItems.DesLastModification;
                }
                else m_sorting = sortingOrderItems.AscLastModification;
            }

            //Do nothing if the column is not sortable
            if (e.Column == 2 || e.Column == 3 || e.Column == 4) return;

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //import items from exported and encrypted file
        private void importItemsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //check if database is unlocked
            if (!secconbl.UnlockDB(check: true)) return;

            //Select file to import
            OpenFileDialog opendbname = new OpenFileDialog();
            opendbname.AddExtension = true;
            opendbname.Filter = "Seccon encrypted file (*.sef)|*.sef";
            opendbname.DefaultExt = "SEF";

            if (opendbname.ShowDialog() != DialogResult.OK) return;

            string file = opendbname.FileName;

            //open file
            MemoryStream input = new MemoryStream();

            try
            {
                using (FileStream import = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    import.CopyTo(input);
                }
            }
            catch
            {
                this.updateLog(4, string.Format("There was an error opening the file \"" + file + "\"."));
                return;
            }

            string group = "";
            if (listViewGroups.SelectedItems.Count == 1 && !listViewGroups.SelectedItems[0].Text.Contains("*")) group = listViewGroups.SelectedItems[0].Text;

            secconbl.importItems(input, group, imageList2, file);
            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //copy the item to the clipboard
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewAccounts.SelectedItems.Count == 0)
            {
                this.updateLog(4, string.Format("No item selected."));
                return;
            }
            ArrayList indexes = new ArrayList();
            foreach (ListViewItem i in listViewAccounts.SelectedItems)
            {
                indexes.Add(Convert.ToInt16(i.Name));
            }
            secconbl.exportItem(indexes, SecconBL.exportItemsEncryption.AESClipboard);
        }

        //insert items from the clipboard
        private void paseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryStream clipboard = new MemoryStream();
            clipboard = (MemoryStream)Clipboard.GetData("SECCONITEMS");
            if (clipboard != null)
            {
                string group = "";
                if (listViewGroups.SelectedItems.Count == 1 && !listViewGroups.SelectedItems[0].Text.Contains("*")) group = listViewGroups.SelectedItems[0].Text;

                secconbl.importItems(clipboard, group, imageList2, fromclipboard: true);
                if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
                else this.showlist("*All");
            }
        }

        //Select all items when ctrl+a is pressed
        private void listViewAccounts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A && e.Control)
            {
                foreach (ListViewItem item in listViewAccounts.Items)
                {
                    item.Selected = true;
                }
            }

        }

        //Event after splitter was moved
        private void splitContainer2_SplitterMoved(object sender, SplitterEventArgs e)
        {
            scale.Refresh();
        }

        //Enabling context menu to set obsolete timespan
        private void contextMenuStripObsoleteSettings_EnabledChanged(object sender, EventArgs e)
        {
            foreach (ToolStripItem menuitem in contextMenuStripObsoleteSettings.Items)
            {
                menuitem.Enabled = contextMenuStripObsoleteSettings.Enabled;
            }

        }

        //Context menu was enabled / disabled
        private void contextMenuStripentry_EnabledChanged(object sender, EventArgs e)
        {
            //this.contextMenuEnableItems(SelectedItemType.Item);
        }

        //Enable / Disable context menu items when context menu is enabled / disabled
        private enum SelectedItemType { Item, Group, None }
        private void contextMenuEnableItems(SelectedItemType selectedItemType)
        {
            foreach (Object item in contextMenuStripentry.Items)
            {
                if (item.GetType() == typeof(ToolStripMenuItem))
                {
                    ToolStripMenuItem _item = item as ToolStripMenuItem;
                    if (secconbl.PWManager.Locked) _item.Enabled = false;
                    else _item.Enabled = contextMenuStripentry.Enabled;
                }
            }
            closeDatabaseToolStripMenuItem.Enabled = true;

            if (selectedItemType == SelectedItemType.Item)
            {
                editEntryToolStripMenuItem.Text = "Edit item...";
                if (listViewAccounts.SelectedItems.Count != 1) editEntryToolStripMenuItem.Enabled = false;
                addEntryToolStripMenuItem.Text = "Add item...";
                deleteEntryToolStripMenuItem.Text = "Delete item...";
                if (listViewAccounts.SelectedItems.Count != 1) deleteEntryToolStripMenuItem.Enabled = false;

                if (listViewAccounts.SelectedItems.Count != 1)
                {
                    copyPasswordToClipboardToolStripMenuItem.Enabled = false;
                    copyUsernameToClipboardToolStripMenuItem.Enabled = false;
                    browseURLToolStripMenuItem.Enabled = false;
                }
                if (listViewAccounts.SelectedItems.Count == 0)
                {
                    copyToolStripMenuItem.Enabled = false;
                }
                if (listViewAccounts.SelectedItems.Count > 0)
                {
                    deleteEntryToolStripMenuItem.Enabled = true;
                }
            }

            if (selectedItemType == SelectedItemType.Group)
            {
                editEntryToolStripMenuItem.Text = "Edit group...";
                if (listViewGroups.SelectedItems.Count != 1) editEntryToolStripMenuItem.Enabled = false;
                addEntryToolStripMenuItem.Text = "Add group...";
                deleteEntryToolStripMenuItem.Text = "Delete group...";
                if (listViewGroups.SelectedItems.Count != 1) deleteEntryToolStripMenuItem.Enabled = false;

                copyPasswordToClipboardToolStripMenuItem.Enabled = false;
                copyUsernameToClipboardToolStripMenuItem.Enabled = false;
                browseURLToolStripMenuItem.Enabled = false;
                copyToolStripMenuItem.Enabled = false;
                paseToolStripMenuItem.Enabled = false;

            }


        }

        //Before return from taskbar ask for pin if database is locked
        private void SECCONFORM_Resize(object sender, EventArgs e)
        {
            if (secconbl.Filename != "")
            {
                if (this.WindowState != FormWindowState.Minimized)
                {

                    //Check if database is unlocked
                    if (secconbl.PWManager.Locked)
                    {
                        this.Visible = false;
                        secconbl.UnlockDB(check: true);
                    }
                    if (secconbl.PWManager.Locked)
                    {
                        this.WindowState = FormWindowState.Minimized;
                    }
                    this.Visible = true;

                }
            }
        }

        //Add new group
        private void toolStripButtonAddGroup_Click(object sender, EventArgs e)
        {
            string selectedgroup = "";
            if (listViewGroups.SelectedItems.Count == 1 && !listViewGroups.SelectedItems[0].Text.Contains("*")) selectedgroup = listViewGroups.SelectedItems[0].Text;

            if (secconbl.Filename != "") secconbl.addEmptyItem(SecItem.ItemType.Group, secconbl.DBGroups, selectedgroup, imageList2);

            secconbl.m_groups.Clear();
            listViewGroups.Clear(); //redundant?

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //Delete group
        private void toolStripButtonDeleteGroup_Click(object sender, EventArgs e)
        {
            if (listViewGroups.SelectedItems.Count == 1) secconbl.EditGroup(listViewGroups.SelectedItems[0].Text, imageList2, true);

            secconbl.m_groups.Clear();
            listViewGroups.Clear(); //redundant?

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //Edit group definition
        private void toolStripButtonEditGroup_Click(object sender, EventArgs e)
        {
            if (listViewGroups.SelectedItems.Count == 1) secconbl.EditGroup(listViewGroups.SelectedItems[0].Text, imageList2);

            secconbl.m_groups.Clear();
            listViewGroups.Clear(); //redundant?

            if (listViewGroups.SelectedItems.Count == 1) this.showlist(listViewGroups.SelectedItems[0].Text);
            else this.showlist("*All");
        }

        //Move group up
        private void toolStripButtonMoveGroupUp_Click(object sender, EventArgs e)
        {
            if (listViewGroups.SelectedItems.Count == 1) this.MoveGroup(listViewGroups.SelectedItems[0].Text, SecconBL.MoveDirection.Up);
        }

        //Move group down
        private void toolStripButtonMoveGroupDown_Click(object sender, EventArgs e)
        {
            if (listViewGroups.SelectedItems.Count == 1) this.MoveGroup(listViewGroups.SelectedItems[0].Text, SecconBL.MoveDirection.Down);
        }

        //Move group
        private void MoveGroup(String groupname, SecconBL.MoveDirection movedirection)
        {
            secconbl.MoveGroup(listViewGroups.SelectedItems[0].Text, movedirection);

            secconbl.m_groups.Clear();
            listViewGroups.Clear(); 

            this.showlist(groupname);

            //select the group in the listview
            foreach (ListViewItem group in listViewGroups.Items)
            {
                if (group.Text == groupname) group.Selected = true;
            }
        }
        #endregion

        #region BLEvents
        //The PWHandler Event: PW is locked / unlocked
        public void PWHandler_Event(object sender, PWHandler.PWHandlerEventArgs e)
        {
            //PW is locked
            if (e.Event == PWHandler.PWHandlerEventArgs.PWHandlerEventEnum.Lock)
            {
                if (secconbl.DBEntries != null)
                {
                    if (e.Reason == PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.Timeout)
                        this.updateLog(2, "Database is locked due to timeout.");
                    else if (e.Reason == PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.PinInvalidated)
                    {
                        secconbl.Sessionpinactive = false;
                        this.updateLog(4, "Wrong pin was entered 3 times. Pin has been invalidated for this session.");
                    }
                    else this.updateLog(2, "Database is locked.");

                    if (!secconbl.Sessionpinactive)
                    {
                        toolStripLockState.Image = imageListToolStrip.Images[0]; //red lock icon to indicate stron master key protection
                    }
                    else
                    {
                        toolStripLockState.Image = imageListToolStrip.Images[6]; //Yellow lock icon to indicate weak protection
                        toolStripOwner.Text += " Keep in mind that your database is protected by a weak session pin only!";
                        toolStripOwner.Image = imageListToolStrip.Images[3];
                    }

                    this.WindowState = FormWindowState.Minimized;
                }
                else
                {
                    this.updateLog(2, "No database opened.");
                    toolStripLockState.Image = imageListToolStrip.Images[0];
                }
                textBoxDetails.Text = "";
            }
            if (e.Event == PWHandler.PWHandlerEventArgs.PWHandlerEventEnum.WrongPW) secconbl.Sessionpinactive = false;

            //PW is unlocked
            if (e.Event == PWHandler.PWHandlerEventArgs.PWHandlerEventEnum.UnlockByKey)
            {
                this.updateLog(2, "Check masterkey and unlock database...");
                toolStripLockState.Image = imageListToolStrip.Images[1];

            }
            if (e.Event == PWHandler.PWHandlerEventArgs.PWHandlerEventEnum.UnlockByPin)
            {
                this.updateLog(2, "Database is unlocked by session pin.");
                toolStripLockState.Image = imageListToolStrip.Images[1];

            }

            //SESSION PIN ENABLED
            if (e.Reason == PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.PinActivated && e.PinState == true) this.updateLog(2, "A session pin has been enabled for this session.");

            //PIN
            if (e.PinState == true)
            {
                toolStripStatusLabelPIN.Text = "PIN";
                toolStripButtonLock.Image = imageListToolStrip.Images[6];
                lockToolStripMenuItem.Image = imageListToolStrip.Images[6];
                lockDatabaseToolStripMenuItem.Image = imageListToolStrip.Images[6];
            }
            else
            {
                toolStripStatusLabelPIN.Text = "";
                toolStripButtonLock.Image = imageListToolStrip.Images[0];
                lockToolStripMenuItem.Image = imageListToolStrip.Images[0];
                lockDatabaseToolStripMenuItem.Image = imageListToolStrip.Images[0];
            }
        }

        //the event that something changed in the database
        public void DBChanged_Event(object sender, EventArgs e)
        {
            if (secconbl.WatchChangesDB.Changed) toolStripunsaved.Text = "There are unsaved changes";
            else toolStripunsaved.Text = "";
        }

        //clipboard deletion timer
        public void killclipboard_tick(object sender, EventArgs e)
        {
            if (secconbl.Killclipboardtimerexpired <= 0)
            {                
                this.updateLog(2, "The content of the clipboard was deleted.");
                toolStripProgressBar1.Visible = false;
            }
            else
            {
                toolStripProgressBar1.Value = secconbl.Killclipboardtimerexpired;
            }
        }









        #endregion

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            AboutDlg about = new AboutDlg();
            about.ShowDialog();
        }
    }
}
