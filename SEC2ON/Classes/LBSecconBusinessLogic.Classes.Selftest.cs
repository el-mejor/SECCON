using LBPasswordAndCryptoServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

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
}
