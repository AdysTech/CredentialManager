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

            var outCredBuffer = new IntPtr();
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

        internal static bool ParseUserName(string usernameBuf, int maxUserName, int maxDomain, out string user, out string domain)
        {
            var userBuilder = new StringBuilder(maxUserName);
            var domainBuilder = new StringBuilder(maxDomain);
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
            var credUI = new NativeCode.CredentialUIInfo
            {
                hwndParent = IntPtr.Zero, pszMessageText = " ", pszCaptionText = " ", hbmBanner = IntPtr.Zero
            };
            return PromptForCredentials(target, credUI, ref save, out user, out password, out domain);
        }

        internal static bool PromptForCredentials(string target, ref bool save, string message, string caption, out string user, out string password, out string domain)
        {
            var credUI = new NativeCode.CredentialUIInfo
            {
                pszMessageText = message,
                pszCaptionText = caption,
                hwndParent = IntPtr.Zero,
                hbmBanner = IntPtr.Zero
            };
            return PromptForCredentials(target, credUI, ref save, out user, out password, out domain);
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string target, ref bool save)
        {
            string username, passwd, domain;
            return PromptForCredentials(target, ref save, out username, out passwd, out domain) ? new NetworkCredential(username, passwd, domain) : null;
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <param name="message">A brief message to display in the dialog box</param>
        /// <param name="caption">Title for the dialog box</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string target, ref bool save, string message, string caption)
        {
            string username, passwd, domain;
            return PromptForCredentials(target, ref save, message, caption, out username, out passwd, out domain) ? new NetworkCredential(username, passwd, domain) : null;
        }

        /// <summary>
        /// Accepts credentials in a console window
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
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
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <param name="credential">Credential to store</param>
        /// <returns>True:Success, throw if failed</returns>
        public static bool SaveCredentials(string target, NetworkCredential credential)
        {
            // Go ahead with what we have are stuff it into the CredMan structures.
            var cred = new Credential(credential)
            {
                TargetName = target, Persist = NativeCode.Persistance.Entrprise
            };
            NativeCode.NativeCredential ncred = cred.GetNativeCredential();

            // Write the info into the CredMan storage.
            if (NativeCode.CredWrite(ref ncred, 0))
            {
                return true;
            }

            int lastError = Marshal.GetLastWin32Error();
            string message = String.Format("'CredWrite' call throw an error (Error code: {0})", lastError);
            throw new Win32Exception(lastError, message);
        }

        /// <summary>
        /// Extract the stored credential from Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <param name="type">Credential type</param>
        /// <returns>return the credentials if success, null if target not found, throw if failed to read stored credentials</returns>
        public static NetworkCredential GetCredentials(string target, CredentialType type = CredentialType.Generic)
        {
            IntPtr nCredPtr;
            var username = String.Empty;
            var passwd = String.Empty;
            var domain = String.Empty;

            // Make the API call using the P/Invoke signature
            bool isSuccess = NativeCode.CredRead(target, (NativeCode.CredentialType) type, 0, out nCredPtr);
            if (!isSuccess)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == (int) NativeCode.CredentialUIReturnCodes.NotFound)
                    return null;
                throw new Win32Exception(lastError,
                    String.Format("'CredRead' call throw an error (Error code: {0})", lastError));
            }

            try
            {
                using (var critCred = new CriticalCredentialHandle(nCredPtr))
                {
                    Credential cred = critCred.GetCredential();
                    passwd = cred.CredentialBlob;
                    if (!String.IsNullOrEmpty(cred.UserName))
                    {
                        var user = cred.UserName;
                        StringBuilder userBuilder = new StringBuilder(cred.UserName.Length + 2);
                        StringBuilder domainBuilder = new StringBuilder(cred.UserName.Length + 2);

                        var returnCode = NativeCode.CredUIParseUserName(user, userBuilder, userBuilder.Capacity, domainBuilder, domainBuilder.Capacity);
                        var lastError = Marshal.GetLastWin32Error();

                        //assuming invalid account name to be not meeting condition for CredUIParseUserName
                        //"The name must be in UPN or down-level format, or a certificate"
                        if (returnCode == NativeCode.CredentialUIReturnCodes.InvalidAccountName)
                            userBuilder.Append(user);
                        else if (returnCode != 0)
                            throw new Win32Exception(lastError, String.Format("CredUIParseUserName throw an error (Error code: {0})", lastError));

                        username = userBuilder.ToString();
                        domain = domainBuilder.ToString();
                    }
                    return new NetworkCredential(username, passwd, domain);
                }
            }
            catch(Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Remove stored credentials from windows credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <returns>True: Success, throw if failed</returns>
        public static bool RemoveCredentials(string target)
        {
            // Make the API call using the P/Invoke signature
            var isSuccess = NativeCode.CredDelete(target, NativeCode.CredentialType.Generic, 0);

            if (isSuccess)
                return true;

            int lastError = Marshal.GetLastWin32Error();
            throw new Win32Exception(lastError, String.Format("'CredDelete' call throw an error (Error code: {0})", lastError));
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