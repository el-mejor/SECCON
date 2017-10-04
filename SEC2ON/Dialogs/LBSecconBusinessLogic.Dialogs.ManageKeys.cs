using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic.Dialogs
{
    public partial class ManageKeys : Form
    {
        private string m_selectedKeyID = "";
        private List<XmlElement> m_dbkeys = new List<XmlElement>();
        private bool m_askforkey = false;
        private bool m_changed = false;

        public bool KeyListChanged
        {
            get { return m_changed; }
        }

        public String SelectedKeyID
        {
            get { return m_selectedKeyID; }
        }

        public ManageKeys(List<XmlElement> DBKeys, bool askforkey = false)
        {
            InitializeComponent();
            m_dbkeys = DBKeys;

            if (askforkey)
            {
                label10.Text = "Select a PGP key to encrypt with.";

                button1.Text = "Use key";
                button2.Visible = true;
                m_askforkey = true;
                button3.Visible = false;
                button4.Visible = false;
            }
            else
            {
                label10.Text = "Manage your PGP keys.";
            }

        }

        private void ManageKeys_Load(object sender, EventArgs e)
        {
            this.refreshlist();
        }

        //refresh the key list
        private void refreshlist()
        {
            listViewKeys.Clear();
            listViewKeys.Columns.Add("Key ID");
            listViewKeys.Columns.Add("Owner");
            listViewKeys.Columns.Add("Valid from");
            listViewKeys.Columns.Add("Valid thru");

            int i = 0;
            int keyindex = 0;
            foreach (XmlElement key in m_dbkeys)
            {
                if (key.GetAttributeNode("deleted") != null)
                {
                    if (key.GetAttributeNode("deleted").Value == "true")
                    {
                        keyindex++;
                        continue;
                    }
                }

                listViewKeys.Items.Add(key.GetAttributeNode("ID").Value);

                listViewKeys.Items[i].Name = keyindex.ToString();
                listViewKeys.Items[i].ImageIndex = 0;
                //byte[] keyinfo = System.Text.Encoding.UTF8.GetBytes(key.InnerText);
                try
                {
                    using (MemoryStream keyin = new MemoryStream(ASCIIEncoding.UTF8.GetBytes(key.InnerText)))
                    {

                        using (Stream inputStream = PgpUtilities.GetDecoderStream(keyin))
                        {
                            PgpPublicKeyRingBundle publicKeyRingBundle = new PgpPublicKeyRingBundle(inputStream);
                            foreach (PgpPublicKeyRing kRing in publicKeyRingBundle.GetKeyRings())
                            {
                                PgpPublicKey pubkey = kRing.GetPublicKey();

                                if (pubkey != null)
                                {
                                    IEnumerable ids = pubkey.GetUserIds();

                                    foreach (string userid in ids)
                                    {
                                        listViewKeys.Items[i].SubItems.Add(userid);
                                        break;
                                    }

                                    DateTime validthru = new DateTime();
                                    DateTime validfrom = pubkey.CreationTime;

                                    listViewKeys.Items[i].SubItems.Add(validfrom.ToString() + " [UTC]");

                                    if (pubkey.ValidDays > 0) 
                                    {
                                        validthru = pubkey.CreationTime.AddDays(pubkey.ValidDays);
                                        
                                        listViewKeys.Items[i].SubItems.Add(validthru.ToString() + " [UTC]");
                                        if (DateTime.Compare(DateTime.UtcNow, validthru) > 0)
                                        {
                                            listViewKeys.Items[i].BackColor = Color.FromArgb(255, 128, 128);
                                            listViewKeys.Items[i].ImageIndex = 1;
                                        }
                                    }
                                    else
                                    {
                                        listViewKeys.Items[i].SubItems.Add("--.--.---- --:--:-- [UTC]");
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    listViewKeys.Items[i].Remove();
                    key.SetAttribute("deleted", "true");
                }
                i++;
                keyindex++;
            }
            listViewKeys.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        //Close and use the selected key if needed
        private void button1_Click(object sender, EventArgs e)
        {
            if (listViewKeys.SelectedItems.Count != 1 && m_askforkey)
            {
                DialogResult nokey = MessageBox.Show("Please select a key.", "Select key for encrypting...", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                if (nokey == DialogResult.OK) return;
                else
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }
            }
            if (m_askforkey && listViewKeys.SelectedItems[0].ImageIndex == 1)
            {
                DialogResult obsoletekey = MessageBox.Show("The selected key is out of date. Would you select another key?", "Select key for encrypting...", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (obsoletekey == DialogResult.Yes) return;
                if (obsoletekey == DialogResult.Cancel)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }
            }
            if(m_askforkey) m_selectedKeyID = listViewKeys.SelectedItems[0].Text;
            this.DialogResult = DialogResult.OK; ;
            this.Close();
        }

        //Close and cancel
        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        //Add new key, select keyfile and proceed to add key function
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog opendbname = new OpenFileDialog();
            opendbname.AddExtension = true;
            opendbname.Filter = "Open PGP Keyfile (*.asc)|*.asc|All files (*.*)|*.*";
            opendbname.DefaultExt = "asc";

            if (opendbname.ShowDialog() != DialogResult.OK) return;

            string filename = opendbname.FileName;

            addkeyfile(filename);
            this.refreshlist();

            m_changed = true;
        }

        //delete the selected key
        private void button4_Click(object sender, EventArgs e)
        {
            if (listViewKeys.SelectedItems.Count != 1) return;

            DialogResult removekey = MessageBox.Show("Would you really delete the selected key from your database?", "Remove key...", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (removekey == DialogResult.No) return;

            //m_dbkeys.RemoveAt(Convert.ToInt16(listViewKeys.SelectedItems[0].Name));

            XmlElement key = (XmlElement)m_dbkeys[Convert.ToInt16(listViewKeys.SelectedItems[0].Name)];
            key.SetAttribute("deleted", "true");
            key.GetAttributeNode("latest").Value = DateTime.UtcNow.ToString();
            key.InnerText = "";
            this.refreshlist();

            m_changed = true;
        }

        //Add key
        private void addkeyfile(string filename)
        {
            XmlDocument newelement = new XmlDocument();

            XmlElement newentry = newelement.CreateElement("key");

            string keystring = "";
            using (StreamReader dummy = new StreamReader(filename))
            {
                keystring = dummy.ReadToEnd();

                MemoryStream keyin = new MemoryStream(ASCIIEncoding.UTF8.GetBytes(keystring));

                using (Stream inputStream = PgpUtilities.GetDecoderStream(keyin))
                {
                    PgpPublicKeyRingBundle publicKeyRingBundle = new PgpPublicKeyRingBundle(inputStream);
                    foreach (PgpPublicKeyRing kRing in publicKeyRingBundle.GetKeyRings())
                    {
                        PgpPublicKey pubkey = kRing.GetPublicKey();

                        if (pubkey != null)
                        {
                            newentry.SetAttribute("ID", pubkey.KeyId.ToString("X"));
                            newentry.SetAttribute("latest", DateTime.UtcNow.ToString());
                        }
                    }
                }
            }
            newentry.InnerText = keystring;
            m_dbkeys.Add(newentry);
        }

    }
}
