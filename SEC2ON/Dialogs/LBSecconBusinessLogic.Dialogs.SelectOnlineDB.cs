using SEC2ON.LBSecconBusinessLogic.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class SelectOnlineDB : Form
    {
        List<OnlineDatabase> m_onlinedbs = new List<OnlineDatabase>();
        String m_selectedDB = null;
        enum SortOnlineDatabases { byName, byLastModification };
        SortOnlineDatabases m_sortOnlineDatabases = SortOnlineDatabases.byName;

        public String SelectedDB
        { get { return m_selectedDB; } }

        public SelectOnlineDB(List<OnlineDatabase> onlineDBs)
        {
            InitializeComponent();
            m_onlinedbs = onlineDBs;

            label10.Text = "Select a dropbox database to download...";

            //Show online databases
            this.refreshList();

            //if the dialog is closed for any reason except hitting the download button it was cancelled.
            this.DialogResult = DialogResult.Cancel; 
        }

        //refresh list of online databases
        private void refreshList()
        {
            listViewDBs.Clear();

            //Add columns
            listViewDBs.Columns.Add("File name");
            listViewDBs.Columns.Add("Last Modification");
            listViewDBs.Columns.Add("Size");

            //Sort databases
            OnlineDatabase.sortOnlineDatabaseName sortbyName = new OnlineDatabase.sortOnlineDatabaseName();
            OnlineDatabase.sortOnlineDatabaseLastModification sortbyLastMod = new OnlineDatabase.sortOnlineDatabaseLastModification();

            if (m_sortOnlineDatabases == SortOnlineDatabases.byName)
            {
                listViewDBs.Columns[0].Text = "<> " + listViewDBs.Columns[0].Text;
                m_onlinedbs.Sort(sortbyName);
            }
            if (m_sortOnlineDatabases == SortOnlineDatabases.byLastModification)
            {
                listViewDBs.Columns[1].Text = "<> " + listViewDBs.Columns[1].Text;
                m_onlinedbs.Sort(sortbyLastMod);
            }

            int i = 0;
            foreach (OnlineDatabase onlineDB in m_onlinedbs)
            {
                //make a difference between database, database backup and other files.
                //databases are shown always
                if (onlineDB.Type == OnlineDatabase.FileType.Database)
                {
                    listViewDBs.Items.Add(onlineDB.Name);
                    listViewDBs.Items[i].Name = onlineDB.Name;
                    listViewDBs.Items[i].SubItems.Add(onlineDB.LastModified.ToString());
                    listViewDBs.Items[i].SubItems.Add(onlineDB.Size);
                    listViewDBs.Items[i].ImageIndex = 0;
                    i++;
                }
                //database backups are shown if "show all files" is checked - they got the database icon but they are written in a grey color
                else if (checkBoxShowUnknown.Checked && onlineDB.Type == OnlineDatabase.FileType.DatabaseBackup)
                {
                    listViewDBs.Items.Add(onlineDB.Name);
                    listViewDBs.Items[i].Name = onlineDB.Name;
                    listViewDBs.Items[i].SubItems.Add(onlineDB.LastModified.ToString());
                    listViewDBs.Items[i].SubItems.Add(onlineDB.Size);
                    listViewDBs.Items[i].ForeColor = Color.DarkGray;
                    listViewDBs.Items[i].ImageIndex = 0;
                    i++;
                }
                //all other files are shown if "show all files" is checked - they are written in a grey color 
                else if (checkBoxShowUnknown.Checked && onlineDB.Type == OnlineDatabase.FileType.Unknown)
                {
                    listViewDBs.Items.Add(onlineDB.Name);
                    listViewDBs.Items[i].Name = onlineDB.Name;
                    listViewDBs.Items[i].SubItems.Add(onlineDB.LastModified.ToString());
                    listViewDBs.Items[i].SubItems.Add(onlineDB.Size);
                    listViewDBs.Items[i].ForeColor = Color.DarkGray;
                    i++;
                }
            }

            listViewDBs.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            return;
        }

        //Download button was hit
        private void button1_Click(object sender, EventArgs e)
        {
            if (listViewDBs.SelectedItems.Count == 0) return;

            m_selectedDB = listViewDBs.SelectedItems[0].Name;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        //determine if the download button is enabled or not
        private void listViewDBs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewDBs.SelectedItems.Count == 1) button1.Enabled = true;
            else button1.Enabled = false;
        }

        //Cancel the dialog
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //Load Event
        private void SelectOnlineDB_Load(object sender, EventArgs e)
        {
            
        }

        //Change sorting by clicking the column headers
        private void listViewDBs_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                m_sortOnlineDatabases = SortOnlineDatabases.byName;
                this.refreshList();
            }

            if (e.Column == 1)
            {
                m_sortOnlineDatabases = SortOnlineDatabases.byLastModification;
                this.refreshList();
            }
        }

        //Show all files checked/unchecked
        private void checkBoxShowUnknown_CheckedChanged(object sender, EventArgs e)
        {
            this.refreshList();
        }
    }
}
