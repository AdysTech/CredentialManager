using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AdysTech.CredentialManager
{

    //ref: http://blogs.msdn.com/b/peerchan/archive/2005/11/01/487834.aspx

    public static class CredentialManager
    {


        public static NetworkCredential PromptForCredentials(string Target)
        {
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            if ( !PromptForCredentials (Target, out username, out passwd, out domain) )
                return null;
            return new NetworkCredential (username, passwd, domain);
        }

        public static NetworkCredential PromptForCredentials(string Target, string Message, string Caption)
        {
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            if ( !PromptForCredentials (Target, Message, Caption, out username, out passwd, out domain) )
                return null;
            return new NetworkCredential (username, passwd, domain);
        }

        internal static bool PromptForCredentials(string target, out string user, out string password, out string domain)
        {
            return PromptForCredentials (target, new NativeStructs.CredentialUIInfo (), out user, out password, out domain);
        }

        internal static bool PromptForCredentials(string target, string Message, string Caption, out string user, out string password, out string domain)
        {
            NativeStructs.CredentialUIInfo credUI = new NativeStructs.CredentialUIInfo ();
            credUI.pszMessageText = Message;
            credUI.pszCaptionText = Caption;
            return PromptForCredentials (target, credUI, out user, out password, out domain);
        }

        private static bool PromptForCredentials(string target, NativeStructs.CredentialUIInfo credUI, out string user, out string password, out string domain)
        {
            // Setup the flags and variables
            StringBuilder userPassword = new StringBuilder (), userID = new StringBuilder ();
            credUI.cbSize = Marshal.SizeOf (credUI);
            bool save = true;
            NativeStructs.CredentialUIFlags flags = NativeStructs.CredentialUIFlags.COMPLETE_USERNAME | NativeStructs.CredentialUIFlags.PERSIST | NativeStructs.CredentialUIFlags.EXCLUDE_CERTIFICATES;

            // Prompt the user
            NativeStructs.CredentialUIReturnCodes returnCode = NativeStructs.CredUIPromptForCredentials (ref credUI, target, IntPtr.Zero, 0, userID, 100, userPassword, 100, ref save, flags);

            password = userPassword.ToString ();

            StringBuilder userBuilder = new StringBuilder ();
            StringBuilder domainBuilder = new StringBuilder ();

            returnCode = NativeStructs.CredUIParseUserName (userID.ToString (), userBuilder, int.MaxValue, domainBuilder, int.MaxValue);
            switch ( returnCode )
            {
                case NativeStructs.CredentialUIReturnCodes.NO_ERROR: // The username is valid.
                    user = userBuilder.ToString ();
                    domain = domainBuilder.ToString ();
                    return true;

                case NativeStructs.CredentialUIReturnCodes.ERROR_INVALID_ACCOUNT_NAME: // The username is not valid.
                    user = userID.ToString ();
                    domain = null;
                    return false;

                case NativeStructs.CredentialUIReturnCodes.ERROR_INSUFFICIENT_BUFFER: // One of the buffers is too small.
                    throw new OutOfMemoryException ();

                case NativeStructs.CredentialUIReturnCodes.ERROR_INVALID_PARAMETER: // ulUserMaxChars or ulDomainMaxChars is zero OR userName, user, or domain is NULL.
                    throw new ArgumentNullException ("userName");

                default:
                    user = null;
                    domain = null;
                    return false;
            }
        }




        public static bool SaveCredentials(string Target, NetworkCredential credential)
        {
            // Go ahead with what we have are stuff it into the CredMan structures.
            Credential cred = new Credential (credential);
            cred.TargetName = Target;
            cred.Persist = NativeStructs.Persistance.ENTERPRISE;
            NativeStructs.NativeCredential ncred = cred.GetNativeCredential ();
            // Write the info into the CredMan storage.
            bool written = NativeStructs.CredWrite (ref ncred, 0);
            int lastError = Marshal.GetLastWin32Error ();
            if ( written )
            {
                return true;
            }
            else
            {
                string message = string.Format ("CredWrite failed with the error code {0}.", lastError);
                throw new Exception (message);
            }
        }

        public static NetworkCredential GetCredentials(string Target)
        {
            IntPtr nCredPtr;
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            // Make the API call using the P/Invoke signature
            bool ret = NativeStructs.CredRead (Target, NativeStructs.CredentialType.GENERIC, 0, out nCredPtr);
            int lastError = Marshal.GetLastWin32Error ();
            if ( !ret )
                throw new Win32Exception (lastError, "CredDelete throw an error");

            // If the API was successful then...
            if ( ret )
            {
                using ( CriticalCredentialHandle critCred = new CriticalCredentialHandle (nCredPtr) )
                {
                    Credential cred = critCred.GetCredential ();
                    passwd = cred.CredentialBlob;
                    var user = cred.UserName;
                    StringBuilder userBuilder = new StringBuilder ();
                    StringBuilder domainBuilder = new StringBuilder ();
                    NativeStructs.CredUIParseUserName (user, userBuilder, int.MaxValue, domainBuilder, int.MaxValue);
                    username = userBuilder.ToString ();
                    domain = domainBuilder.ToString ();
                    return new NetworkCredential (username, passwd, domain);
                }
            }
            return null;
        }

        public static bool RemoveCredentials(string Target)
        {
            // Make the API call using the P/Invoke signature
            var ret = NativeStructs.CredDelete (Target, NativeStructs.CredentialType.GENERIC, 0);
            int lastError = Marshal.GetLastWin32Error ();
            if ( !ret )
                throw new Win32Exception (lastError, "CredDelete throw an error");
            return ret;
        }

        public static string GetBasicAuthString(this NetworkCredential cred)
        {
            byte[] credentialBuffer = new UTF8Encoding ().GetBytes (cred.UserName + ":" + cred.Password);
            return Convert.ToBase64String (credentialBuffer);
        }
    }
}

