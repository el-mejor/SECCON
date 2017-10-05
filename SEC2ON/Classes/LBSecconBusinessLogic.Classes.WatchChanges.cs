using LBPasswordAndCryptoServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic.Classes
{
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
}
