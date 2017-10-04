using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace SEC2ON
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {   

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new SECCONFORM(args));
            }
            catch (System.ObjectDisposedException)
            {
                //the form was closed because only a file was decrypted - so it's ok. 
            }
            
        }
    }
}
