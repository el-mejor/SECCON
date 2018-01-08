using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace LBPasswordAndCryptoServices
{
    //Handling the password and creates key and IV
    #region PWHandler

    //Handles the User password: It stores the pw encrypted and provides the SHA Hash for verification. 
    //After three minutes the pw will be overwritten by null
    public class PWHandler
    {
        private byte[] m_passwordHash = null; //the password hash   
        private byte[] m_pinhash = null; //the pin hash
        private byte[] m_pwkeylow = null; //the encrypted key (low)
        private byte[] m_pwIVlow = null; //the encrypted IV (low)
        private byte[] m_pwkeyhigh = null; //the encrypted key (high)
        private byte[] m_pwIVhigh = null; //the encrypted IV (high)
        private Int16 m_wrongpincount = 3;
        private Timer m_timeout = new Timer();
        bool m_locked = true;

        public String PWHash
        {
            get
            {
                if (m_passwordHash == null) return null;
                //return System.Text.Encoding.UTF8.GetString(ProtectedData.Unprotect(m_passwordHash, null, DataProtectionScope.CurrentUser)); 
                return System.Text.Encoding.UTF8.GetString(m_passwordHash);
            }
        }

        public Byte[] SetPin
        {
            set
            {
                if (value == null || value.Length == 0) m_pinhash = null;
                else
                {
                    //compute hash and store it
                    SHA256 pinhash = new SHA256Managed();
                    m_pinhash = pinhash.ComputeHash(value);
                    PWHandlerEventArgs e;
                    if(m_pinhash != null) e = new PWHandlerEventArgs(PWHandlerEventArgs.PWHandlerEventEnum.None, PWHandlerEventArgs.PWHandlerLockReasonEnum.PinActivated, true);
                    else e = new PWHandlerEventArgs(PWHandlerEventArgs.PWHandlerEventEnum.None, PWHandlerEventArgs.PWHandlerLockReasonEnum.None, true);
                    this.m_PWHandlerEvent(e);
                }
            }
        }

        public bool Locked
        { get { return m_locked; } }

        public Int16 WrongPinCount
        {
            get { return m_wrongpincount; }
        }

        public PWHandler(int interval)
        {
            //initialize timer for timeout of current session
            m_timeout.Interval = interval;
            m_timeout.Enabled = true;
            m_timeout.Stop();
            m_timeout.Tick += new EventHandler(timeout_pw_session);
        }
        public PWHandler()
        {
            //no timer is activated, pwhandler will not be locked
            m_timeout.Interval = 180000;  //dummy parameters since the timer can be started manually with ResetTimeout()
            m_timeout.Enabled = false;
            m_timeout.Stop();
            m_timeout.Tick += new EventHandler(timeout_pw_session);            
        }

        //Resetting the timeout timer
        public void ResetTimeout()
        {
            m_timeout.Stop();
            m_timeout.Start();
        }
        public void ResetTimeout(int interval)
        {
            m_timeout.Interval = interval;
            this.ResetTimeout();
        }

        //returns the current passwords key
        public byte[] CurrentPWKey(bool high = false)
        {
            if (m_locked) return null;
            this.ResetTimeout();

            if (high)
            {
                return ProtectedData.Unprotect(m_pwkeyhigh, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                return ProtectedData.Unprotect(m_pwkeylow, null, DataProtectionScope.CurrentUser);
            }
        }

        //returns the current passwords IV
        public byte[] CurrentPWIV(bool high = false)
        {
            if (m_locked) return null;
            this.ResetTimeout();

            if (high)
            {
                return ProtectedData.Unprotect(m_pwIVhigh, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                return ProtectedData.Unprotect(m_pwIVlow, null, DataProtectionScope.CurrentUser);
            }
        }

        //Unlock the password manager by giving a password or the session pin, 
        //store the password hash encrypted with the current user information
        //store key and IV encrypted with the current user information
        //trigger unlock event
        //public void Unlock(Byte[] password, bool IsPin = false)
        //{
        //    if (!IsPin) this.UnlockPW(password);
        //    if (IsPin) this.UnlockPin(password);
        //    password = null;
        //}        
        public void UnlockPin(Byte[] pin)
        {
            if (m_wrongpincount == 0 || m_pinhash == null) return;

            SHA256 pinhash = new SHA256Managed();
            byte[] currentpinhash = pinhash.ComputeHash(pin);
            pin = null;
            pinhash.Dispose();

            if (System.Text.Encoding.ASCII.GetString(currentpinhash) != System.Text.Encoding.ASCII.GetString(m_pinhash))
            {
                m_wrongpincount--;
                if (m_wrongpincount == 0)
                {
                    m_pinhash = null;
                    this.Lock(PWHandlerEventArgs.PWHandlerLockReasonEnum.PinInvalidated);
                }
                return; //return without unlocking if pin was wrong
            }
            m_wrongpincount = 3;

            this.UnlockFinal(PWHandlerEventArgs.PWHandlerEventEnum.UnlockByPin);

        }
        public void UnlockPW(Byte[] password)
        {
            //compute hash and store it
            SHA256 pwhash = new SHA256Managed();
            m_passwordHash = pwhash.ComputeHash(password);

            //encrypt keys / IVs
            m_pwkeylow = ProtectedData.Protect(new PasswordDeriveBytes(password, new byte[] { 0x56 }).GetBytes(32), null, DataProtectionScope.CurrentUser);
            m_pwkeyhigh = ProtectedData.Protect(new PasswordDeriveBytes(password, new byte[] { 0x56 }, "SHA256", 5000).GetBytes(32), null, DataProtectionScope.CurrentUser);

            m_pwIVlow = ProtectedData.Protect(new PasswordDeriveBytes(password, new byte[] { 0x56 }).GetBytes(16), null, DataProtectionScope.CurrentUser);
            m_pwIVhigh = ProtectedData.Protect(new PasswordDeriveBytes(password, new byte[] { 0x56 }, "SHA256", 5000).GetBytes(16), null, DataProtectionScope.CurrentUser); ;

            pwhash.Dispose();

            password = null;

            this.UnlockFinal(PWHandlerEventArgs.PWHandlerEventEnum.UnlockByKey);
        }
        private void UnlockFinal(PWHandlerEventArgs.PWHandlerEventEnum unlock)
        {
            //activate timer
            m_timeout.Start();

            m_locked = false;

            bool pinstate = false;
            if (m_pinhash != null) pinstate = true;

            PWHandlerEventArgs e = new PWHandlerEventArgs(unlock, PWHandlerEventArgs.PWHandlerLockReasonEnum.User, pinstate);
            this.m_PWHandlerEvent(e);
        }

        //the session timed out - forward to lock methode
        private void timeout_pw_session(object sender, EventArgs e)
        {
            m_timeout.Stop();
            Lock(PWHandlerEventArgs.PWHandlerLockReasonEnum.Timeout);
        }

        //lock pw manager forget the keys and trigger event
        public void Lock(PWHandlerEventArgs.PWHandlerLockReasonEnum reason)
        {
            m_locked = true;
            m_timeout.Stop();
            if (m_pinhash == null || reason == PWHandlerEventArgs.PWHandlerLockReasonEnum.CloseDB)
            {
                PWHandler.ClearByte(m_pwkeyhigh);
                m_pwkeyhigh = null;
                PWHandler.ClearByte(m_pwkeylow);
                m_pwkeylow = null;
                PWHandler.ClearByte(m_pwIVhigh);
                m_pwIVhigh = null;
                PWHandler.ClearByte(m_pwIVlow);
                m_pwIVlow = null;
                PWHandler.ClearByte(m_pinhash);
                m_pinhash = null;
            }            

            bool pinstate = false;
            if (m_pinhash != null) pinstate = true;

            PWHandlerEventArgs e = new PWHandlerEventArgs(PWHandlerEventArgs.PWHandlerEventEnum.Lock, reason, pinstate);
            if (reason == PWHandlerEventArgs.PWHandlerLockReasonEnum.Internal) e = new PWHandlerEventArgs(PWHandlerEventArgs.PWHandlerEventEnum.Internal, reason, pinstate);
            if (reason == PWHandlerEventArgs.PWHandlerLockReasonEnum.WrongPW) e = new PWHandlerEventArgs(PWHandlerEventArgs.PWHandlerEventEnum.WrongPW, reason, pinstate);
            this.m_PWHandlerEvent(e);
        }

        //clear a byte array if not null
        public static void ClearByte(byte[] toclear)
        {
            if (toclear != null) Array.Clear(toclear, 0, toclear.Length);            
        }

        //Lock event handler
        public delegate void PWEventHandler(object sender, PWHandlerEventArgs e);
        public event PWEventHandler PWHandlerEvent;
        public void m_PWHandlerEvent(PWHandlerEventArgs e)
        {
            if (this.PWHandlerEvent != null) this.PWHandlerEvent.Invoke(this, e);
        }

        //Event class for the pwhandler
        public class PWHandlerEventArgs : EventArgs
        {
            public enum PWHandlerEventEnum { None, UnlockByKey, UnlockByPin, Lock, Internal, WrongPW };
            public enum PWHandlerLockReasonEnum { Timeout, CloseDB, WrongPW, User, None, PinInvalidated, PinActivated, Internal };            
            public PWHandlerEventEnum Event
            { get; set; }
            public PWHandlerLockReasonEnum Reason
            { get; set; }
            public bool PinState
            { get; set; }

            public PWHandlerEventArgs()
            {
            }
            public PWHandlerEventArgs(PWHandlerEventEnum CurrentEvent, PWHandlerLockReasonEnum CurrentReason, bool pinstate)
            {
                Event = CurrentEvent;
                Reason = CurrentReason;
                PinState = pinstate;
            }
        }
    }
    #endregion

    //Cryptographic functions
    #region Cryptoservice
    //class for encrypt and decrypt data
    public static class cryptoservice
    {
        //encrypt the input byte by giving the pwhandler object
        public static String encrypt(byte[] input, PWHandler pwm)
        {
            byte[] key = pwm.CurrentPWKey();
            byte[] IV = pwm.CurrentPWIV();

            return encrypt(input, key, IV);
        }

        //encrypt the input byte by giving key and iv
        public static String encrypt(byte[] byteinput, byte[] key, byte[] IV)
        {
            MemoryStream ms = new MemoryStream();

            Rijndael enc = Rijndael.Create();
            enc.KeySize = 256;
            enc.Mode = CipherMode.CBC;

            enc.Key = key;
            enc.IV = IV;

            CryptoStream cs = new CryptoStream(ms, enc.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(byteinput, 0, byteinput.Length);
            cs.Close();

            enc.Clear();
            enc.Dispose();
            PWHandler.ClearByte(byteinput);
            PWHandler.ClearByte(IV);
            PWHandler.ClearByte(key);

            return Convert.ToBase64String(ms.ToArray());
        }

        //encrypt the input stream by giving the pwhandler
        public static MemoryStream fileencrypt(MemoryStream m_input, PWHandler pwm)
        {
            byte[] m_key = pwm.CurrentPWKey();
            byte[] m_IV = pwm.CurrentPWIV();

            return fileencrypt(m_input, m_key, m_IV);
        }

        //encrypt the input stream by giving the key and iv
        public static MemoryStream fileencrypt(MemoryStream m_input, byte[] m_key, byte[] m_IV)
        {
            MemoryStream ms = new MemoryStream();

            byte[] input = m_input.ToArray();

            Rijndael enc = Rijndael.Create();
            enc.KeySize = 256;
            enc.Mode = CipherMode.CBC;

            enc.Key = m_key;
            enc.IV = m_IV;

            CryptoStream cs = new CryptoStream(ms, enc.CreateEncryptor(), CryptoStreamMode.Write);

            cs.Write(input, 0, input.Length);
            cs.Close();

            enc.Clear();
            enc.Dispose();
            m_input = null;
            m_IV = null;
            m_key = null;

            return ms;
        }

        //encrypt the input stream by using PGP
        public static void PGPencrypt(String m_input, String keystring, string targetfile)
        {
            if (string.IsNullOrEmpty(keystring))
            {
                throw new Exception("No key was given.");                
            }

            using (StreamWriter x = new StreamWriter(targetfile))
            {
                x.Write(Environment.NewLine);
                x.Write(m_input);
            }

            byte[] input = System.Text.Encoding.UTF8.GetBytes(m_input);

            PgpPublicKey key = null;

            MemoryStream keyIn = new MemoryStream(ASCIIEncoding.UTF8.GetBytes(keystring));
            using (Stream inputStream = PgpUtilities.GetDecoderStream(keyIn))
            {
                PgpPublicKeyRing bla = new PgpPublicKeyRing(inputStream);
                //PgpPublicKeyRingBundle publicKeyRingBundle = new PgpPublicKeyRingBundle(inputStream);                

                var x = bla.GetPublicKeys();
                //var x = publicKeyRingBundle.GetKeyRings();                

                foreach (PgpPublicKey kRing in x)
                {
                    if (kRing.IsEncryptionKey & !kRing.IsMasterKey & !kRing.IsRevoked())
                    {
                        key = kRing;
                        break; 
                    }
                }
            }

            MemoryStream f = new MemoryStream();

            PGPEncryptFile(f, targetfile, key, true, true);
            using (FileStream file = new FileStream(targetfile, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                file.Write(f.ToArray(), 0, f.ToArray().Length);
            }

        }

        //Create and save PGP File
        private static void PGPEncryptFile(
        Stream outputStream,
        string fileName,
        PgpPublicKey pubkey,
        bool armor,
        bool withIntegrityCheck)
        {
            if (armor)
            {
                outputStream = new ArmoredOutputStream(outputStream);
            }

            MemoryStream bOut = new MemoryStream();

            PgpCompressedDataGenerator comData = new PgpCompressedDataGenerator(CompressionAlgorithmTag.Zip);

            PgpUtilities.WriteFileToLiteralData(comData.Open(bOut), PgpLiteralData.Text, new FileInfo(fileName), new byte[2048]);

            comData.Close();

            byte[] bytes = bOut.ToArray();

            PgpEncryptedDataGenerator cPk = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Cast5, withIntegrityCheck, new SecureRandom());

            cPk.AddMethod(pubkey);

            Stream cOut = cPk.Open(outputStream, bytes.Length);

            cOut.Write(bytes, 0, bytes.Length);

            cOut.Close();

            if (armor)
            {
                outputStream.Close();
            }
        }

        //decrypt the input string by giving the pwhandler object
        public static byte[] decrypt(string input, PWHandler pwm)
        {
            byte[] key = pwm.CurrentPWKey();
            byte[] IV = pwm.CurrentPWIV();

            return decrypt(input, key, IV);
        }

        //decrypt the input string by giving key and iv
        public static byte[] decrypt(string input, byte[] key, byte[] IV)
        {
            byte[] byteinput = Convert.FromBase64String(input);

            MemoryStream ms = new MemoryStream();

            Rijndael enc = Rijndael.Create();
            enc.KeySize = 256;
            enc.Mode = CipherMode.CBC;

            enc.Key = key;
            enc.IV = IV;

            CryptoStream cs = new CryptoStream(ms, enc.CreateDecryptor(), CryptoStreamMode.Write);

            cs.Write(byteinput, 0, byteinput.Length);
            cs.Close();

            enc.Clear();
            enc.Dispose();
            PWHandler.ClearByte(IV);
            PWHandler.ClearByte(key);

            return ms.ToArray();
        }

        //decrypt the input stream by giving the pwhandler
        public static MemoryStream filedecrypt(MemoryStream m_input, PWHandler pwm)
        {
            byte[] m_key = pwm.CurrentPWKey();
            byte[] m_IV = pwm.CurrentPWIV();

            return filedecrypt(m_input, m_key, m_IV);
        }

        //decrypt the input stream by giving the key and iv
        public static MemoryStream filedecrypt(MemoryStream m_input, byte[] m_key, byte[] m_IV)
        {
            MemoryStream ms = new MemoryStream();

            byte[] input = m_input.ToArray();

            Rijndael enc = Rijndael.Create();
            enc.KeySize = 256;
            enc.Mode = CipherMode.CBC;

            enc.Key = m_key;
            enc.IV = m_IV;

            CryptoStream cs = new CryptoStream(ms, enc.CreateDecryptor(), CryptoStreamMode.Write);

            cs.Write(input, 0, input.Length);
            cs.Close();

            enc.Clear();
            enc.Dispose();
            m_input = null;
            m_IV = null;
            m_key = null;

            return ms;
        }
    }
    #endregion

}