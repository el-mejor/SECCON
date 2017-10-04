using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Security;
using System.IO;
using System.Xml;
using System.Collections;

using DropNet;
using SEC2ON.LBSecconBusinessLogic.Dialogs.Controlls;

using LBPasswordAndCryptoServices;

namespace SEC2ON.LBSecconBusinessLogic.Classes
{
    //Selftest of the encryption and decryption functions
    #region selftest
    //selftest
    public static class selftest
    {
        public static String Run()
        {
            try
            {
                //target file for selftest                                
                String filename = Path.Combine(Application.LocalUserAppDataPath, "selftest" + System.Diagnostics.Process.GetCurrentProcess().Id);                

                //use a predefined password
                byte[] password = Encoding.ASCII.GetBytes("selftest");

                //use a predefined string
                String phrase = "The quick brown fox jumps over the lazy dog!";

                //open pw handler
                PWHandler pwh = new PWHandler(180000);
                pwh.UnlockPW(password);

                //encrypt string
                String encphrase = cryptoservice.encrypt(Encoding.Unicode.GetBytes(phrase), pwh);

                //check encrypted string here
                if (encphrase != "UGc2DbnBC3Glzzoof1y4qmpvWVAz+rLulvChNWT0U+Jt3TpJ8nUBFT0YdoBd0l98moYkCVTNOg6ZYIY19c3G650uqGnBqRLNQvm/nMrkR5uGwee9AdR9FcLn3hmqyNb3")
                    return "Selftest: Encryption of a String failed!"; //encryption of string failed

                MemoryStream f = new MemoryStream();

                f.Write(Encoding.UTF8.GetBytes(encphrase), 0, Encoding.UTF8.GetBytes(encphrase).Length);

                //encrypt file
                using (FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    byte[] output = cryptoservice.fileencrypt(f, pwh.CurrentPWKey(high: true), pwh.CurrentPWIV(high: true)).ToArray();
                    file.Write(output, 0, output.Length);
                }

                //decrypt file
                MemoryStream g = new MemoryStream();
                using (FileStream file2 = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    file2.CopyTo(g);
                }

                //check encrypted file
                string comp = "ZBV3M9YucB8P9TLaWlbZbJoLlhWp0cjJMK4DfJzGJwXA6a+WWhhaO1fEkAXp3w8dbfP0mOzH7DeZKEOFFCxXN1mb+WeUfYlqKHKuMI7mHoEQlYGdlXXEkS0X+g16n2Ibh5HSaiX5pmBSSTswRO12cCTRSr9nWwa/H1Bv4205quxFkip0MqmIz9br0V0jDt+i";

                if (Convert.ToBase64String(g.ToArray()) != comp)
                    return "Selftest: Encryption of a file failed!"; //encryption of file failed

                //decrypt file
                String decfile;
                try
                {
                    decfile = Encoding.UTF8.GetString(cryptoservice.filedecrypt(g, pwh.CurrentPWKey(high: true), pwh.CurrentPWIV(high: true)).ToArray());
                    if (decfile != encphrase)
                        return "Selftest: Decryption of a file failed!";
                }
                catch
                {
                    return "Selftest: Decryption of a file failed!";
                }

                //decrypt string
                try
                {
                    String decstring = Encoding.Unicode.GetString(cryptoservice.decrypt(decfile, pwh));

                    if (decstring != phrase)
                        return "Selftest: Decryption of a string failed!";
                }
                catch
                {
                    return "Selftest: Decryption of a string failed!";
                }

                if (File.Exists(filename))
                    File.Delete(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return "Selftest: Something went wrong generally!";
            }

            return "Selftest: PASSED";
        }
    }
    #endregion

    //database changes are recognized here
    #region WatchChanges
    //if there are changes made to the database call an event
    public class WatchChanges
    {
        private bool m_changed = false;

        public delegate void Databasechanged_Handler(object sender, EventArgs e);

        public bool Changed
        {
            get { return m_changed; }
            set
            {
                m_changed = value;
                this.m_Databasechanged();
            }
        }

        public event Databasechanged_Handler DatabaseChanged;
        public void m_Databasechanged()
        {
            if (this.DatabaseChanged != null) this.DatabaseChanged.Invoke(this, null);
        }
    }
    #endregion

    //handling the XML of a database
    #region Databasehandling / XML Source    
    //create and open databases and add new items to the database. extending the xmldocument class
    public class SecDB: XmlDocument
    {
        private List<SecItem> m_dbentries = new List<SecItem>();
        private List<XmlElement> m_keys = new List<XmlElement>();
        private bool m_showusernamesisactive = false;
        private bool m_highlightpasswordsage = false;
        public Int16 m_markAsObsolete = 365;
        private bool m_detailsview = true;
        private bool m_allowsessionpin = false;
        private DateTime m_lastsync = DateTime.MinValue;
        private Int16 m_updated = 0; //count updated entries

        public List<SecItem> DBEntries
        { get { return m_dbentries; } }
        public List<XmlElement> DBKeys
        { get { return m_keys; } }

        public bool ShowUserNameIsActive
        {
            get { return m_showusernamesisactive; }
            set { m_showusernamesisactive = value; }
        }
        public bool HighLightPasswordsAge
        {
            get { return m_highlightpasswordsage; }
            set { m_highlightpasswordsage = value; }
        }
        public double MarkAsObsolete
        {
            get { return Convert.ToDouble(m_markAsObsolete); }
            set { m_markAsObsolete = Convert.ToInt16(value); }
        }
        public bool Detailsview
        {
            get { return m_detailsview; }
            set { m_detailsview = value; }
        }
        public bool AllowSessionPin
        {
            get { return m_allowsessionpin; }
            set { m_allowsessionpin = value; }
        }

        public DateTime LastSync
        {
            get { return m_lastsync; }
            set { m_lastsync = value; }
        }

        public Int16 UpdatedItems
        {
            get { return m_updated; }
        }

        public SecDB()
        {
            
        }

        //Create a new empty  database containing a random string at the beginning (avoid known text attacks since the header is the same in each seccon xml) and create
        //the hash tag toverify the password and the entries tag for the items.
        public void CreateNewDatabase()
        {
            this.PrependChild(this.CreateElement("seccondb"));

            //Add settings tags
            XmlElement Settings = this.CreateElement("settings");
            this.DocumentElement.AppendChild(Settings);

            //Add showusernameisactive tag
            AddSettingstag("showusernameisactive", m_showusernamesisactive);

            //Add highlightpasswordsage tag
            AddSettingstag("highlightpasswordsage", m_highlightpasswordsage);

            //Add mark as obsolete value
            AddSettingstag("markasobsolete", Convert.ToString(m_markAsObsolete));

            //Add detailsview tag
            AddSettingstag("detailsview", m_detailsview);

            //Add detailsview tag
            AddSettingstag("allowsessionpin", m_allowsessionpin);

            //Add LastSync tag
            AddSettingstag("lastsync", m_lastsync.ToString());

            //Add database tag
            XmlElement entries = this.CreateElement("entries");
            this.DocumentElement.AppendChild(entries);

            //Add keys tag
            XmlElement keys = this.CreateElement("keys");
            this.DocumentElement.AppendChild(keys);            
        }

        //open a database and returning the password hash for verification, copy the items as XMLElement to the DBEntries List
        public void OpenDatabase()
        {   
            //load settings and information
            if (LoadSettingstag("showusernameisactive") == "true") m_showusernamesisactive = true;
            else m_showusernamesisactive = false;

            if (LoadSettingstag("highlightpasswordsage") == "true") m_highlightpasswordsage = true;
            else m_highlightpasswordsage = false;

            if (LoadSettingstag("markasobsolete") != null) m_markAsObsolete = Convert.ToInt16(LoadSettingstag("markasobsolete"));
            else m_markAsObsolete = 360;

            if (LoadSettingstag("detailsview") == "false") m_detailsview = false;
            else m_detailsview = true;

            if (LoadSettingstag("allowsessionpin") == "true") m_allowsessionpin = true;
            else m_allowsessionpin = false;

            if (LoadSettingstag("lastsync") != null) m_lastsync = Convert.ToDateTime(LoadSettingstag("lastsync"));
            else m_lastsync = DateTime.MinValue;

            //generate list of entries
            foreach (XmlElement entry in this.SelectNodes("seccondb/entries/entry"))
            {   
                //add entry                

                SecItem newitem = new SecItem(entry);
                
                m_updated += newitem.Updated;

                m_dbentries.Add(newitem);
            }

            //generate list of keys
            foreach (XmlElement entry in this.SelectNodes("seccondb/keys/key"))
            {
                m_keys.Add(entry);
            }

            return;           
        }

        //Add settings tags to xml
        public void AddSettingstag(string name, bool value)
        {
            if (value) AddSettingstag(name, "true");
            else AddSettingstag(name, "false");
        }
        public void AddSettingstag(string name, string value)
        {
            XmlElement addtag = this.CreateElement(name);
            addtag.InnerText = value;
            this.SelectSingleNode("seccondb/settings").AppendChild(addtag);
        }

        //Load settingstags from xml
        public string LoadSettingstag(string name)
        {
            if (this.SelectSingleNode("seccondb/settings/" + name) == null) return null;
            return this.SelectSingleNode("seccondb/settings/" + name).InnerText;
        }



       
    }
    #endregion

    //handling the items of the database
    #region Seccon Items handling
    public class SecItem
    {
        #region fields

        public enum ItemType { Item, Group };

        public XmlElement XML 
        { 
            get { return m_XML; }             
        }
        public Int16 Updated { get; set; }
        public String Group
        {
            get
            {
                return m_XML.GetAttributeNode("group").Value;
            }
            set
            {
                m_XML.GetAttributeNode("group").Value = value;
            }
        }
        public String Name
        {
            get 
            {
                return m_XML.GetAttributeNode("name").Value;
            }
            set
            {
                m_XML.GetAttributeNode("name").Value = value;
            }
        }
        public Int16 ImageIndex
        {
            get
            {
                return Convert.ToInt16(m_XML.GetAttributeNode("image").Value); 
            }
            set
            {
                m_XML.GetAttributeNode("image").Value = value.ToString();
            }
        }
        public String Url
        {
            get
            {
                return m_XML.SelectSingleNode("url").InnerText;
            }
            set
            {
                m_XML.SelectSingleNode("url").InnerText = value;
            }
        }
        public String Username
        {
            get
            {
                return m_XML.SelectSingleNode("username").InnerText;
            }
            set
            {
                m_XML.SelectSingleNode("username").InnerText = value;
            }
        }
        public String Password
        {
            get
            {
                return m_XML.SelectSingleNode("password1").InnerText;
            }
            set
            {
                m_XML.SelectSingleNode("password1").InnerText = value;
            }
        }
        public String Password2
        {
            get
            {
                return m_XML.SelectSingleNode("password2").InnerText;
            }
            set
            {
                m_XML.SelectSingleNode("password2").InnerText = value;
            }
        }
        public String Notes
        {
            get
            {
                return m_XML.SelectSingleNode("notes").InnerText;
            }
            set
            {
                m_XML.SelectSingleNode("notes").InnerText = value;
            }
        }
        public DateTime ExpirationDate
        {
            get
            {
                return Convert.ToDateTime(XML.SelectSingleNode("expirationdate").InnerText);
            }
            set
            {
                m_XML.SelectSingleNode("expirationdate").InnerText = value.ToString();
            }
        }
        public DateTime LastModification
        {
            get
            {
                return Convert.ToDateTime(XML.SelectSingleNode("lastmodified").InnerText);
            }
            set
            {
                m_XML.SelectSingleNode("lastmodified").InnerText = value.ToString();
            }
        }
        public DateTime Latest
        {
            get
            {
                return Convert.ToDateTime(XML.SelectSingleNode("latest").InnerText);
            }
            set
            {
                m_XML.SelectSingleNode("latest").InnerText = value.ToString();
            }
        }

        #endregion

        #region members        
        private XmlDocument m_xmldocu  = new XmlDocument();
        private XmlElement m_XML;
        #endregion

        //create a empty item
        public SecItem()
        {
            XmlElement newelement = m_xmldocu.CreateElement("entry");
            newelement.SetAttribute("name", "");
            newelement.SetAttribute("group", "");
            newelement.SetAttribute("image", "0");

            this.AddUsername(newelement, "");
            this.AddUrl(newelement, "");
            this.AddPassword1(newelement, "");
            this.AddPassword2(newelement, "");
            this.AddNotes(newelement, "");
            this.AddLastModified(newelement);
            this.AddLatest(newelement);
            this.AddExpirationDate(newelement, DateTime.Now.AddYears(9999 - DateTime.Now.Year));

            m_XML = newelement;
        }

        //create item from xml
        public SecItem(XmlElement entry)
        {
            m_XML = entry;
            this.UpdateItem();
        }

        //create new item
        public SecItem(string name, string group, int imageindex, string url,
            string username,
            string password1,
            string password2,
            string notes,
            DateTime expirationdate)
        {
            XmlElement newelement = m_xmldocu.CreateElement("entry");
            newelement.SetAttribute("name", name);
            newelement.SetAttribute("group", group);
            newelement.SetAttribute("image", imageindex.ToString());

            this.AddUsername(newelement, username);
            this.AddUrl(newelement, url);
            this.AddPassword1(newelement, password1);
            this.AddPassword2(newelement, password2);
            this.AddNotes(newelement, notes);
            this.AddLastModified(newelement);
            this.AddLatest(newelement);
            this.AddExpirationDate(newelement, expirationdate);

            m_XML = newelement;
        }

        //delete item
        public void DeleteItem()
        {
            this.SetDeletedFlag();
            this.Group = "";
            this.ImageIndex = 0;
            this.Url = "";
            this.Username = "";
            this.Password = "";
            this.Notes = "";
            this.Latest = DateTime.UtcNow;
        }
        
        //update items
        private void UpdateItem()
        {
            //update: check if expiration date tag exists
            if (m_XML.SelectSingleNode("expirationdate") == null)
                {
                    DateTime addexpirydate = DateTime.Now;
                    addexpirydate = addexpirydate.AddYears(9999 - addexpirydate.Year);
                    this.AddExpirationDate(m_XML, addexpirydate);

                    Updated++;
                }

                //update: add datetime information to deleted entries. this will prevent the database from wrong sorting.
                if (m_XML.SelectSingleNode("lastmodified").InnerText == "")
                {
                    m_XML.SelectSingleNode("lastmodified").InnerText = DateTime.UtcNow.ToString();

                    Updated++;
                }
        }

        #region Flags
        //if (entry.GetAttributeNode("groupdefinitionflag") != null && entry.GetAttributeNode("deleted") == null)
        //groupdefinitionflag
        public bool GetGroupDefinitionFlag()
        {
            if (XML.GetAttributeNode("groupdefinitionflag") == null) return false;
            else return true;
        }
        public void SetGroupDefinitionFlag()
        {
            XML.SetAttribute("groupdefinitionflag", "true");
        }

        //deleted flag
        public bool GetDeletedFlag()
        {
            if (XML.GetAttributeNode("deleted") == null) return false;
            else return true;
        }
        public void SetDeletedFlag()
        {
            XML.SetAttribute("deleted", "true");
        }
        public void RemoveDeletedFlag()
        {
            XML.SetAttribute("deleted", "false");
        }

        #endregion

        #region add tags
        //Add xml tag to entry
        private void AddUsername(XmlElement item, String username)
        {
            XmlElement addusername = m_xmldocu.CreateElement("username");
            addusername.InnerText = username;
            item.AppendChild(addusername);
            
        }

        //Add xml tag to entry
        private void AddUrl(XmlElement item, String url)
        {
            XmlElement addurl = m_xmldocu.CreateElement("url");
            addurl.InnerText = url;
            item.AppendChild(addurl);
            
        }

        //Add xml tag to entry
        private void AddPassword1(XmlElement item, String password1)
        {
            XmlElement addpassword1 = m_xmldocu.CreateElement("password1");
            addpassword1.InnerText = password1;
            item.AppendChild(addpassword1);
            
        }

        //Add xml tag to entry
        private void AddPassword2(XmlElement item, String password2)
        {
            XmlElement addpassword2 = m_xmldocu.CreateElement("password2");
            addpassword2.InnerText = password2;
            item.AppendChild(addpassword2);
            
        }

        //Add xml tag to entry
        private void AddNotes(XmlElement item, String notes)
        {
            XmlElement addnotes = m_xmldocu.CreateElement("notes");
            addnotes.InnerText = notes;
            item.AppendChild(addnotes);
            
        }

        //Add xml tag to entry
        private void AddLastModified(XmlElement item)
        {
            XmlElement addlastmodified = m_xmldocu.CreateElement("lastmodified");
            addlastmodified.InnerText = Convert.ToString(DateTime.Now);
            item.AppendChild(addlastmodified);
            
        }

        //Add xml tag to entry
        private void AddLatest(XmlElement item)
        {
            XmlElement latest = m_xmldocu.CreateElement("latest");
            latest.InnerText = Convert.ToString(DateTime.Now);
            item.AppendChild(latest);
            
        }

        //Add xml tag to entry
        private void AddExpirationDate(XmlElement item, DateTime expirationdate)
        {
            XmlElement addexpirationdate = m_xmldocu.CreateElement("expirationdate");
            addexpirationdate.InnerText = expirationdate.ToUniversalTime().ToString();
            item.AppendChild(addexpirationdate);
            
        }
        #endregion
    }
    #endregion

    //Class to transport information of available databases (dropbox) to the select online db dialog
    #region OnlineDatabase 
    public class OnlineDatabase
    {
        //Comparer:
        //Sort online databases by name
        public class sortOnlineDatabaseLastModification : IComparer<OnlineDatabase>
        {
            int IComparer<OnlineDatabase>.Compare(OnlineDatabase x, OnlineDatabase y)
            {
                return DateTime.Compare(y.LastModified, x.LastModified);
            }
        }
        //Sort online databases by name
        public class sortOnlineDatabaseName : IComparer<OnlineDatabase>
        {
            int IComparer<OnlineDatabase>.Compare(OnlineDatabase y, OnlineDatabase x)
            {
                return String.Compare(y.Name, x.Name);
            }
        }

        String m_name = "";
        DateTime m_lastmodified = DateTime.Now;
        public enum FileType { Database, DatabaseBackup, Unknown };
        FileType m_type = FileType.Unknown;
        String m_size = "";
        
        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public DateTime LastModified
        {
            get { return m_lastmodified; }
            set { m_lastmodified = value; }
        }
        public FileType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }
        public String Size
        {
            set { m_size = value; }
            get { return m_size; }
        }        
    
        //Constructor, determine type
        public OnlineDatabase(String Name, String Size, DateTime LastModification)
        {
            m_name = Name;
            m_lastmodified = LastModification;
            m_size = Size; 
            if (Path.GetExtension(m_name) == ".sdb") m_type = FileType.Database;
            else if (Path.GetExtension(m_name) == Properties.Resources.ExtensionBackup) m_type = FileType.DatabaseBackup;
            else m_type = FileType.Unknown;
        }
    }
    #endregion

}
