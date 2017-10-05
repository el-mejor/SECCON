using LBPasswordAndCryptoServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic.Classes
{
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
