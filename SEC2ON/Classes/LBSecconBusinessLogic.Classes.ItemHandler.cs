using System;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic.Classes
{
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
}
