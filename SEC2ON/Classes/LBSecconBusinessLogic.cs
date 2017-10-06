using LBPasswordAndCryptoServices;
using SEC2ON.LBSecconBusinessLogic.Classes;
using SEC2ON.LBSecconBusinessLogic.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace SEC2ON.LBSecconBusinessLogic
{
    public partial class SecconBL
    {
        #region properties        

        public SECCONFORM GUI = null;
        public PWHandler PWManager = new PWHandler(Convert.ToInt32(Properties.Resources.timeOutPasswordHandler)); //Passwordhandler
        public WatchChanges WatchChangesDB = new WatchChanges(); //Was the database modified?
        public List<SecItem> DBEntries = new List<SecItem>(); //Collection of all items
        public List<SecItem> DBGroups = new List<SecItem>(); //Collection of all group items
        public List<XmlElement> DBKeys = new List<XmlElement>(); //Collection of PGP Keys
        public Dictionary<string, int> m_groups = new Dictionary<string, int>(); //Dictionary of the groups
        public Timer Killclipboard = new Timer(); //timeout to delete clipboard
        public Int32 Killclipboardtimerexpired = 0;

        public String Filename {get; set;}
        public String Currenthash { get; set; }
        
        public bool HighLightPasswordAge {get; set;}

        public Boolean Sessionpinactive { get; set; }

        public Boolean ShowUserNameIsActive
        { get { return m_showlogins; } set { m_showlogins = value; } }

        public bool DetailsView {get; set;}
        public bool AllowSessionPin {get; set;}        
        public double obsolete {get; set;} //default value after how many days a item will be highlighted red (or yellow at 2/3 of obsolete timespan)

        public bool SelfTestResult { get; set; }
        public String SelfTestResultString { get; set; }

        #endregion

        #region fields        
        private DateTime m_lastsync = DateTime.MinValue;
        private Boolean m_showlogins = true;
        #endregion

        #region ctor
        public SecconBL()
        {
            initializeKillClipboardTimer();

            //perform selftest
            SelfTestResult = false;
            SelfTestResultString = selftest.Run();

            if (!SelfTestResultString.Contains("PASSED"))
            {                
                DialogResult res = MessageBox.Show("There was an error while performing the selftest! You should not use your databases nor encrypt files until the error is fixed.", "Selftest...", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else SelfTestResult = true;

            Filename = "";
        }
        #endregion

        #region Functions / BL
        //A new database will be created here
        public void CreateNewDatabase()
        {
            //Close the current database, abort if there are unsaved changes
            if (!this.closeDatabase()) return;

            //Select file to create
            SaveFileDialog newdbname = new SaveFileDialog();
            newdbname.AddExtension = true;
            newdbname.Filter = "Seccon Database (*.sdb)|*.sdb";
            newdbname.DefaultExt = "SDB";
            if (newdbname.ShowDialog() != DialogResult.OK) return;
            Filename = newdbname.FileName;

            //Ask for password and unlock PWhandler
            enterpassword enternewpassword = new enterpassword(Filename, verified: true);
            enternewpassword.ShowDialog();
            if (enternewpassword.cancel) return;
            PWManager.UnlockPW(enternewpassword.password);
            enternewpassword.deletepassword();
            enternewpassword.Dispose();

            //Create new empty database containing the passwords hash
            SecDB xmldoc = new SecDB();
            xmldoc.CreateNewDatabase();

            //create random pattern
            string pattern = PWGen.pwGenerator(1024, true, true, true, true);
            //combine pattern and xml
            string encxml = pattern + xmldoc.SelectSingleNode("/").InnerXml;

            //encrypt database and save it            
            encxml = cryptoservice.encrypt(Encoding.Unicode.GetBytes(encxml), PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true));

            //save database
            using (StreamWriter savedb = new StreamWriter(Filename))
            {
                try
                {
                    savedb.Write(encxml);
                    GUI.updateLog(2, "DB was saved succesfully.");
                }
                catch
                {
                    DialogResult dlg = MessageBox.Show("There was an error saving the database. Please try again or save it in a new file.", "Save...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GUI.updateLog(4, "There was an error saving the database.");
                }
            }

            //open new database
            this.open_database(Filename);
        }

        //open database
        public Boolean open_database(string filename)
        {
            //get new filename (either it is predefined or by open file dialog)
            Filename = filename;
            if (Filename == null)
            {
                OpenFileDialog opendbname = new OpenFileDialog();
                opendbname.AddExtension = true;
                opendbname.Filter = "Seccon Database (*.sdb)|*.sdb";
                opendbname.DefaultExt = "SDB";

                if (opendbname.ShowDialog() != DialogResult.OK) return false;

                Filename = opendbname.FileName;
            }

            bool done = false;
            SecDB openDB = new SecDB();

            while (!done)
            {
                //ask for password
                if (!this.UnlockDB())
                {
                    this.closeDatabase();
                    return false; //if it was not possible to unlock the pwm (cancel was hit in password dialog) return
                }

                //open database
                string encxml = "";

                using (StreamReader loaddb = new StreamReader(Filename))
                {
                    encxml = loaddb.ReadToEnd();
                }

                try //if decryption failes this is a good indication for a wrong key / or a faulty file - there's no difference
                {
                    string xmldoc = Encoding.Unicode.GetString(
                        cryptoservice.decrypt(encxml, PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true)));

                    //remove random pattern
                    xmldoc = xmldoc.Remove(0, 1024);

                    openDB.InnerXml = xmldoc;
                    openDB.OpenDatabase();

                    Currenthash = PWManager.PWHash;

                    done = true; //leave loop because everything is done
                }
                catch
                {
                    PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.WrongPW);
                    GUI.updateLog(4, "Wrong masterkey. No database opened.");


                    DialogResult res = MessageBox.Show(string.Format("Wrong master key entered. Reenter key?"), "Open database...", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (res == DialogResult.Cancel) return false;
                    else continue;
                }
            }

            DBEntries = openDB.DBEntries;
            DBKeys = openDB.DBKeys;

            //Move all group items to another list and remove them from the database    
            DBGroups = new List<SecItem>();
            SeparateGroups(DBEntries, DBGroups);

            HighLightPasswordAge = openDB.HighLightPasswordsAge;
            ShowUserNameIsActive = openDB.ShowUserNameIsActive;
            DetailsView = openDB.Detailsview;
            AllowSessionPin = openDB.AllowSessionPin;

            obsolete = openDB.MarkAsObsolete;
            m_lastsync = openDB.LastSync;

            if (m_lastsync == DateTime.MinValue) GUI.updateLog(2, Path.GetFileName(Filename) + " loaded succesfully.");
            else GUI.updateLog(2, Path.GetFileName(Filename) + " loaded succesfully. Last synchronization: " + m_lastsync.ToString());
            GUI.Text = Path.GetFileName(Filename) + " - SECCON";

            

            //if password was good ask for a session pin to unlock db without entering the password again
            if (openDB.AllowSessionPin)
            {
                enterpassword enterpin = new enterpassword(Filename, pin: true);
                enterpin.ShowDialog();

                if (enterpin.DialogResult != DialogResult.Cancel)
                {
                    Sessionpinactive = true;
                    PWManager.SetPin = enterpin.pin;
                }
            }

            if (openDB.UpdatedItems > 0)
            {
                WatchChangesDB.Changed = true;
                GUI.updateLog(2, string.Format("MAINTENANCE: {0} values have been updated.", openDB.UpdatedItems));
            }

            openDB = null;

            return true;
        }

        //Save the database
        public bool savedatabase(bool saveasnewfile = false)
        {
            SecDB xmldoc = new SecDB();

            //save in a new file if desired
            if (saveasnewfile)
            {
                //Select file to create
                SaveFileDialog newdbname = new SaveFileDialog();
                newdbname.AddExtension = true;
                newdbname.Filter = "Seccon Database (*.sdb)|*.sdb";
                newdbname.DefaultExt = "SDB";

                if (newdbname.ShowDialog() != DialogResult.OK) return false;
                Filename = newdbname.FileName;
            }

            //Check if database is unlocked, cancel operation if unlocking was not possible (wrong password)
            if (!this.UnlockDB(check: true)) return false;
            
            //save settings
            xmldoc.ShowUserNameIsActive = ShowUserNameIsActive;
            xmldoc.HighLightPasswordsAge = HighLightPasswordAge;
            xmldoc.MarkAsObsolete = obsolete;
            xmldoc.Detailsview = DetailsView;
            xmldoc.AllowSessionPin = AllowSessionPin;
            xmldoc.LastSync = m_lastsync;

            xmldoc.CreateNewDatabase();

            Int16 index = 0;

            //move the groupitems back to the main database            
            RecombineGroups(DBEntries, DBGroups);

            //copy all db items
            foreach (SecItem copyentry in DBEntries)
            {
                xmldoc.SelectSingleNode("/seccondb/entries").AppendChild(xmldoc.ImportNode(copyentry.XML, true));
                index++;
            }

            //Move all group items to another list and remove them from the database  
            SeparateGroups(DBEntries, DBGroups);

            //copy all db keys
            foreach (XmlElement copyentry in DBKeys)
            {
                XmlNode copiedentry = xmldoc.ImportNode(copyentry, true);

                xmldoc.SelectSingleNode("/seccondb/keys").AppendChild(copiedentry);
            }

            try
            {
                //create random pattern
                string pattern = PWGen.pwGenerator(1024, true, true, true, true);
                //combine pattern and xml
                string encxml = pattern + xmldoc.SelectSingleNode("/").InnerXml;

                //encrypt database and save it
                encxml = cryptoservice.encrypt(Encoding.Unicode.GetBytes(encxml), PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true));

                //create Hash for verifying database
                SHA1 hash = new SHA1Managed();
                byte[] filehash = hash.ComputeHash(Encoding.UTF8.GetBytes(encxml));

                using (StreamWriter savedb = new StreamWriter(Filename))
                {
                    savedb.Write(encxml);
                }
                using (StreamReader checkdb = new StreamReader(Filename))
                {
                    //verify saved file
                    byte[] checkhash = hash.ComputeHash(Encoding.UTF8.GetBytes(checkdb.ReadToEnd()));
                    if (Convert.ToBase64String(filehash) != Convert.ToBase64String(checkhash))
                    {
                        GUI.updateLog(4, string.Format("Verifying of database failed.", Convert.ToBase64String(filehash), Convert.ToBase64String(checkhash)));

                        DialogResult dlg = MessageBox.Show("There was an error saving the database. Please try again or save it in a new file.", "Save...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                    
                    GUI.updateLog(2, "Database was saved succesfully.");

                    WatchChangesDB.Changed = false;

                    //if the database was saved into a new file open it now
                    if (saveasnewfile) this.open_database(Filename);
                }
            }
            catch
            {
                DialogResult dlg = MessageBox.Show("There was an error saving the database. Please try again or save it in a new file.", "Save...", MessageBoxButtons.OK, MessageBoxIcon.Error);

                GUI.updateLog(4, "There was an error saving the database!");

                return false;
            }
            return true;
        }

        //close the database
        public bool closeDatabase()
        {
            //Popup message that there are unsaved changes!
            if (WatchChangesDB.Changed)
            {
                DialogResult dlg = MessageBox.Show("There are unsaved changes. Would you save them before closing the DB?", "Close database...", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (dlg == DialogResult.Cancel) return false;
                if (dlg == DialogResult.Yes)
                {
                    this.savedatabase();
                }
            }

            m_groups.Clear();
            
            WatchChangesDB.Changed = false;

            Currenthash = "";

            Sessionpinactive = false;
            Filename = "";
            DBEntries = null;
            DBGroups = null;
            DBKeys = null;

            PWManager.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.CloseDB);

            return true;
        }

        //Unlock the database
        public bool UnlockDB(PWHandler pwh = null, bool check = false, bool dontUseSessionPin = false)
        {
            if (pwh == null) pwh = PWManager; //if now password handler is given use the default one

            //directly return if pw manager is unlocked.
            if (!pwh.Locked) return true;

            //if session pin is active (and the pin was not entered false 3 times) ask for it, verify and return, if wrong show message box how to proceed
            while (!dontUseSessionPin)
            {
                if (Sessionpinactive && pwh.WrongPinCount > 0)
                {
                    enterpassword enterpin = new enterpassword(Filename, pin: true);
                    enterpin.ShowDialog();

                    if (enterpin.DialogResult != DialogResult.Cancel)
                    {
                        pwh.UnlockPin(enterpin.pin);
                        if (!pwh.Locked) return true;
                        else if (pwh.WrongPinCount > 0)
                        {
                            GUI.updateLog(3, "Wrong pin. Please try again.");
                        }
                    }
                    else
                    {
                        dontUseSessionPin = true;
                        continue;
                    }

                    //message box: Wrong pin was entered: retry / cancel / enter master key
                    if (pwh.WrongPinCount > 0)
                    {
                        DialogResult wrongpin =
                        MessageBox.Show(String.Format("Wrong session pin entered. Would you enter the master key instead? Click \"no\" to retry session pin. You have {0} attempts left.",
                        pwh.WrongPinCount),
                        "Wrong pin...", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                        if (wrongpin == DialogResult.Yes) dontUseSessionPin = true;
                        if (wrongpin == DialogResult.Cancel) return false;
                    }
                    else
                    {
                        DialogResult wrongpin =
                        MessageBox.Show(String.Format("Wrong session pin entered for 3 times. Would you enter the master key instead?"),
                        "Wrong pin...", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                        if (wrongpin == DialogResult.OK) dontUseSessionPin = true;
                        if (wrongpin == DialogResult.Cancel) return false;
                    }
                }
                else dontUseSessionPin = true;
            }

            enterpassword enternewpassword = new enterpassword(Filename);
            enternewpassword.ShowDialog();
            if (enternewpassword.DialogResult == DialogResult.Cancel) return false;

            pwh.UnlockPW(enternewpassword.password);
            enternewpassword.deletepassword();
            enternewpassword.Dispose();

            if (check)
            {
                if (pwh.PWHash != Currenthash)
                {
                    pwh.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.WrongPW);
                    GUI.updateLog(4, "Wrong masterkey. Please try again.");

                    return false;
                }
                else
                {
                    GUI.updateLog(2, "Masterkey accepted, database is unlocked.");
                }
            }
            return true;
        }

        //Check if database is saved for synchronisation
        private bool CheckIfDBIsSavedForSync()
        {
            //precondition: check if database is saved
            if (WatchChangesDB.Changed)
            {
                DialogResult notsaved = MessageBox.Show("There are unsaved changes in your database. Would you like to save them?", "Synchronize databases...", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (notsaved == DialogResult.No)
                {
                    GUI.updateLog(4, "To synchronize the database you need to save it first.");
                    return false;
                }
                if (!this.savedatabase()) return false;
            }
            return true;
        }

        //prepare and forward to addnewitem
        public void addEmptyItem(SecItem.ItemType itemtype, List<SecItem> DB, String selectedgroup, ImageList imageList2)
        {
            //check if database is unlocked
            if (!this.UnlockDB(check: true)) return;

            //add new empty item
            SecDB xmldoc = new SecDB();

            //generate unique name
            string newname = "";
            string group = "";
            int icon = 5;
            if (itemtype == SecItem.ItemType.Item)
            {
                newname = SecconBL.searchUniqueName("new item", DB);
                if (selectedgroup != "") group = selectedgroup;
            }
            if (itemtype == SecItem.ItemType.Group)
            {
                newname = SecconBL.searchUniqueName("new group", DB);
                group = newname;
                icon = 0;
            }

            SecItem newentry = new SecItem(newname, group, icon, "",
                cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                DateTime.Now.AddYears(9999 - DateTime.Now.Year));            

            if (itemtype == SecItem.ItemType.Group)
            {
                newentry.SetGroupDefinitionFlag();
                if (selectedgroup != "") newentry.Group = selectedgroup;
            }

            this.addItem(newentry, itemtype, DB, imageList2);

            if (itemtype == SecItem.ItemType.Group)
            {
                m_groups.Clear();                
                this.RenumberGroupPosition();
            }

            
        }

        //add new item
        public bool addItem(SecItem newentry, SecItem.ItemType itemtype, List<SecItem> DB, ImageList imageList2, bool suppresseditdialog = false)
        {
            //check if database is unlocked
            if(!this.UnlockDB(check: true)) return false;            

            SecDB xmldoc = new SecDB();

            newentry.Latest = DateTime.UtcNow;
            DB.Add(newentry);

            WatchChangesDB.Changed = true;

            if (!suppresseditdialog)
            {
                List<string> groups = GetListOfGroups(DBGroups);

                //remove newly added group name (e.g. "new group" or the placeholder group from list to not block adding it in the dialog.
                if (itemtype == SecItem.ItemType.Group) groups.RemoveAt(groups.Count - 1);

                Editentry edititem = new Editentry();
                edititem.Itemtype = itemtype;
                edititem.NewItem = true;
                edititem.Filename = Filename;
                edititem.GroupsList = groups;
                edititem.DBEntries = DB;
                edititem.Index = Convert.ToInt16(DB.Count - 1);
                edititem.PWManager = PWManager;
                edititem.DBwatcher = WatchChangesDB;
                edititem.Symbols = imageList2;
                edititem.ShowDialog();
                if (edititem.DialogResult == DialogResult.Cancel)
                {
                    DB.RemoveAt(DB.Count - 1);
                    return false;
                }
            }

            WatchChangesDB.Changed = true;
            if (itemtype == SecItem.ItemType.Item) GUI.updateLog(2, "New item was added.");
            if (itemtype == SecItem.ItemType.Group) GUI.updateLog(2, "New group was added.");

            return true;
        }

        //edit an item
        public void editentry(int index, ImageList imageList2)
        {
            //log entry that an item was opened
            GUI.updateLog(2, string.Format("Item \"{0}\" opened for edit.", DBEntries[index].Name), true, Color.Black, Color.Yellow);

            //open the form
            Editentry edititem = new Editentry();
            edititem.Itemtype = SecItem.ItemType.Item;
            edititem.NewItem = false;
            edititem.Filename = Filename;
            edititem.GroupsList = SecconBL.GetListOfGroups(DBGroups);
            edititem.DBEntries = DBEntries;
            edititem.Index = index;
            edititem.PWManager = PWManager;
            edititem.DBwatcher = WatchChangesDB;
            edititem.Symbols = imageList2;
            edititem.ShowDialog();

            //when the item was renamed create an deleted entry item for the old name to prevent that the item appears with its old name again after synchronizing
            if (edititem.Renamed)
            {
                //add new empty item
                SecDB xmldoc = new SecDB();

                //set items old name
                String newname = edititem.Oldname;

                SecItem newentry = new SecItem(newname, "", 5, "", "", "", "", "", DateTime.Now.AddYears(9999 - DateTime.Now.Year));

                //mark as deleted
                newentry.SetDeletedFlag();

                //add to db
                DBEntries.Add(newentry);
            }
        }

        //delete a item
        public void deleteItem(List<int> indexes)
        {
            

            bool deleteAll = false;
            int deleteCount = 0;
            foreach (int index in indexes)
            {
                //delete item(s)
                //DBEntries.RemoveAt(Convert.ToInt16(listViewAccounts.SelectedItems[0].Name));
                if (!this.UnlockDB(check: true)) return;

                //Ask if the item shall really be deleted / all selected items shall be deleted 
                if (indexes.Count == 1 && !deleteAll)
                {
                    DialogResult dlg = MessageBox.Show(String.Format("Would you really delete \"{0}\"?", DBEntries[index].Name), "Delete...",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dlg == DialogResult.No) continue;
                }
                else if (!deleteAll)
                {
                    DialogResult dlg = MessageBox.Show(String.Format("Would you really delete all selected items ({0})?", indexes.Count), "Delete...",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (dlg == DialogResult.No) break;
                    else deleteAll = true;
                }

                DBEntries[index].DeleteItem();
                
                deleteCount++;

                GUI.updateLog(2, string.Format("Item \"{0}\" has been deleted.", DBEntries[index].Name));

                WatchChangesDB.Changed = true;
            }

            if (deleteAll) GUI.updateLog(2, string.Format("{0} Items have been deleted.", deleteCount));


        }

        //create an item for the file to encrypt and encrypt it 
        public void encryptfile(string[] files, ToolStripProgressBar toolStripProgressBar1, ImageList imageList2)
        {
            //Create new pwhandler
            PWHandler filepw = new PWHandler();
            //create random session pin for file pwhandler - this is for preventing the pwhandler from locking since the pin will be given before each single file
            Random rnd = new Random();
            byte[] pin = Encoding.ASCII.GetBytes(rnd.Next(0, 1000000).ToString());

            //Create new item with the necessary information
            //add new empty item
            SecItem newentry;

            bool checkpwdone = false;

            SecDB xmldoc = new SecDB();

            //if a database is opened create a new item for the encrypted file and show the editentry dialog for creating a password. If not 
            //database is opened just show the enterpassword dialog.
            if (Filename != "")
            {
                //check if database is unlocked
                if (!this.UnlockDB(check: true)) return;

                while (!checkpwdone)
                {
                    string itemname;
                    if (files.Length > 1) itemname = string.Format("{0} + {1} more", Path.GetFileName(files[0]), files.Length - 1);
                    else itemname = string.Format("{0}", Path.GetFileName(files[0]));
                    if (files.Length == 1) newentry = new SecItem(itemname, "Encrypted files", 13, files[0].ToString() + ".sef",
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(string.Format("{0} - {1} files", files[0].ToString() + ".sef", files.Length)), PWManager),
                        DateTime.Now.AddYears(9999 - DateTime.Now.Year));
                    else newentry = new SecItem(itemname, "Encrypted files", 13, Path.GetDirectoryName(files[0]).ToString(),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                        cryptoservice.encrypt(Encoding.Unicode.GetBytes(string.Format("{0} - {1} files", Path.GetDirectoryName(files[0]), files.Length)), PWManager),
                        DateTime.Now.AddYears(9999 - DateTime.Now.Year));

                    //Add the item and show the edit item dialog
                    if (!this.addItem(newentry, SecItem.ItemType.Item, DBEntries, imageList2))
                    {   
                        return;
                    }

                    //Use the given password for encrypting, check it before storing the item and proceed
                    byte[] pw = Encoding.ASCII.GetBytes(Encoding.Unicode.GetString(cryptoservice.decrypt(newentry.Password, PWManager)));
                    filepw.UnlockPW(pw);
                    filepw.SetPin = pin;

                    if (pw.Length == 0)
                    {
                        DialogResult respw = MessageBox.Show(string.Format("No key was given, please enter a key."), "Encrypting file...", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation);
                        DBEntries.RemoveAt(DBEntries.Count - 1);
                        if (respw == DialogResult.Cancel)
                        {                            
                            return;
                        }
                    }
                    else
                    {
                        checkpwdone = true;
                        
                    }
                }
            }
            else
            {
                //Ask for password if no database is opened (withou creating an item)
                enterpassword enternewpassword = new enterpassword(files[0], verified: true, file: true);
                enternewpassword.ShowDialog();
                if (enternewpassword.cancel)
                {                    
                    return;
                }
                //Use the given password for encrypting
                filepw.UnlockPW(enternewpassword.password);
                filepw.SetPin = pin;
                enternewpassword.deletepassword();
                enternewpassword.Dispose();
            }

            //If there are more than one files to encrypt show the progressbar
            if (files.Length > 1)
            {
                toolStripProgressBar1.Visible = true;
                toolStripProgressBar1.Maximum = files.Length;
            }
            int i = 0;
            int err = 0;

            //Encrypt the files
            foreach (string singlefile in files)
            {
                i++;
                if (files.Length > 1) toolStripProgressBar1.Value = i;

                //Unlock the handler by giving the pin for each file to prevent it from locking in the meantime
                filepw.UnlockPin(pin);

                //encrypt the file
                MemoryStream f = new MemoryStream();
                MemoryStream h = new MemoryStream();

                GUI.updateLog(5, string.Format("Encryption of \"{0}\" in progress... ({1}/{2})", Path.GetFileName(singlefile), i, files.Length));

                Application.DoEvents();
                SHA256 sha = new SHA256Managed();
                byte[] hashsrc = null;
                byte[] hashenc = null;
                //try
                //{
                    using (FileStream file = new FileStream(singlefile, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        file.CopyTo(f);
                    }
                    //calculate hash for source file
                    hashsrc = sha.ComputeHash(f.ToArray());

                    using (FileStream file = new FileStream(singlefile + ".sef", FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        byte[] output = cryptoservice.fileencrypt(f, filepw.CurrentPWKey(high: true), filepw.CurrentPWIV(high: true)).ToArray();
                        file.Write(output, 0, output.Length);
                        output = null;
                    }

                    //calculate hash for encrypted and decrypted file and compare
                    using (FileStream file = new FileStream(singlefile + ".sef", FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        file.CopyTo(h);
                    }

                    hashenc = sha.ComputeHash(cryptoservice.filedecrypt(h, filepw.CurrentPWKey(high: true), filepw.CurrentPWIV(high: true)).ToArray());

                    if (Convert.ToBase64String(hashsrc) != Convert.ToBase64String(hashenc))
                    {
                        //hashes are not equal, claim and count errors
                        GUI.updateLog(4, string.Format("Verifying of \"{0}\" failed.", Path.GetFileName(singlefile)));

                        DialogResult res = MessageBox.Show(string.Format("Verification \"{0}\" failed.", Path.GetFileName(singlefile)), "Encrypting file...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (files.Length == 1 && Filename != "")
                        {
                            DBEntries.RemoveAt(DBEntries.Count - 1);

                        }
                        err++;
                    }
                //}
                //catch
                //{
                //    GUI.updateLog(4, string.Format("Encryption of \"{0}\" failed.", Path.GetFileName(singlefile)));

                //    DialogResult res = MessageBox.Show(string.Format("Something went wrong, encryption of \"{0}\" failed.", Path.GetFileName(singlefile)), "Encrypting file...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //    if (files.Length == 1)
                //    {
                //        DBEntries.RemoveAt(DBEntries.Count - 1);

                //    }
                //    err++;
                //}

                //lock the pwhandler
                filepw.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);
            }
            filepw.SetPin = null;
            filepw.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);

            //show ready message
            if (err > 0 && files.Length > 1)
            {
                GUI.updateLog(4, string.Format("Encryption of {0} file(s) out of {1} failed.", err, files.Length));
            }
            else if (files.Length == 1)
            {
                GUI.updateLog(2, string.Format("Encryption of \"{0}\" finished.", Path.GetFileName(files[0])));
            }
            else if (i == files.Length)
            {
                GUI.updateLog(2, string.Format("Encryption of {0} files finished.", files.Length));
            }
            toolStripProgressBar1.Visible = false;


        }

        //decrypt file
        public void decryptfile(string[] files, ToolStripProgressBar toolStripProgressBar1,bool openfile = true)
        {            
            bool cancel = false;

            string newfilename = "";

            int err = 0;

            //Create new pwhandler
            PWHandler filepw = new PWHandler();
            //create random session pin for file pwhandler - this is for preventing the pwhandler from locking since the pin will be given before each single file
            Random rnd = new Random();
            byte[] pin = Encoding.ASCII.GetBytes(rnd.Next(0, 1000000).ToString());

            while (!cancel)
            {
                //Ask for password and unlock the PWhandler
                enterpassword pw = new enterpassword(files[0], verified: false, file: true);
                pw.ShowDialog();
                if (pw.cancel)
                {                    
                    return;
                }
                filepw.UnlockPW(pw.password);
                filepw.SetPin = pin;
                pw.deletepassword();
                pw.Dispose();

                int i = 0;

                if (files.Length > 1)
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripProgressBar1.Maximum = files.Length;
                }

                cancel = true; //the loop can be left if nothing happens while decryption
                foreach (string file in files)
                {
                    MemoryStream f = new MemoryStream();

                    i++;
                    toolStripProgressBar1.Value = i;

                    filepw.UnlockPin(pin);

                    //decrypt file and save it
                    GUI.updateLog(5, string.Format("Decryption of \"{0}\" ({1}/{2}) in progress...", Path.GetFileName(file), i, files.Length));

                    Application.DoEvents();
                    newfilename = file.Replace(Path.GetExtension(file), "");
                    string newfilename_woi = file.Replace(Path.GetExtension(file), "");
                    string newfilename_ext = Path.GetExtension(newfilename);

                    try
                    {
                        using (FileStream decfile = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            decfile.CopyTo(f);
                        }

                        //if the file already exists create it with an index
                        int ix = 0;
                        while (File.Exists(newfilename))
                        {
                            ix++;
                            newfilename = newfilename_woi.Replace(Path.GetExtension(newfilename), "(" + ix + ")") + newfilename_ext;
                        }

                        using (FileStream decfile = new FileStream(newfilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            byte[] output = cryptoservice.filedecrypt(f, filepw.CurrentPWKey(high: true), filepw.CurrentPWIV(high: true)).ToArray();
                            decfile.Write(output, 0, output.Length);
                            output = null;
                        }

                        GUI.updateLog(2, string.Format("Decryption of \"{0}\" finished.", Path.GetFileName(file)));
                    }
                    //catch when the decryption failed
                    catch (System.Security.Cryptography.CryptographicException)
                    {
                        GUI.updateLog(4, string.Format("Decryption of \"{0}\" failed.", Path.GetFileName(file)));
                        try { File.Delete(newfilename); }
                        catch { }

                        if (i == 1) //when first file failes ask for reentering the key
                        {
                            err++;
                            DialogResult res = MessageBox.Show(string.Format("Wrong key entered for {0}. Reenter key?", file), "Decrypting file...", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error);
                            if (res == DialogResult.Yes)
                            {
                                cancel = false; //retry
                                filepw.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);
                                break;
                            }
                            if (res == DialogResult.Cancel)
                            {
                                cancel = true; //cancel decryption
                                
                                break;
                            }
                        }

                        if (i > 1)
                        {
                            err++;
                            DialogResult res = MessageBox.Show(string.Format("Wrong key entered for {0}. Proceed?", file), "Decrypting file...", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                            if (res == DialogResult.No)
                            {
                                cancel = true; //cancel decryption
                                break;
                            }
                        }
                    }
                    //catch that something went wrong while decryption
                    catch
                    {
                        GUI.updateLog(4, string.Format("Decryption of \"{0}\" failed.", Path.GetFileName(file)));

                        try { File.Delete(newfilename); }
                        catch { }
                        err++;
                        DialogResult res = MessageBox.Show(string.Format("Decryption of {0} failed. Proceed?", file), "Decrypting file...", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                        if (res == DialogResult.No)
                        {
                            cancel = true; //cancel decryption
                            
                            break;
                        }
                    }
                    filepw.SetPin = null;
                    filepw.Lock(PWHandler.PWHandlerEventArgs.PWHandlerLockReasonEnum.None);
                }

                pw.deletepassword();
                pw.Dispose();

                //show ready message
                if (err > 0 && files.Length > 1)
                {
                    GUI.updateLog(4, string.Format("Decryption of {0} file(s) out of {1} failed.", err, files.Length));
                }
                else if (files.Length == 1)
                {
                    GUI.updateLog(2, string.Format("Decryption of \"{0}\" finished.", Path.GetFileName(files[0])));
                }
                else if (i == files.Length)
                {
                    GUI.updateLog(2, string.Format("Decryption of {0} files finished.", files.Length));
                }


            }

            toolStripProgressBar1.Visible = false;

            if (files.Length > 1 && err > 0)
            {
                GUI.updateLog(4, string.Format("Decryption of {0} files failed.", err));

            }
            else if (files.Length > 1)
            {
                GUI.updateLog(2, string.Format("Decryption of {0} files finished.", files.Length));

            }

            if (openfile)
            {
                if (files.Length == 1)
                {
                    try
                    {
                        System.Diagnostics.ProcessStartInfo pinfo = new System.Diagnostics.ProcessStartInfo();
                        pinfo.FileName = newfilename;
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo = pinfo;
                        p.Start();
                    }
                    catch
                    {
                        GUI.updateLog(4, string.Format("Can not open the file: {0}", newfilename));

                        DialogResult __res = MessageBox.Show("Can not open the file.", "Decrypting file...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            
        }

        //Create string with items contents
        private byte[] exportItemGetString(int index)
        {
            string expirydate = "";

            if (DBEntries[index].ExpirationDate.Year != 9999)
            {
                expirydate = ("*Expiration date: ") +
                (DBEntries[index].ExpirationDate.ToLocalTime().ToString() + Environment.NewLine);
            }

            byte[] exportstring = Encoding.Unicode.GetBytes(

            string.Format("= {0} =========", DBEntries[index].Name) + Environment.NewLine + Environment.NewLine +

            "#BEGINN" + Environment.NewLine +

            ("*Name: ") +
            (DBEntries[index].Name) + Environment.NewLine +

            ("*Icon: ") +
            (DBEntries[index].ImageIndex.ToString()) + Environment.NewLine +

            ("*Group: ") +
            (DBEntries[index].Group) + Environment.NewLine +

            ("*URL: ") +
            (DBEntries[index].Url + Environment.NewLine) +

            ("*Username: ") +
            (Encoding.Unicode.GetString(cryptoservice.decrypt(DBEntries[index].Username, PWManager)) + Environment.NewLine) +

            ("*Password: ") +
            (Encoding.Unicode.GetString(cryptoservice.decrypt(DBEntries[index].Password, PWManager)) + Environment.NewLine) +

            ("*Notes: " + Environment.NewLine) +
            (Encoding.Unicode.GetString(cryptoservice.decrypt(DBEntries[index].Notes, PWManager)) + Environment.NewLine) +

            expirydate +

            ("*Last change of password: ") +
            DBEntries[index].LastModification.ToLocalTime().ToString() + Environment.NewLine +

            "#END" + Environment.NewLine + Environment.NewLine);

            return exportstring;
        }

        //export item
        public enum exportItemsEncryption { None, PGP, AES, AESClipboard };
        public void exportItem(ArrayList indexes, exportItemsEncryption encryption)
        {
            //check if database is unlocked
            if (!this.UnlockDB(check: true)) return;

            //create string
            MemoryStream exportstream = new MemoryStream();
            Byte[] Header = Encoding.Unicode.GetBytes("EXPORTED SECCON ITEMS" + Environment.NewLine);
            exportstream.Write(Header, 0, Header.Length);
            Header = Encoding.Unicode.GetBytes("===================================================================" + Environment.NewLine + Environment.NewLine);
            exportstream.Write(Header, 0, Header.Length);

            foreach (Int16 index in indexes)
            {
                byte[] buffer = exportItemGetString(index);
                exportstream.Write(buffer, 0, buffer.Length);
                PWHandler.ClearByte(buffer);
            }

            //Choose filename and location for saving (don't ask when the clipboard is the target)
            string exportfilename = "";
            if (encryption != exportItemsEncryption.AESClipboard)
            {
                if (indexes.Count > 1) exportfilename = "Seccon-Items.txt";
                else exportfilename = "Seccon-Item_" + (DBEntries[Convert.ToInt16(indexes[0])]).Name + ".txt";
                if (encryption == exportItemsEncryption.AES) exportfilename += ".sef";
                if (encryption == exportItemsEncryption.PGP) exportfilename += ".pgp";

                SaveFileDialog exportto = new SaveFileDialog();
                exportto.AddExtension = true;
                exportto.DefaultExt = Path.GetExtension(exportfilename);
                exportto.FileName = exportfilename;

                if (exportto.ShowDialog() != DialogResult.OK) return;
                exportfilename = exportto.FileName;
            }

            //encrypt and save string
            if (encryption == exportItemsEncryption.None)
            {
                //warn that this is not a good idea
                DialogResult warn = MessageBox.Show("Would you really export the item without encrypting it? Everybody is able to read the content!", "Export unencrypted...", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (warn == DialogResult.No) return;

                //save string

                using (FileStream exportfile = new FileStream(exportfilename + ".txt", FileMode.Create, FileAccess.Write))
                {
                    exportstream.WriteTo(exportfile);
                }

                GUI.updateLog(3, string.Format("Selected item(s) were exported UNENCRYPTED(!) to: \"{0}\"", exportfilename, Color.Black, Color.Yellow));
                return;
            }
            if (encryption == exportItemsEncryption.AES)
            {
                //Ask for password
                enterpassword enternewpassword = new enterpassword(exportfilename + ".txt", verified: true, file: true);
                enternewpassword.ShowDialog();
                if (enternewpassword.cancel)
                {
                    
                    return;
                }

                PWHandler filepw = new PWHandler();
                filepw.UnlockPW(enternewpassword.password);

                enternewpassword.deletepassword();
                enternewpassword.Dispose();

                //encrypt the file

                using (FileStream file = new FileStream(exportfilename, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    byte[] output = cryptoservice.fileencrypt(exportstream, filepw.CurrentPWKey(high: true), filepw.CurrentPWIV(high: true)).ToArray();
                    file.Write(output, 0, output.Length);
                    output = null;
                }

                GUI.updateLog(2, string.Format("Selected item(s) were encrypted and exported to: \"{0}\"", exportfilename, Color.Black, Color.Yellow));
                return;
            }
            if (encryption == exportItemsEncryption.AESClipboard)
            {
                //encrypt exported item and store it in the clipboard
                using (MemoryStream clipboard = new MemoryStream())
                {
                    byte[] output = cryptoservice.fileencrypt(exportstream, PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true)).ToArray();
                    clipboard.Write(output, 0, output.Length);
                    output = null;

                    Clipboard.SetData("SECCONITEMS", clipboard);
                }

                return;
            }

            if (encryption == exportItemsEncryption.PGP)
            {
                ManageKeys getpgpkey = new ManageKeys(DBKeys, askforkey: true);
                getpgpkey.ShowDialog();
                if (getpgpkey.DialogResult == DialogResult.Cancel) return;

                //PGP encrypt string by using inputstring and given keyfile

                string keystring = "";
                foreach (XmlElement keyelement in DBKeys)
                {
                    if (keyelement.GetAttributeNode("ID").Value == getpgpkey.SelectedKeyID)
                    {
                        keystring = keyelement.InnerText;
                        break;
                    }
                }

                cryptoservice.PGPencrypt(Encoding.Unicode.GetString(exportstream.GetBuffer()), keystring, exportfilename);

                GUI.updateLog(2, string.Format("Selected item(s) were PGP-encrypted and exported to: \"{0}\"", exportfilename, Color.Black, Color.Yellow));
                return;
            }
        }

        //Synchronize with another database
        public void SynchronizeWithOtherDB()
        {
            //precondition: check if database is unlocked
            if (!this.UnlockDB(check: true)) return;

            //precondition: check if database is saved
            if (!CheckIfDBIsSavedForSync()) return;

            //Precondition: select target database
            SaveFileDialog targetfilename = new SaveFileDialog();
            targetfilename.AddExtension = true;
            targetfilename.Filter = "Seccon Database (*.sdb)|*.sdb";
            targetfilename.DefaultExt = "SDB";

            if (targetfilename.ShowDialog() != DialogResult.OK) return;
            string targetdb = targetfilename.FileName;

            if (targetdb == Filename)
            {
                //claim that target and current database must not be the same
                DialogResult res = MessageBox.Show("Current and target database must not be the same!", "Synchronizing...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                GUI.updateLog(4, "Current and target database must not be the same!");
                return;
            }

            //Precondition: make backup
            string backup = targetdb.Replace(".sdb", Properties.Resources.ExtensionBackup);
            try
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Copy(targetdb, backup);
            }
            catch
            {
                GUI.updateLog(4, "Making a backup of the target database failed.");
                return;
            }

            //BEGIN SYNCHRONIZE
            int updated = 0;
            int updatedpgp = 0;
            SynchronizeDatabasesResult synchresult = this.synchronizeDatabases(targetdb, ref updated, ref updatedpgp);
            if (synchresult == SynchronizeDatabasesResult.Error || synchresult == SynchronizeDatabasesResult.Cancelled)
                return;
            if (synchresult == SynchronizeDatabasesResult.OverwriteTarget)
            {
                try
                {
                    File.Delete(targetdb);
                    File.Copy(Filename, targetdb);
                }
                catch
                {
                    GUI.updateLog(4, "Overwriting the target database failed. Maybe it's in use by another process or it is write protected.");
                    return;
                }
            }
            //END SYNCHRONIZE

            //Save current database
            if (!this.savedatabase())
            {
                GUI.updateLog(4, "Saving the current database failed - the target database is keeping untouched.");
                return;
            }

            //Delete target database and replace it by the current one
            try
            {
                File.Delete(targetdb);
                File.Copy(Filename, targetdb);
            }
            catch
            {
                GUI.updateLog(4, "Updating the target database failed - the target database is keeping untouched.");
            }

            //refresh the listview
            m_groups.Clear();

            //Report if there were updated items
            if (updated == 0 && updatedpgp == 0) GUI.updateLog(2, string.Format("The local database is up to date.", updated, updatedpgp));
            if (updated > 0 && updatedpgp == 0) GUI.updateLog(2, string.Format("Updated items: {0}", updated, updatedpgp));
            if (updated == 0 && updatedpgp > 0) GUI.updateLog(2, string.Format("Updated PGP-Keys: {1}", updated, updatedpgp));
            if (updated > 0 && updatedpgp > 0) GUI.updateLog(2, string.Format("Updated items: {0}, updated PGP-Keys: {1}", updated, updatedpgp));

            return;
        }

        //Synchronize two databases
        enum SynchronizeDatabasesResult { Finished, Cancelled, OverwriteTarget, Error };
        private SynchronizeDatabasesResult synchronizeDatabases(string targetDB, ref int updated, ref int updatedpgp)
        {
            //open target database
            SecDB dbdatabase = new SecDB();
            string encxml = "";

            using (StreamReader loaddb = new StreamReader(targetDB))
            {
                encxml = loaddb.ReadToEnd();
            }

            try //if decryption failes this is a good indication for a wrong key / or a faulty file - there's no difference
            {
                string xmldoc = Encoding.Unicode.GetString(cryptoservice.decrypt(encxml, PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true)));

                //remove random pattern
                xmldoc = xmldoc.Remove(0, 1024);

                dbdatabase.InnerXml = xmldoc;
                dbdatabase.OpenDatabase();
            }
            catch
            {
                DialogResult invaliddb = MessageBox.Show(String.Format("The target database cannot be opened. Maybe the master key is different to the current database.{0}" +
                "- If you changed the master key the target version needs to be replaced by the local version. " +
                "Make sure that you have synchronized any changes before you changed the master key. Hit OK if you agree with replacing the target version. {0}" +
                "- If you have changed the master key of the target version just change your master key in the current database too. " +
                "Hit CANCEL to abort synchronizing. {0}" +
                "- The target version of the database is corrupt. Try again to synchronize it (hit CANCEL) or delete the target version to replace it by the current version (hit OK).{0}" +
                "- The name you choosed for your current database is already used on the target location. Hit CANCEL to save the database under a different name." +
                "{0}{0}Do you want to delete the target version of the database? The data of the target database will be lost.", Environment.NewLine), "Synchronizing database...", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

                if (invaliddb == DialogResult.Cancel)
                {
                    GUI.updateLog(4, "The target database cannot be opened. Maybe the password is different to the current database or the file is corrupt.");
                    return SynchronizeDatabasesResult.Cancelled;
                }
                else
                {
                    return SynchronizeDatabasesResult.OverwriteTarget;
                }
            }

            List<SecItem> DBEntries2 = dbdatabase.DBEntries;

            updated = 0;

            //combine groups and items for sync
            //move the groupitems back to the main database
            RecombineGroups(DBEntries, DBGroups);

            //check if there are newer items or new items in the dropbox version and overtake them locally

            for (int ionl = 0; ionl < DBEntries2.Count; ionl++)
            {
                SecItem onlineitem = DBEntries2[ionl];
                bool doesntexist = true;
                for (int iloc = 0; iloc < DBEntries.Count; iloc++)
                {
                    SecItem localitem = DBEntries[iloc];

                    if ((onlineitem.Name == localitem.Name) ||
                        (onlineitem.Name.ToLower() == localitem.Name.ToLower() && onlineitem.GetGroupDefinitionFlag()))
                    {
                        doesntexist = false;
                        //check if newer
                        if (DateTime.Compare(onlineitem.Latest, localitem.Latest) > 0)
                        {
                            //is newer, replace
                            DBEntries[iloc] = DBEntries2[ionl];
                            WatchChangesDB.Changed = true;

                            if (!onlineitem.GetDeletedFlag())
                            {
                                GUI.updateLog(5, string.Format("SYNCHRONIZE: {0} has been updated - Timestamp: Source {1}, Target {2}",
                                    onlineitem.Name,
                                    localitem.Latest,
                                    onlineitem.Latest), logonly: true);
                            }
                            else if (onlineitem.GetDeletedFlag())
                            {
                                GUI.updateLog(5, string.Format("SYNCHRONIZE: {0} has been deleted - Timestamp: Source {1}, Target {2}",
                                    onlineitem.Name,
                                    localitem.Latest,
                                    onlineitem.Latest), logonly: true);
                            }

                            updated++;
                        }
                        else
                        {
                            if (!onlineitem.GetDeletedFlag())
                            {
                                //this.updateLog(-1, string.Format("Merge: {0} has not been updated - Timestamp: Source {1}, Target {2}",
                                //    onlineitem.GetAttributeNode("name").Value,
                                //    localitem.SelectSingleNode("latest").InnerText,
                                //    onlineitem.SelectSingleNode("latest").InnerText), logonly: true);
                            }
                        }
                    }
                }

                //if does not exist - add
                if (doesntexist)
                {
                    DBEntries.Add(DBEntries2[ionl]);
                    WatchChangesDB.Changed = true;

                    //give a message and count when a non-deleted item was added only
                    if (!onlineitem.GetDeletedFlag())
                    {
                        GUI.updateLog(5, string.Format("SYNCHRONIZE: {0} has been added.",
                        onlineitem.Name));
                        updated++;
                    }
                }
            }

            //check if there are newer pgp keys or new keys in the dropbox version and overtake them locally
            updatedpgp = 0;
            List<XmlElement> DBKeys2 = dbdatabase.DBKeys;

            for (int ionl = 0; ionl < DBKeys2.Count; ionl++)
            {
                XmlElement onlineitem = DBKeys2[ionl];
                bool doesntexist = true;
                for (int iloc = 0; iloc < DBKeys.Count; iloc++)
                {
                    XmlElement localitem = DBKeys[iloc];
                    if (onlineitem.GetAttributeNode("ID").Value == localitem.GetAttributeNode("ID").Value)
                    {
                        doesntexist = false;
                        //check if newer
                        if (onlineitem.GetAttributeNode("latest") == null) onlineitem.SetAttribute("latest", DateTime.UtcNow.ToString());
                        if (localitem.GetAttributeNode("latest") == null) localitem.SetAttribute("latest", DateTime.UtcNow.ToString());
                        if (DateTime.Compare(Convert.ToDateTime(onlineitem.GetAttributeNode("latest").Value), Convert.ToDateTime(localitem.GetAttributeNode("latest").Value)) > 0)
                        {
                            //is newer, replace                            
                            DBKeys[iloc] = DBKeys2[ionl];
                            WatchChangesDB.Changed = true;
                            updatedpgp++;
                            break;
                        }
                    }
                }
                //if the key does not exist - add it
                if (doesntexist)
                {
                    DBKeys.Add(DBKeys2[ionl]);
                    WatchChangesDB.Changed = true;
                    updatedpgp++;
                }
            }

            //Clean up
            //Check for redundant group information and delete the superfluous group def item
            bool noredundantitemfound = false;
            while (!noredundantitemfound)
            {
                noredundantitemfound = true;
                for (int i = 0; i < DBEntries.Count; i++)
                {
                    if (DBEntries[i].GetGroupDefinitionFlag() && !DBEntries[i].GetDeletedFlag())
                    {
                        int searchforseconditem = 0;
                        for (int i2 = DBEntries.Count - 1; i2 >= 0; i2--)
                        {
                            if (DBEntries[i2].GetGroupDefinitionFlag() && !DBEntries[i2].GetDeletedFlag() &&
                                DBEntries[i2].Name.ToLower() == DBEntries[i].Name.ToLower())
                            {
                                searchforseconditem++;
                                if (searchforseconditem == 2)
                                {
                                    noredundantitemfound = false;
                                    GUI.updateLog(5, string.Format("Redundant group definition deleted: {0}", DBEntries[i2].Group), true);
                                    DBEntries.RemoveAt(i2);

                                    break;
                                }
                            }
                        }
                        if (!noredundantitemfound) break;
                    }
                }
            }

            //Move all group items to another list and remove them from the database    
            SeparateGroups(DBEntries, DBGroups);

            //remember the time of the synchronization
            m_lastsync = DateTime.Now;

            return SynchronizeDatabasesResult.Finished;
        }

        //Import Items
        public void importItems(MemoryStream input, String group, ImageList imageList2, Boolean fromclipboard)
        {
            this.importItems(input, group, imageList2, "clipboard content", true);
        }
        public void importItems(MemoryStream input, String group, ImageList imageList2, String file)
        {
            this.importItems(input, group, imageList2, file, false);
        }
        public void importItems(MemoryStream input, String group, ImageList imageList2, String file, Boolean fromclipboard)
        {
            byte[] importsource = null;
            try //try to decrypt with the database's password, when not possible ask for password to decrypt
            {
                importsource = cryptoservice.filedecrypt(input, PWManager.CurrentPWKey(high: true), PWManager.CurrentPWIV(high: true)).ToArray();
                input.Dispose();
            }
            catch
            {
                try
                {
                    //ask for password to decrypt file

                    enterpassword password = new enterpassword(file, file: true);
                    password.ShowDialog();
                    PWHandler importpw = new PWHandler();
                    importpw.UnlockPW(password.password);
                    password.deletepassword();
                    password.Dispose();

                    importsource = cryptoservice.filedecrypt(input, importpw.CurrentPWKey(high: true), importpw.CurrentPWIV(high: true)).ToArray();
                    input.Dispose();
                }
                catch
                {
                    GUI.updateLog(4, string.Format("Decryption of \"" + file + "\" failed. Maybe wrong password was given."));
                    return;
                }
            }

            //import item(s)

            int importcount = 0;
            int errcount = 0;

            //split string into lines
            string[] importstringlines = Encoding.Unicode.GetString(importsource.ToArray(), 0, importsource.Length).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            PWHandler.ClearByte(importsource);

            int line = 0;
            while (line <= importstringlines.Length - 1)
            {
                if (importstringlines[line] == "#BEGINN") //item starts with #BEGINN and ends with #END
                {
                    try
                    {
                        SecDB xmldoc = new SecDB();
                        String name = "";                        
                        int image = 0;
                        String url = "";
                        String lastmodified = DateTime.UtcNow.ToString();
                        String expirydate = (DateTime.Now.AddYears(9999 - DateTime.Now.Year)).ToString();
                        byte[] username = null;
                        byte[] password1 = null;
                        byte[] notes = null;

                        line++;
                        while (line <= importstringlines.Length - 1)
                        {

                            //Gain items information
                            if (importstringlines[line].Contains("*Name: ")) name = importstringlines[line].Replace("*Name: ", "");
                            if (importstringlines[line].Contains("*Group: ") && group == "") group = importstringlines[line].Replace("*Group: ", "");                            
                            if (importstringlines[line].Contains("*Icon: ")) image = Convert.ToInt16(importstringlines[line].Replace("*Icon: ", ""));
                            if (importstringlines[line].Contains("*URL: ")) url = importstringlines[line].Replace("*URL: ", "");


                            if (importstringlines[line].Contains("*Username: ")) username = Encoding.Unicode.GetBytes(importstringlines[line].Replace("*Username: ", ""));
                            if (importstringlines[line].Contains("*Password: ")) password1 = Encoding.Unicode.GetBytes(importstringlines[line].Replace("*Password: ", ""));

                            if (importstringlines[line].Contains("*Notes: "))
                            {
                                MemoryStream notesstream = new MemoryStream();
                                if (!fromclipboard)
                                {
                                    byte[] addnotes = Encoding.Unicode.GetBytes("IMPORTED" + Environment.NewLine);
                                    notesstream.Write(addnotes, 0, addnotes.Length);
                                }

                                while (line <= importstringlines.Length - 1)
                                {
                                    line++;
                                    if (importstringlines[line] == "#END") break;
                                    if (importstringlines[line].Contains("*Last change of password: "))
                                    {
                                        lastmodified = importstringlines[line].Replace("*Last change of password: ", "");
                                        continue;
                                    }
                                    if (importstringlines[line].Contains("*Expiration date: "))
                                    {
                                        expirydate = importstringlines[line].Replace("*Expiration date: ", "");
                                        continue;
                                    }

                                    byte[] newline = Encoding.Unicode.GetBytes(Environment.NewLine);

                                    notesstream.Write(Encoding.Unicode.GetBytes(importstringlines[line]), 0, Encoding.Unicode.GetBytes(importstringlines[line]).Length);
                                    notesstream.Write(newline, 0, newline.Length);
                                }
                                notes = notesstream.ToArray();
                                notesstream.Dispose();
                                break;
                            }
                            line++;
                        }

                        if (username == null) username = Encoding.Unicode.GetBytes("");
                        if (password1 == null) password1 = Encoding.Unicode.GetBytes("");
                        if (notes == null) notes = Encoding.Unicode.GetBytes("IMPORTED");

                        SecItem newentry = new SecItem(SecconBL.searchUniqueName(name, DBEntries), group, image, url,
                            cryptoservice.encrypt(username, PWManager),
                            cryptoservice.encrypt(password1, PWManager),
                            cryptoservice.encrypt(Encoding.Unicode.GetBytes(""), PWManager),
                            cryptoservice.encrypt(notes, PWManager),
                            Convert.ToDateTime(expirydate).ToUniversalTime());

                        newentry.LastModification = Convert.ToDateTime(lastmodified).ToUniversalTime();

                        this.addItem(newentry, SecItem.ItemType.Item, DBEntries, imageList2, suppresseditdialog: true);

                        importcount++;

                        PWHandler.ClearByte(username);
                        PWHandler.ClearByte(password1);
                        PWHandler.ClearByte(notes);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                        errcount++;
                    }
                }
                line++;
            }            

            //Show result message
            if (errcount == 0)
            {
                if (importcount == 1) GUI.updateLog(2, string.Format("{0} Item has been imported succesfully.", importcount));
                else GUI.updateLog(2, string.Format("{0} Items have been imported succesfully.", importcount));                
            }
            else
            {
                GUI.updateLog(4, string.Format("{0} Item(s) have been imported. {1} Item(s) were faulty and importing was impossible.", importcount, errcount));
            }
        }

        //Edit and Delete group
        public void EditGroup(string groupname, ImageList imageList2, bool delete = false)
        {
            for (int index = DBGroups.Count - 1; index >= 0; index--)
            {
                if (DBGroups[index].Group.ToLower() == groupname.ToLower() && DBGroups[index].GetGroupDefinitionFlag() && !DBGroups[index].GetDeletedFlag())
                {
                    if (delete) //delete item(s)
                    {
                        if (!this.UnlockDB(check: true)) return;

                        DBGroups[index].DeleteItem();                                                

                        GUI.updateLog(2, string.Format("Group definition \"{0}\" has been deleted.", groupname));

                        WatchChangesDB.Changed = true;
                    }
                    else
                    {
                        if (!this.UnlockDB(check: true)) return;

                        //open the edit entry dialog
                        Editentry edititem = new Editentry();
                        edititem.Itemtype = SecItem.ItemType.Group;
                        edititem.NewItem = false;
                        edititem.Filename = Filename;
                        edititem.GroupsList = GetListOfGroups(DBGroups);
                        edititem.DBEntries = DBGroups;
                        edititem.Index = (Int16)index;
                        edititem.PWManager = PWManager;
                        edititem.DBwatcher = WatchChangesDB;
                        edititem.Symbols = imageList2;
                        edititem.ShowDialog();
                    }
                }

            }

            m_groups.Clear();            

            this.RenumberGroupPosition();


        }

        //Move group
        public enum MoveDirection { Up, Down };
        public void MoveGroup(String groupname, MoveDirection direction)
        {
            //search source index
            Int16 index = 0;
            bool found = false;
            foreach (SecItem entry in DBGroups)
            {
                if (entry.Group == groupname && entry.GetGroupDefinitionFlag() && !entry.GetDeletedFlag())
                {
                    found = true;
                    break;
                }
                index++;
            }

            if (!found) return;
            found = false;

            //Move
            if (direction == MoveDirection.Up && index > 0)
            {
                SecItem movegroup = DBGroups[index];
                DBGroups.RemoveAt(index);
                DBGroups.Insert(index - 1, movegroup);
            }
            else if (direction == MoveDirection.Down && index < DBGroups.Count - 1)
            {
                SecItem movegroup = DBGroups[index];
                DBGroups.RemoveAt(index);
                DBGroups.Insert(index + 1, movegroup);
            }
            else
            {             
                
                return;
            }

            //afterwork
            WatchChangesDB.Changed = true;

            

            this.RenumberGroupPosition();
        }

        //renumbering of groupposition
        private void RenumberGroupPosition()
        {
            bool groupitemschanged = false;

            for (int index = DBEntries.Count - 1; index >= 0; index--)
            {
                if (DBEntries[index].GetGroupDefinitionFlag() && !DBEntries[index].GetDeletedFlag())
                {
                    int oldpos = -1;

                    if (DBEntries[index].Url != "") oldpos = Convert.ToInt16(DBEntries[index].Url);

                    DBEntries[index].Url = index.ToString(); //store position in database for group items

                    if (oldpos != index) groupitemschanged = true;
                    if (groupitemschanged) DBEntries[index].Latest = DateTime.UtcNow;
                }

            }

        }

        //This timer will delete the clipboard after copying a password to it.
        private void initializeKillClipboardTimer()
        {            
            Killclipboard.Enabled = true;
            Killclipboard.Stop();
            Killclipboard.Interval = 1000;
            Killclipboard.Tick += new EventHandler(killclipboard_tick);
        }

        #endregion

        #region static functions
        //combining groups and items
        private static void RecombineGroups(List<SecItem> dbentries, List<SecItem> dbgroups)
        {
            for (int groupindex = dbgroups.Count - 1; groupindex >= 0; groupindex--)
            {
                dbentries.Insert(0, dbgroups[groupindex]);
            }
        }

        //separate groups from items
        private static void SeparateGroups(List<SecItem> dbentries, List<SecItem> dbgroups)
        {
            dbgroups.Clear();
            for (int groupindex = dbentries.Count - 1; groupindex >= 0; groupindex--)
            {                
                if (dbentries[groupindex].GetGroupDefinitionFlag() && !dbentries[groupindex].GetDeletedFlag())
                {
                    dbgroups.Insert(0, dbentries[groupindex]);
                    dbentries.RemoveAt(groupindex);
                }
            }
        }

        //get list of groups
        public static List<string> GetListOfGroups(List<SecItem> dbgroups)
        {
            List<string> groups = new List<string>();
            foreach (SecItem groupitem in dbgroups)
            {
                //if (groupitem.GetAttributeNode("groupdefinitionflag") != null && groupitem.GetAttributeNode("deleted") == null)
                if (groupitem.GetGroupDefinitionFlag() && !groupitem.GetDeletedFlag())
                {
                    groups.Add(groupitem.Group);
                }
            }
            return groups;
        }

        //check for unique Group Name
        public static bool IsUniqueGroupName(String newname, List<string> groups)
        {
            return IsUniqueGroupName(newname, groups, "", false);
        }
        public static bool IsUniqueGroupName(String newname, List<string> groups, string oldname, bool isrenamed)
        {
            foreach (String existingGroup in groups)
            {
                if ((newname == existingGroup && !isrenamed) ||
                    (newname == existingGroup && newname != oldname && isrenamed))
                {
                    return false;
                }
            }

            return true;
        }

        //search for unique name in database
        public static string searchUniqueName(String initialname, List<SecItem> entries)
        {
            int i = 0;
            string newname = initialname;

            bool uniquename = false;
            while (!uniquename)
            {
                uniquename = true;
                for (int ie = entries.Count - 1; ie >= 0; ie--)
                {
                    //XmlElement entry = (XmlElement)entries[ie];
                    if (entries[ie].Name.ToLower() == newname.ToLower())
                    {
                        if (entries[ie].GetDeletedFlag())
                        {
                            //entries.RemoveAt(ie);
                            entries[ie].RemoveDeletedFlag();
                            break; //a deleted item can be overwritten but the "deleted" attribute must be removed                            
                        }

                        i++;
                        newname = string.Format("{0} ({1})", initialname, i);
                        uniquename = false;
                        break;
                    }
                }

            }
            return newname;
        }
        #endregion

        #region events

        //The PWHandler Event: PW is locked / unlocked
        private void PWHandler_Event(object sender, PWHandler.PWHandlerEventArgs e)
        {
            
        }

        //the event that something changed in the database
        private void DBChanged_Event(object sender, EventArgs e)
        {
            
        }

        //delete clipboard after copying a password to it
        public void killclipboard_tick(object sender, EventArgs e)
        {
            Killclipboardtimerexpired--;

            if (Killclipboardtimerexpired <= 0)
            {
                Clipboard.SetText("-");
                Killclipboard.Stop();                
            }            
        }

        #endregion

        

    }
    

}
