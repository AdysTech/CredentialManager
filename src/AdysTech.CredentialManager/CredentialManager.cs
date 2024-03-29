using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace AdysTech.CredentialManager
{
    //ref: https://docs.microsoft.com/en-us/archive/blogs/peerchan/application-password-security

    public static class CredentialManager
    {
        private static bool PromptForCredentials(string target, NativeCode.CredentialUIInfo credUI, ref bool save, ref string user, out string password, out string domain)
        {
            password = string.Empty;
            domain = string.Empty;

            // Setup the flags and variables
            credUI.cbSize = Marshal.SizeOf(credUI);
            int errorcode = 0;
            uint authPackage = 0;

            IntPtr outCredBuffer;
            var flags = NativeCode.PromptForWindowsCredentialsFlags.GenericCredentials |
                    NativeCode.PromptForWindowsCredentialsFlags.EnumerateCurrentUser;
            flags = save ? flags | NativeCode.PromptForWindowsCredentialsFlags.ShowCheckbox : flags;

            // Prefill username
            IntPtr inCredBuffer;
            int inCredSize;
            GetInputBuffer(user, out inCredBuffer, out inCredSize);

            // Setup the flags and variables
            int result = NativeCode.CredUIPromptForWindowsCredentials(ref credUI,
                errorcode,
                ref authPackage,
                inCredBuffer,
                inCredSize,
                out outCredBuffer,
                out uint outCredSize,
                ref save,
                flags);

            if (inCredBuffer != IntPtr.Zero)
            {
                NativeCode.CoTaskMemFree(inCredBuffer);
            }

            if (result == 0)
            {
                GetCredentialsFromOutputBuffer(ref user, ref password, ref domain, outCredBuffer, outCredSize);
                return true;
            }

            user = null;
            domain = null;
            return false;
        }

        private static void GetCredentialsFromOutputBuffer(ref string user, ref string password, ref string domain, IntPtr outCredBuffer, uint outCredSize)
        {
            int maxUserName = 100;
            int maxDomain = 100;
            int maxPassword = 100;
            var usernameBuf = new StringBuilder(maxUserName);
            var passwordBuf = new StringBuilder(maxDomain);
            var domainBuf = new StringBuilder(maxPassword);

            if (NativeCode.CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, usernameBuf, ref maxUserName,
                                               domainBuf, ref maxDomain, passwordBuf, ref maxPassword))
            {
                user = usernameBuf.ToString();
                password = passwordBuf.ToString();
                domain = domainBuf.ToString();
                if (string.IsNullOrWhiteSpace(domain))
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
        }

        private static void GetInputBuffer(string user, out IntPtr inCredBuffer, out int inCredSize)
        {
            if (!string.IsNullOrEmpty(user))
            {
                var usernameBuf = new StringBuilder(user);
                var passwordBuf = new StringBuilder();

                inCredSize = 1024;
                inCredBuffer = Marshal.AllocCoTaskMem(inCredSize);
                if (NativeCode.CredPackAuthenticationBuffer(0x00, usernameBuf, passwordBuf, inCredBuffer, ref inCredSize))
                    return;

                if (inCredBuffer != IntPtr.Zero)
                {
                    NativeCode.CoTaskMemFree(inCredBuffer);
                }
            }

            inCredBuffer = IntPtr.Zero;
            inCredSize = 0;
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

        internal static bool PromptForCredentials(string target, ref bool save, ref string user, out string password, out string domain, IntPtr parentWindowHandle = default)
        {
            var credUI = new NativeCode.CredentialUIInfo
            {
                hwndParent = parentWindowHandle,
                pszMessageText = " ",
                pszCaptionText = " ",
                hbmBanner = IntPtr.Zero
            };
            return PromptForCredentials(target, credUI, ref save, ref user, out password, out domain);
        }

        internal static bool PromptForCredentials(string target, ref bool save, string message, string caption, ref string user, out string password, out string domain, IntPtr parentWindowHandle = default)
        {
            var credUI = new NativeCode.CredentialUIInfo
            {
                hwndParent = parentWindowHandle,
                pszMessageText = message,
                pszCaptionText = caption,
                hbmBanner = IntPtr.Zero
            };
            return PromptForCredentials(target, credUI, ref save, ref user, out password, out domain);
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string target, ref bool save, IntPtr parentWindowHandle = default)
        {
            string username = "", password, domain;
            return PromptForCredentials(target, ref save, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <param name="message">A brief message to display in the dialog box</param>
        /// <param name="caption">Title for the dialog box</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string target, ref bool save, string message, string caption, IntPtr parentWindowHandle = default)
        {
            string username = "", password, domain;
            return PromptForCredentials(target, ref save, message, caption, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
        }

        /// <summary>
        /// Opens OS Version specific Window prompting for credentials
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <param name="save">Whether or not to offer the checkbox to save the credentials</param>
        /// <param name="message">A brief message to display in the dialog box</param>
        /// <param name="caption">Title for the dialog box</param>
        /// <param name="defaultUserName">Default value for username</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentials(string target, ref bool save, string message, string caption, string defaultUserName, IntPtr parentWindowHandle = default)
        {
            string username = defaultUserName, password, domain;
            return PromptForCredentials(target, ref save, message, caption, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
        }

        /// <summary>
        /// Accepts credentials in a console window
        /// </summary>
        /// <param name="target">A descriptive text for where teh credentials being asked are used for</param>
        /// <returns>NetworkCredential object containing the user name, </returns>
        public static NetworkCredential PromptForCredentialsConsole(string target)
        {
            var user = String.Empty;
            var domain = string.Empty;

            // Setup the flags and variables
            StringBuilder userPassword = new StringBuilder(), userID = new StringBuilder();
            bool save = true;
            NativeCode.CredentialUIFlags flags = NativeCode.CredentialUIFlags.CompleteUsername | NativeCode.CredentialUIFlags.ExcludeCertificates | NativeCode.CredentialUIFlags.GenericCredentials;

            // Prompt the user
            NativeCode.CredentialUIReturnCodes returnCode = NativeCode.CredUICmdLinePromptForCredentials(target, IntPtr.Zero, 0, userID, 100, userPassword, 100, ref save, flags);

            string password = userPassword.ToString();

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
                    throw new ArgumentException("userName");
            }
            return new NetworkCredential(user, password, domain);
        }

        /// <summary>
        /// Saves the given Network Credential into Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <param name="credential">Credential to store</param>
        /// <param name="type">Credential type</param>
        /// <returns>True:Success, throw if failed</returns>
        public static ICredential SaveCredentials(string target, NetworkCredential credential, CredentialType type = CredentialType.Generic, bool AllowNullPassword= false)
        {
            // Go ahead with what we have are stuff it into the CredMan structures.
            var cred = new Credential(credential)
            {
                TargetName = target,
                Persistance = Persistance.Enterprise,
                Type = type
            };
            if( cred.SaveCredential(AllowNullPassword))
            {
                return cred;
            }
            return null;
        }

        /// <summary>
        /// Extract the stored credential from Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <param name="type">Credential type</param>
        /// <returns>return the credentials if success, null if target not found, throw if failed to read stored credentials</returns>
        public static NetworkCredential GetCredentials(string target, CredentialType type = CredentialType.Generic)
        {
            return (GetICredential(target, type) as Credential)?.ToNetworkCredential();
        }

        /// <summary>
        /// Enumerate the specified stored credentials in the Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application or URL for which the credential is used</param>
        /// <returns>Return a <see cref="List{NetworkCredential}"/> if success, null if target not found, throw if failed to read stored credentials</returns>
        public static List<NetworkCredential> EnumerateCredentials(string target = null)
        {
            return EnumerateICredentials(target)?.Select(c => c.ToNetworkCredential())?.ToList();
        }

        /// <summary>
        /// Enumerate the specified stored credentials in the Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application or URL for which the credential is used</param>
        /// <returns>Return a <see cref="List{ICredential}"/> if success, null if target not found, throw if failed to read stored credentials</returns>
        public static List<ICredential> EnumerateICredentials(string target = null)
        {
            IntPtr pCredentials = IntPtr.Zero;
            uint count = 0;

            var success = NativeCode.CredEnumerate(target, 0, out count, out pCredentials);

            if (!success)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == (int)NativeCode.CredentialUIReturnCodes.NotFound)
                {
                    return null;
                }

                throw new CredentialAPIException($"Unable to Enumerate Credential store", "CredEnumerate", lastError);
            }

            List<NetworkCredential> networkCredentials = new List<NetworkCredential>();
            Credential[] credentials;

            try
            {
                using var criticalSection = new CriticalCredentialHandle(pCredentials);
                credentials = criticalSection.EnumerateCredentials(count);
            }
            catch (Exception)
            {
                return null;
            }

            return credentials.Select(c => c as ICredential).ToList();
        }

        /// <summary>
        /// Remove stored credentials from windows credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <returns>True: Success, throw if failed</returns>
        public static bool RemoveCredentials(string target, CredentialType type = CredentialType.Generic)
        {
            var cred = new Credential(
                target,
                type
            );
            return cred.RemoveCredential();
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

        /// <summary>
        /// Extract the stored credential from Windows Credential store
        /// </summary>
        /// <param name="target">Name of the application/Url where the credential is used for</param>
        /// <param name="type">Credential type</param>
        /// <returns>return the ICredential if success, null if target not found, throw if failed to read stored credentials</returns>
        public static ICredential GetICredential(string target, CredentialType type = CredentialType.Generic)
        {
            IntPtr nCredPtr;

            // Make the API call using the P/Invoke signature
            bool isSuccess = NativeCode.CredRead(target, (UInt32)type, 0, out nCredPtr);
            if (!isSuccess)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == (int)NativeCode.CredentialUIReturnCodes.NotFound)
                    return null;
                throw new CredentialAPIException($"Unable to Read Credential", "CredRead", lastError);
            }

            try
            {
                using var critCred = new CriticalCredentialHandle(nCredPtr);
                Credential cred = critCred.GetCredential();
                return cred;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}