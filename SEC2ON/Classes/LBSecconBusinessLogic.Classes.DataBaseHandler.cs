using System;
using System.Collections.Generic;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic.Classes
{
    //handling the XML of a database
    #region Databasehandling / XML Source    
    //create and open databases and add new items to the database. extending the xmldocument class
    public class SecDB : XmlDocument
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
}
