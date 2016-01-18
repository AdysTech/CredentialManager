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

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="Target">A descriptive text for where teh credentials being asked are used for</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string Target)
        {
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            if ( !PromptForCredentials (Target, out username, out passwd, out domain) )
                return null;
            return new NetworkCredential (username, passwd, domain);
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="Target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="Message">A brief message to display in the dialog box</param>
        /// <param name="Caption">Title for the dialog box</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
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



        /// <summary>
        /// Saves teh given Network Credential into Windows Credential store
        /// </summary>
        /// <param name="Target">Name of the application/Url where the credential is used for</param>
        /// <param name="credential">Credential to store</param>
        /// <returns>True:Success, False:Failure</returns>
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


        /// <summary>
        /// Extract the stored credential from WIndows Credential store
        /// </summary>
        /// <param name="Target">Name of the application/Url where the credential is used for</param>
        /// <returns>null if target not found, else stored credentials</returns>
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
                    var ret1 = NativeStructs.CredUIParseUserName (user, userBuilder, int.MaxValue, domainBuilder, int.MaxValue);
                    lastError = Marshal.GetLastWin32Error ();

                    //assuming invalid account name to be not meeting condition for CredUIParseUserName 
                    //"The name must be in UPN or down-level format, or a certificate"
                    if ( ret1 == NativeStructs.CredentialUIReturnCodes.ERROR_INVALID_ACCOUNT_NAME )
                        userBuilder.Append (user);
                    else if ( (uint) ret1 > 0 )
                        throw new Win32Exception (lastError, "CredUIParseUserName throw an error");

                    username = userBuilder.ToString ();
                    domain = domainBuilder.ToString ();
                    return new NetworkCredential (username, passwd, domain);
                }
            }
            return null;
        }


        /// <summary>
        /// Remove stored credentials from windows credential store
        /// </summary>
        /// <param name="Target">Name of the application/Url where the credential is used for</param>
        /// <returns>True: Success, False: Failure</returns>
        public static bool RemoveCredentials(string Target)
        {
            // Make the API call using the P/Invoke signature
            var ret = NativeStructs.CredDelete (Target, NativeStructs.CredentialType.GENERIC, 0);
            int lastError = Marshal.GetLastWin32Error ();
            if ( !ret )
                throw new Win32Exception (lastError, "CredDelete throw an error");
            return ret;
        }

        /// <summary>
        /// Generates a string that can be used for "Auth" headers in web requests, "username:password" encoded in Base64
        /// </summary>
        /// <param name="cred"></param>
        /// <returns></returns>
        public static string GetBasicAuthString(this NetworkCredential cred)
        {
            byte[] credentialBuffer = new UTF8Encoding ().GetBytes (cred.UserName + ":" + cred.Password);
            return Convert.ToBase64String (credentialBuffer);
        }
    }
}

