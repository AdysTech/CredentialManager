using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace AdysTech.CredentialManager
{
    //ref: http://blogs.msdn.com/b/peerchan/archive/2005/11/01/487834.aspx

    public static class CredentialManager
    {
        public enum CredentialType : uint
        {
            Generic = 1,
            Windows = 2,
            CertificateBased = 3
        }

        private static bool PromptForCredentials(string target, NativeCode.CredentialUIInfo credUI, ref bool save, out string user, out string password, out string domain)
        {
            user = String.Empty;
            password = String.Empty;
            domain = String.Empty;

            // Setup the flags and variables
            credUI.cbSize = Marshal.SizeOf(credUI);
            int errorcode = 0;
            uint authPackage = 0;

            IntPtr outCredBuffer = new IntPtr();
            uint outCredSize;
            var flags = NativeCode.PromptForWindowsCredentialsFlags.GenericCredentials | 
                    NativeCode.PromptForWindowsCredentialsFlags.EnumerateCurrentUser;
            flags = save ? flags | NativeCode.PromptForWindowsCredentialsFlags.ShowCheckbox : flags;

            // Setup the flags and variables
            int result = NativeCode.CredUIPromptForWindowsCredentials(ref credUI,
                errorcode,
                ref authPackage,
                IntPtr.Zero,
                0,
                out outCredBuffer,
                out outCredSize,
                ref save,
                flags);

            var usernameBuf = new StringBuilder(100);
            var passwordBuf = new StringBuilder(100);
            var domainBuf = new StringBuilder(100);

            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            if (result == 0)
            {
                if (NativeCode.CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName,
                                                   domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
                {
                    user = usernameBuf.ToString();
                    password = passwordBuf.ToString();
                    domain = domainBuf.ToString();
                    if (String.IsNullOrWhiteSpace(domain))
                    {
                        Debug.WriteLine("Domain null");
                        if (!ParseUserName(usernameBuf.ToString(), usernameBuf.Capacity, domainBuf.Capacity, out user, out domain))
                            user = usernameBuf.ToString();
                        password = passwordBuf.ToString();
                    }
                }

                //mimic SecureZeroMem function to make sure buffer is zeroed out. SecureZeroMem is not an exported function, neither is RtlSecureZeroMemory
                var zeroBytes = new byte[outCredSize];
                Marshal.Copy(zeroBytes, 0, outCredBuffer, (int)outCredSize);

                //clear the memory allocated by CredUIPromptForWindowsCredentials
                NativeCode.CoTaskMemFree(outCredBuffer);
                return true;
            }

            user = null;
            domain = null;
            return false;
        }

        private static bool ParseUserName(string usernameBuf, int maxUserName, int maxDomain, out string user, out string domain)
        {
            StringBuilder userBuilder = new StringBuilder();
            StringBuilder domainBuilder = new StringBuilder();
            user = String.Empty;
            domain = String.Empty;

            var returnCode = NativeCode.CredUIParseUserName(usernameBuf, userBuilder, maxUserName, domainBuilder, maxDomain);
            Debug.WriteLine(returnCode);
            switch (returnCode)
            {
                case NativeCode.CredentialUIReturnCodes.Success: // The username is valid.
                    user = userBuilder.ToString();
                    domain = domainBuilder.ToString();
                    return true;
            }
            return false;
        }

        internal static bool PromptForCredentials(string target, ref bool save, out string user, out string password, out string domain)
        {
            NativeCode.CredentialUIInfo credUI = new NativeCode.CredentialUIInfo();
            credUI.hwndParent = IntPtr.Zero;
            credUI.pszMessageText = " ";
            credUI.pszCaptionText = " ";
            credUI.hbmBanner = IntPtr.Zero;
            credUI.hwndParent = IntPtr.Zero;
            return PromptForCredentials(target, credUI, ref save, out user, out password, out domain);
        }

        internal static bool PromptForCredentials(string target, ref bool save, string Message, string Caption, out string user, out string password, out string domain)
        {
            NativeCode.CredentialUIInfo credUI = new NativeCode.CredentialUIInfo();
            credUI.pszMessageText = Message;
            credUI.pszCaptionText = Caption;
            credUI.hwndParent = IntPtr.Zero;
            credUI.hbmBanner = IntPtr.Zero;
            return PromptForCredentials(target, credUI, ref save, out user, out password, out domain);
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="Target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string Target, ref bool save)
        {
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            if (!PromptForCredentials(Target, ref save, out username, out passwd, out domain))
                return null;
            return new NetworkCredential(username, passwd, domain);
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="Target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <param name="Message">A brief message to display in the dialog box</param>
        /// <param name="Caption">Title for the dialog box</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string Target, ref bool save, string Message, string Caption)
        {
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            if (!PromptForCredentials(Target, ref save, Message, Caption, out username, out passwd, out domain))
                return null;
            return new NetworkCredential(username, passwd, domain);
        }

        /// <summary>
        /// Accepts credentials in a console window
        /// </summary>
        /// <param name="Target">A descriptive text for where teh credentials being asked are used for</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentialsConsole(string target)
        {
            var user = String.Empty;
            var password = String.Empty;
            var domain = String.Empty;

            // Setup the flags and variables
            StringBuilder userPassword = new StringBuilder(), userID = new StringBuilder();
            bool save = true;
            NativeCode.CredentialUIFlags flags = NativeCode.CredentialUIFlags.CompleteUsername | NativeCode.CredentialUIFlags.ExcludeCertificates | NativeCode.CredentialUIFlags.GenericCredentials;

            // Prompt the user
            NativeCode.CredentialUIReturnCodes returnCode = NativeCode.CredUICmdLinePromptForCredentials(target, IntPtr.Zero, 0, userID, 100, userPassword, 100, ref save, flags);

            password = userPassword.ToString();

            StringBuilder userBuilder = new StringBuilder();
            StringBuilder domainBuilder = new StringBuilder();

            returnCode = NativeCode.CredUIParseUserName(userID.ToString(), userBuilder, int.MaxValue, domainBuilder, int.MaxValue);
            switch (returnCode)
            {
                case NativeCode.CredentialUIReturnCodes.Success: // The username is valid.
                    user = userBuilder.ToString();
                    domain = domainBuilder.ToString();
                    break;

                case NativeCode.CredentialUIReturnCodes.InvalidAccountName: // The username is not valid.
                    user = userID.ToString();
                    domain = null;
                    break;

                case NativeCode.CredentialUIReturnCodes.InsufficientBuffer: // One of the buffers is too small.
                    throw new OutOfMemoryException();

                case NativeCode.CredentialUIReturnCodes.InvalidParameter: // ulUserMaxChars or ulDomainMaxChars is zero OR userName, user, or domain is NULL.
                    throw new ArgumentNullException("userName");
            }
            return new NetworkCredential(user, password, domain);
        }

        /// <summary>
        /// Saves the given Network Credential into Windows Credential store
        /// </summary>
        /// <param name="Target">Name of the application/Url where the credential is used for</param>
        /// <param name="credential">Credential to store</param>
        /// <returns>True:Success, False:Failure</returns>
        public static bool SaveCredentials(string Target, NetworkCredential credential)
        {
            // Go ahead with what we have are stuff it into the CredMan structures.
            Credential cred = new Credential(credential);
            cred.TargetName = Target;
            cred.Persist = NativeCode.Persistance.Entrprise;
            NativeCode.NativeCredential ncred = cred.GetNativeCredential();
            // Write the info into the CredMan storage.
            bool written = NativeCode.CredWrite(ref ncred, 0);
            int lastError = Marshal.GetLastWin32Error();
            if (written)
            {
                return true;
            }
            else
            {
                string message = string.Format("CredWrite failed with the error code {0}.", lastError);
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Extract the stored credential from Windows Credential store
        /// </summary>
        /// <param name="Target">Name of the application/Url where the credential is used for</param>
        /// <returns>null if target not found, else stored credentials</returns>
        public static NetworkCredential GetCredentials(string Target,CredentialType type = CredentialType.Generic)
        {
            IntPtr nCredPtr;
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            // Make the API call using the P/Invoke signature

            bool ret = NativeCode.CredRead(Target, (NativeCode.CredentialType) type, 0, out nCredPtr);
            int lastError = Marshal.GetLastWin32Error();
            if (!ret)
                throw new Win32Exception(lastError, "CredDelete throw an error");
            // If the API was successful then...
            if (ret)
            {
                try
                {

                    using (CriticalCredentialHandle critCred = new CriticalCredentialHandle(nCredPtr))
                    {
                        Credential cred = critCred.GetCredential();
                        passwd = cred.CredentialBlob;
                        if (!String.IsNullOrEmpty(cred.UserName))
                        {
                            var user = cred.UserName;
                            StringBuilder userBuilder = new StringBuilder(cred.UserName.Length + 2);
                            StringBuilder domainBuilder = new StringBuilder(cred.UserName.Length + 2);

                            var ret1 = NativeCode.CredUIParseUserName(user, userBuilder, userBuilder.Capacity, domainBuilder, domainBuilder.Capacity);
                            lastError = Marshal.GetLastWin32Error();

                            //assuming invalid account name to be not meeting condition for CredUIParseUserName
                            //"The name must be in UPN or down-level format, or a certificate"
                            if (ret1 == NativeCode.CredentialUIReturnCodes.InvalidAccountName)
                                userBuilder.Append(user);
                            else if ((uint)ret1 > 0)
                                throw new Win32Exception(lastError, "CredUIParseUserName throw an error");

                            username = userBuilder.ToString();
                            domain = domainBuilder.ToString();
                        }
                        return new NetworkCredential(username, passwd, domain);
                    }
                }
                catch(Exception e)
                {
                    return null;
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
            var ret = NativeCode.CredDelete(Target, NativeCode.CredentialType.Generic, 0);
            int lastError = Marshal.GetLastWin32Error();
            if (!ret)
                throw new Win32Exception(lastError, "CredDelete throw an error");
            return ret;
        }

        /// <summary>
        /// Generates a string that can be used for "Auth" headers in web requests, "username:password" encoded in Base64
        /// </summary>
        /// <param name="cred"></param>
        /// <returns></returns>
        public static string GetBasicAuthString(this NetworkCredential cred)
        {
            byte[] credentialBuffer = new UTF8Encoding().GetBytes(cred.UserName + ":" + cred.Password);
            return Convert.ToBase64String(credentialBuffer);
        }
    }
}