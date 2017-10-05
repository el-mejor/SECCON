using Dropbox.Api;
using Dropbox.Api.Files;
using SEC2ON.LBSecconBusinessLogic.Classes;
using SEC2ON.LBSecconBusinessLogic.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Http;
using System.Security.Cryptography;

namespace SEC2ON.LBSecconBusinessLogic
{
    #region Dropbox functionalities
    public partial class SecconBL
    {
        #region fields
        private string m_loginToken;
        private string m_loginUid;
        private DropboxClient m_dbclient;
        #endregion
        
        //Download database from dropbox
        public async Task dropboxDownloadDB()
        {
            //Get a list of available databases on the dropbox
            List<OnlineDatabase> onlineDBs = new List<OnlineDatabase>();
            try
            {
                var dbclient = getdropboxClient();

                var m = await dbclient.Files.ListFolderAsync("");

                //Gain all files in the directory, they will be distinguished in OnlineDatabase-Class
                foreach (var item in m.Entries.Where(c => c.IsFile))
                {
                    OnlineDatabase newdb = new OnlineDatabase(item.AsFile.Name, item.AsFile.Size.ToString(), item.AsFile.ServerModified);
                    onlineDBs.Add(newdb);
                }
            }
            catch (DropboxException ex)
            {
                GUI.updateLog(4, String.Format("There was an error while getting a list of available databases: {0}", ex.Message));
                return;
            }

            //Open SelectOnlineDB dialog to choose a database for downloading
            SelectOnlineDB selectdb = new SelectOnlineDB(onlineDBs);
            selectdb.ShowDialog();

            if (selectdb.DialogResult == DialogResult.Cancel)
            {
                GUI.updateLog(2, "Action cancelled before downloading.");
                return;
            }

            //Asking for destination folder
            SaveFileDialog newdbname = new SaveFileDialog();
            newdbname.AddExtension = true;
            newdbname.Filter = "Seccon Database (*.sdb)|*.sdb";
            newdbname.DefaultExt = "SDB";
            newdbname.FileName = selectdb.SelectedDB;

            if (newdbname.ShowDialog() != DialogResult.OK)
            {
                GUI.updateLog(2, "Action cancelled before downloading.");
                return;
            }

            //close current database
            if (!this.closeDatabase()) return;

            //download database
            Filename = newdbname.FileName;

            //Save database
            if (await dropboxFileOperations(selectdb.SelectedDB, Filename, dropboxFileOperation.Download))
            {
                //Open database after downloading
                this.open_database(Filename);
            }

            return;
        }

        //Synchronize with dropbox
        public async Task dropboxSynchronizeDB()
        {
            //precondition: check if database is unlocked
            if (!this.UnlockDB(check: true)) return;

            //precondition: check if database is saved
            if (!CheckIfDBIsSavedForSync()) return;

            GUI.updateLog(2, "Wait for dropbox...");
            Application.DoEvents();
            try
            {
                //precondition: check if there's a version of the database on the dropbox, if not just copy it there
                var dbclient = getdropboxClient();

                var m = await dbclient.Files.ListFolderAsync("");

                if (!m.Entries.Any(c => c.IsFile && c.Name == Path.GetFileName(Filename)))
                {
                    DialogResult notExistentUpload = MessageBox.Show("There is no version of your database online. Would you upload a copy?", "Synchronize with dropbox...", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (notExistentUpload == DialogResult.Yes)
                    {
                        if (await dropboxFileOperations(Path.GetFileName(Filename), dropboxFileOperation.Upload))
                        {
                            GUI.updateLog(2, "There was no version of your database online but a copy was uploaded.");
                            return;
                        }
                        else
                        {
                            GUI.updateLog(4, "There was no version of your database online. Uploading a copy of your database failed.");
                            return;
                        }
                    }
                    else
                    {
                        GUI.updateLog(2, "There was no version of your database online, action was cancelled.");
                        return;
                    }

                }
            }
            catch (Exception ex)
            {
                GUI.updateLog(4, string.Format("There was an error while login to your dropbox: {0}", ex.Message));
                return;
            }

            //All preconditions are meeting - begin to synchronize with online version
            GUI.updateLog(5, string.Format("Synchronizing database..."));

            Application.DoEvents();

            //store a temporary copy of the dropbox version locally
            if (!await dropboxFileOperations(Path.GetFileName(Filename), Path.Combine(Application.LocalUserAppDataPath, Properties.Resources.TempDBFile), dropboxFileOperation.Download))
            {
                return;
            }

            // BEGIN OF SYNCHRONIZATION
            int updated = 0;
            int updatedpgp = 0;
            SynchronizeDatabasesResult result = this.synchronizeDatabases(Path.Combine(Application.LocalUserAppDataPath, Properties.Resources.TempDBFile), ref updated, ref updatedpgp);
            if (result == SynchronizeDatabasesResult.OverwriteTarget)
            {
                if (await dropboxFileOperations(Path.GetFileName(Filename), dropboxFileOperation.Upload))
                {
                    GUI.updateLog(2, "The online version of the database was overwritten by the local database. A backup copy was created on the dropbox.");
                    return;
                }
                else
                    GUI.updateLog(4, "While uploading the database something went wrong.");
                return;
            }
            if (result == SynchronizeDatabasesResult.Cancelled)
                return;
            // END OF SYNCHRONIZATION

            //delete the temporary database
            File.Delete(Path.Combine(Application.LocalUserAppDataPath, Properties.Resources.TempDBFile));

            //Save the local database and upload a copy to the dropbox
            if (!this.savedatabase())
            {
                GUI.updateLog(4, string.Format("Cannot save the databse locally. The database on your dropbox keeps untouched. Please try again.", updated, updatedpgp));
                return;
            }

            GUI.updateLog(5, string.Format("Upload database..."));

            Application.DoEvents();

            if (!await this.dropboxFileOperations(Filename, dropboxFileOperation.Upload))
            {
                //write no further error text to the toolstrip since the details were written by dropboxFileOperations()
                DialogResult res = MessageBox.Show("Saving the database to the dropbox failed. Please try again.", "Synchronize database...", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Report if there were updated items
            if (updated == 0 && updatedpgp == 0) GUI.updateLog(2, string.Format("The local database is up to date.", updated, updatedpgp));
            if (updated > 0 && updatedpgp == 0) GUI.updateLog(2, string.Format("Updated items: {0}", updated, updatedpgp));
            if (updated == 0 && updatedpgp > 0) GUI.updateLog(2, string.Format("Updated PGP-Keys: {1}", updated, updatedpgp));
            if (updated > 0 && updatedpgp > 0) GUI.updateLog(2, string.Format("Updated items: {0}, updated PGP-Keys: {1}", updated, updatedpgp));
        }

        //Check if dropbox is connected, if not start connecting process
        private bool dropboxCheckLogin()
        {
            if (!this.dropboxLoadUserLogin())
            {
                //login to dropbox is needed first
                GUI.updateLog(3, "Dropbox not connected, must be connected first...");
                if (!this.dropboxAuthentification())
                {
                    GUI.updateLog(4, "Dropbox not connected. Action was cancelled or an error occured.");
                    return false;
                }
            }
            return true;
        }

        //Up- or downloading files to or from the dropbox
        enum dropboxFileOperation { Download, Upload, Delete, Copy };
        private async Task<bool> dropboxFileOperations(string dbname, dropboxFileOperation fileop)
        {
            return await dropboxFileOperations(dbname, dbname, fileop);
        }
        private async Task<bool> dropboxFileOperations(string dbname, string destname, dropboxFileOperation fileop)
        {
            try
            {
                var dbclient = getdropboxClient();

                #region delete
                //delete database
                if (fileop == dropboxFileOperation.Delete)
                {
                    var m = await dbclient.Files.ListFolderAsync("");

                    if (m.Entries.Any(c => c.IsFile && c.Name == Path.GetFileName(dbname)))
                    {
                        try
                        {
                            await dbclient.Files.DeleteV2Async("/" + dbname);
                        }
                        catch (DropboxException ex)
                        {
                            GUI.updateLog(4, string.Format("Cannot delete online database: {0}", ex.Message));
                            return false;
                        }
                    }
                    return true;
                }
                #endregion

                #region copy
                //copy database
                if (fileop == dropboxFileOperation.Copy)
                {
                    if (dbname == destname)
                    {
                        GUI.updateLog(4, string.Format("Source and destination filenames are equal. This is a program error!"));
                        return false;
                    }
                    try
                    {
                        await dbclient.Files.CopyV2Async("/" + dbname, "/" + destname);
                    }
                    catch (DropboxException ex)
                    {
                        GUI.updateLog(4, string.Format("Cannot make a backup of the database: {0}", ex.Message));
                        return false;
                    }
                    return true;
                }
                #endregion

                #region download
                //load database
                if (fileop == dropboxFileOperation.Download)
                {
                    try
                    {
                        var file = await dbclient.Files.DownloadAsync("/" + Path.GetFileName(dbname));

                        using (FileStream dbfile = new FileStream(destname, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            var fileBytes = await file.GetContentAsByteArrayAsync();

                            dbfile.Write(fileBytes, 0, fileBytes.Length);
                        }
                    }
                    catch (DropboxException ex)
                    {
                        GUI.updateLog(4, string.Format("There was an error downloading the database: {0}", ex.Message));
                        return false;
                    }
                    catch (Exception ex)
                    {
                        GUI.updateLog(4, string.Format("There was an error saving the database locally: {0}", ex.Message));
                        return false;
                    }
                    return true;
                }
                #endregion

                #region upload
                //save database
                if (fileop == dropboxFileOperation.Upload)
                {
                    //make a backup of the dropbox database first
                    //delete old backup (ignore if there's n othing to delete) 
                    //and make a new backup of the online database (claim if it is not possible except there's nothing to make a backup from)
                    var m = await dbclient.Files.ListFolderAsync("");

                    if (m.Entries.Any(c => c.IsFile && c.Name == Path.GetFileName(dbname)))
                    {
                        string backupFileName = Path.GetFileName(dbname).Replace(".sdb", Properties.Resources.ExtensionBackup);

                        bool deletebackup = await dropboxFileOperations(backupFileName, dropboxFileOperation.Delete);
                        bool copydatabase = await dropboxFileOperations(Path.GetFileName(dbname), backupFileName, dropboxFileOperation.Copy);
                        if (!deletebackup || !copydatabase)
                        {
                            DialogResult proceedWithoutBackup = MessageBox.Show("Making a copy of the online version of the database failed. Would you proceed without making a copy the online version?",
                                "Uploading database to dropbox...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                            if (proceedWithoutBackup == DialogResult.No) return false;
                        }
                    }

                    try
                    {
                        //save actual database to dropbox
                        using (MemoryStream f = new MemoryStream())
                        {
                            using (FileStream dbfile = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                //dbfile.CopyTo(f);

                                await dbclient.Files.UploadAsync("/" + Path.GetFileName(dbname), WriteMode.Overwrite.Instance, body: dbfile);
                            }
                        }
                    }
                    catch (DropboxException ex)
                    {
                        GUI.updateLog(4, string.Format("There was an error uploading the database: {0}", ex.Message));
                        return false;
                    }
                    catch
                    {
                        GUI.updateLog(4, string.Format("There was an error accessing the dropbox database locally."));
                        return false;
                    }
                    return true;
                }
                #endregion
            }
            catch (DropboxException ex)
            {
                GUI.updateLog(4, string.Format("There was an error while accessing your dropbox: {0}", ex.Message));
                return false;
            }
            catch (Exception ex)
            {
                GUI.updateLog(4, string.Format("There was an error while login to your dropbox: {0}", ex.Message));
                return false;
            }

            GUI.updateLog(4, "dropboxFileOperations was called with an unknown action... internal error - check program.");
            return false;
        }

        //check for dropboxuserlogin and load it
        private bool dropboxLoadUserLogin()
        {
            if (File.Exists(string.Format(Path.Combine(Application.LocalUserAppDataPath, Path.Combine(Application.LocalUserAppDataPath, "dbuser_{0}")), System.Security.Principal.WindowsIdentity.GetCurrent().Name.GetHashCode().ToString())))
            {
                try
                {
                    using (StreamReader dbfile = new StreamReader(string.Format(Path.Combine(Application.LocalUserAppDataPath, "dbuser_{0}"), System.Security.Principal.WindowsIdentity.GetCurrent().Name.GetHashCode().ToString())))
                    {
                        byte[] enctoken = Convert.FromBase64String(dbfile.ReadLine());
                        byte[] encuid = Convert.FromBase64String(dbfile.ReadLine());
                        byte[] token = ProtectedData.Unprotect(enctoken, null, DataProtectionScope.CurrentUser);
                        byte[] uid = ProtectedData.Unprotect(encuid, null, DataProtectionScope.CurrentUser);

                        m_loginToken = Encoding.UTF8.GetString(token);
                        m_loginUid = Encoding.UTF8.GetString(uid);

                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
                return false;
        }

        //register to dropbox and get authentification
        public bool dropboxAuthentification()
        {
            try
            {
                var login = new LoginForm(Properties.Settings.Default.dropboxAppKey);

                login.ShowDialog();

                if (login.Result)
                {
                    dropboxStoreLogin(login.AccessToken, login.Uid);
                    GUI.updateLog(2, string.Format("Dropbox connected succesfully."));
                    return true;
                }
                else
                    return false;
            }
            catch (DropboxException ex)
            {
                GUI.updateLog(4, string.Format("There was an error while login to your dropbox: {0}", ex.Message));
                return false;
            }
            catch (Exception ex)
            {
                GUI.updateLog(4, string.Format("There was an error while login to your dropbox: {0}", ex.Message));
                return false;
            }
        }

        //save dropboxlogin
        private void dropboxStoreLogin(string Token, string Uid)
        {
            try
            {
                using (StreamWriter dbfile = new StreamWriter(string.Format(Path.Combine(Application.LocalUserAppDataPath, "dbuser_{0}"), System.Security.Principal.WindowsIdentity.GetCurrent().Name.GetHashCode().ToString())))
                {
                    byte[] token = Encoding.UTF8.GetBytes(Token);
                    byte[] enctoken = ProtectedData.Protect(token, null, DataProtectionScope.CurrentUser);
                    byte[] uid = Encoding.UTF8.GetBytes(Uid);
                    byte[] encuid = ProtectedData.Protect(uid, null, DataProtectionScope.CurrentUser);

                    dbfile.WriteLine(Convert.ToBase64String(enctoken));
                    dbfile.WriteLine(Convert.ToBase64String(encuid));
                }
            }
            catch { }
            return;
        }

        //get DropBoxConfigClient
        private DropboxClient getdropboxClient()
        {
            if (m_dbclient != null)
                return m_dbclient;

            //precondition: check if dropbox is connected
            if (!dropboxCheckLogin())
                throw new Exception("Dropbox not connected. Connect to your Dropbox account first.");

            // Specify socket level timeout which decides maximum waiting time when no bytes are
            // received by the socket.
            var httpClient = new HttpClient(new WebRequestHandler { ReadWriteTimeout = 10 * 1000 })
            {
                // Specify request level timeout which decides maximum time that can be spent on
                // download/upload files.
                Timeout = TimeSpan.FromMinutes(20)
            };

            var client = new DropboxClientConfig("Seccon")
            {
                HttpClient = httpClient
            };

            m_dbclient = new DropboxClient(m_loginToken, client);

            return m_dbclient;
        }
        
    }
    #endregion
}
