using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace shakeyourbunny.CredentialManager;

//ref: https://docs.microsoft.com/en-us/archive/blogs/peerchan/application-password-security

/// <summary>
/// Provides static methods for interacting with the Windows Credential Store.
/// Supports saving, retrieving, enumerating, and removing credentials,
/// as well as prompting users for credentials via Windows UI or console.
/// </summary>
public static class CredentialManager
{
    private static bool PromptForCredentials(NativeCode.CredentialUIInfo credUI, ref bool save, ref string user, out string password, out string domain)
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

        user = null!;
        domain = null!;
        return false;
    }

    private static void GetCredentialsFromOutputBuffer(ref string user, ref string password, ref string domain, IntPtr outCredBuffer, uint outCredSize)
    {
        int maxUserName = NativeCode.CREDUI_MAX_USERNAME_LENGTH;
        int maxDomain = NativeCode.CREDUI_MAX_USERNAME_LENGTH;
        int maxPassword = NativeCode.CREDUI_MAX_PASSWORD_LENGTH;
        var usernameBuf = new StringBuilder(maxUserName);
        var passwordBuf = new StringBuilder(maxPassword);
        var domainBuf = new StringBuilder(maxDomain);

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

        // Zero sensitive buffers using SecureZeroMemory (cannot be optimized away by JIT)
        NativeCode.SecureZeroMemory(outCredBuffer, new UIntPtr(outCredSize));

        // Zero StringBuilder contents to prevent password lingering in managed memory
        for (int i = 0; i < passwordBuf.Length; i++)
            passwordBuf[i] = '\0';

        // Clear the memory allocated by CredUIPromptForWindowsCredentials
        NativeCode.CoTaskMemFree(outCredBuffer);
    }

    private static void GetInputBuffer(string user, out IntPtr inCredBuffer, out int inCredSize)
    {
        if (!string.IsNullOrEmpty(user))
        {
            var usernameBuf = new StringBuilder(user);
            var passwordBuf = new StringBuilder();

            // Query required buffer size first (pass IntPtr.Zero, size 0)
            inCredSize = 0;
            NativeCode.CredPackAuthenticationBuffer(0x00, usernameBuf, passwordBuf, IntPtr.Zero, ref inCredSize);

            if (inCredSize > 0)
            {
                inCredBuffer = Marshal.AllocCoTaskMem(inCredSize);
                if (NativeCode.CredPackAuthenticationBuffer(0x00, usernameBuf, passwordBuf, inCredBuffer, ref inCredSize))
                    return;

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
            case NativeCode.CredentialUIReturnCodes.Success:
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
        return PromptForCredentials(credUI, ref save, ref user, out password, out domain);
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
        return PromptForCredentials(credUI, ref save, ref user, out password, out domain);
    }

    /// <summary>
    /// Opens OS Version specific Window prompting for credentials.
    /// </summary>
    /// <param name="target">A descriptive text for where the credentials being asked are used for.</param>
    /// <param name="save">Whether or not to offer the checkbox to save the credentials.</param>
    /// <param name="parentWindowHandle">Handle to the parent window.</param>
    /// <returns>NetworkCredential object containing the user name, password, and domain; or null if cancelled.</returns>
    public static NetworkCredential? PromptForCredentials(string target, ref bool save, IntPtr parentWindowHandle = default)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        string username = "", password, domain;
        return PromptForCredentials(target, ref save, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
    }

    /// <summary>
    /// Opens OS Version specific Window prompting for credentials.
    /// </summary>
    /// <param name="target">A descriptive text for where the credentials being asked are used for.</param>
    /// <param name="save">Whether or not to offer the checkbox to save the credentials.</param>
    /// <param name="message">A brief message to display in the dialog box.</param>
    /// <param name="caption">Title for the dialog box.</param>
    /// <param name="parentWindowHandle">Handle to the parent window.</param>
    /// <returns>NetworkCredential object containing the user name, password, and domain; or null if cancelled.</returns>
    public static NetworkCredential? PromptForCredentials(string target, ref bool save, string message, string caption, IntPtr parentWindowHandle = default)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (caption == null) throw new ArgumentNullException(nameof(caption));

        string username = "", password, domain;
        return PromptForCredentials(target, ref save, message, caption, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
    }

    /// <summary>
    /// Opens OS Version specific Window prompting for credentials.
    /// </summary>
    /// <param name="target">A descriptive text for where the credentials being asked are used for.</param>
    /// <param name="save">Whether or not to offer the checkbox to save the credentials.</param>
    /// <param name="message">A brief message to display in the dialog box.</param>
    /// <param name="caption">Title for the dialog box.</param>
    /// <param name="defaultUserName">Default value for username.</param>
    /// <param name="parentWindowHandle">Handle to the parent window.</param>
    /// <returns>NetworkCredential object containing the user name, password, and domain; or null if cancelled.</returns>
    public static NetworkCredential? PromptForCredentials(string target, ref bool save, string message, string caption, string defaultUserName, IntPtr parentWindowHandle = default)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (caption == null) throw new ArgumentNullException(nameof(caption));

        string username = defaultUserName, password, domain;
        return PromptForCredentials(target, ref save, message, caption, ref username, out password, out domain, parentWindowHandle) ? new NetworkCredential(username, password, domain) : null;
    }

    /// <summary>
    /// Accepts credentials in a console window.
    /// </summary>
    /// <param name="target">A descriptive text for where the credentials being asked are used for.</param>
    /// <returns>NetworkCredential object containing the user name, password, and domain; or null if the user cancelled.</returns>
    public static NetworkCredential? PromptForCredentialsConsole(string target)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        var user = String.Empty;
        var domain = string.Empty;

        // Setup the flags and variables
        var userPassword = new StringBuilder(NativeCode.CREDUI_MAX_PASSWORD_LENGTH);
        var userID = new StringBuilder(NativeCode.CREDUI_MAX_USERNAME_LENGTH);
        bool save = true;
        NativeCode.CredentialUIFlags flags = NativeCode.CredentialUIFlags.CompleteUsername | NativeCode.CredentialUIFlags.ExcludeCertificates | NativeCode.CredentialUIFlags.GenericCredentials;

        // Prompt the user
        NativeCode.CredentialUIReturnCodes promptResult = NativeCode.CredUICmdLinePromptForCredentials(
            target, IntPtr.Zero, 0,
            userID, NativeCode.CREDUI_MAX_USERNAME_LENGTH,
            userPassword, NativeCode.CREDUI_MAX_PASSWORD_LENGTH,
            ref save, flags);

        if (promptResult == NativeCode.CredentialUIReturnCodes.Cancelled)
        {
            // Zero the password buffer even on cancel
            for (int i = 0; i < userPassword.Length; i++)
                userPassword[i] = '\0';
            return null;
        }

        if (promptResult != NativeCode.CredentialUIReturnCodes.Success)
        {
            for (int i = 0; i < userPassword.Length; i++)
                userPassword[i] = '\0';
            throw new CredentialAPIException($"Console credential prompt failed", "CredUICmdLinePromptForCredentials", (int)promptResult);
        }

        string password = userPassword.ToString();

        // Zero the password StringBuilder after extracting the string
        for (int i = 0; i < userPassword.Length; i++)
            userPassword[i] = '\0';

        var userBuilder = new StringBuilder(NativeCode.CREDUI_MAX_USERNAME_LENGTH);
        var domainBuilder = new StringBuilder(NativeCode.CREDUI_MAX_USERNAME_LENGTH);

        var parseResult = NativeCode.CredUIParseUserName(userID.ToString(), userBuilder, NativeCode.CREDUI_MAX_USERNAME_LENGTH, domainBuilder, NativeCode.CREDUI_MAX_USERNAME_LENGTH);
        switch (parseResult)
        {
            case NativeCode.CredentialUIReturnCodes.Success:
                user = userBuilder.ToString();
                domain = domainBuilder.ToString();
                break;

            case NativeCode.CredentialUIReturnCodes.InvalidAccountName:
                user = userID.ToString();
                domain = null!;
                break;

            case NativeCode.CredentialUIReturnCodes.InsufficientBuffer:
                throw new CredentialAPIException("Buffer too small for parsed user name", "CredUIParseUserName", (int)parseResult);

            case NativeCode.CredentialUIReturnCodes.InvalidParameter:
                throw new CredentialAPIException("Invalid parameter for user name parsing", "CredUIParseUserName", (int)parseResult);
        }
        return new NetworkCredential(user, password, domain);
    }

    /// <summary>
    /// Saves the given Network Credential into Windows Credential store.
    /// </summary>
    /// <param name="target">Name of the application/URL where the credential is used for.</param>
    /// <param name="credential">Credential to store.</param>
    /// <param name="type">Credential type.</param>
    /// <param name="AllowNullPassword">If true, allows saving credentials with an empty password.</param>
    /// <param name="persistence">How the credential should be persisted. Defaults to LocalMachine (local only, no domain roaming).</param>
    /// <returns>The saved ICredential on success, or null on failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target or credential is null.</exception>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails.</exception>
    public static ICredential? SaveCredentials(string target, NetworkCredential credential, CredentialType type = CredentialType.Generic, bool AllowNullPassword = false, Persistence persistence = Persistence.LocalMachine)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (credential == null) throw new ArgumentNullException(nameof(credential));

        var cred = new Credential(credential)
        {
            TargetName = target,
            Persistence = persistence,
            Type = type
        };
        if (cred.SaveCredential(AllowNullPassword))
        {
            return cred;
        }
        return null;
    }

    /// <summary>
    /// Extract the stored credential from Windows Credential store.
    /// </summary>
    /// <param name="target">Name of the application/URL where the credential is used for.</param>
    /// <param name="type">Credential type.</param>
    /// <returns>The credentials if found, null if target not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target is null.</exception>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails (other than not found).</exception>
    public static NetworkCredential? GetCredentials(string target, CredentialType type = CredentialType.Generic)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        return (GetICredential(target, type) as Credential)?.ToNetworkCredential();
    }

    /// <summary>
    /// Enumerate the specified stored credentials in the Windows Credential store.
    /// </summary>
    /// <param name="target">Name of the application or URL for which the credential is used. Pass null to enumerate all credentials.</param>
    /// <returns>A list of credentials if found, null if none match.</returns>
    public static List<NetworkCredential>? EnumerateCredentials(string? target = null)
    {
        return EnumerateICredentials(target)?.Select(c => c.ToNetworkCredential())?.ToList();
    }

    /// <summary>
    /// Enumerate the specified stored credentials in the Windows Credential store.
    /// </summary>
    /// <param name="target">Name of the application or URL for which the credential is used. Pass null to enumerate all credentials.</param>
    /// <returns>A list of ICredential objects if found, null if none match.</returns>
    public static List<ICredential>? EnumerateICredentials(string? target = null)
    {
        var success = NativeCode.CredEnumerate(target, 0, out uint count, out IntPtr pCredentials);

        if (!success)
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError == (int)NativeCode.CredentialUIReturnCodes.NotFound)
            {
                return null;
            }

            throw new CredentialAPIException(SR.UnableToEnumerateCredentials, "CredEnumerate", lastError);
        }

        Credential[] credentials;

        try
        {
            using var criticalSection = new CriticalCredentialHandle(pCredentials, count);
            credentials = criticalSection.EnumerateCredentials(count);
        }
        catch (CredentialAPIException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new CredentialAPIException($"Failed to enumerate credentials: {ex.Message}", "CredEnumerate", 0);
        }

        return credentials.Select(c => c as ICredential).ToList();
    }

    /// <summary>
    /// Remove stored credentials from windows credential store.
    /// </summary>
    /// <param name="target">Name of the application/URL where the credential is used for.</param>
    /// <param name="type">Credential type.</param>
    /// <returns>True on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target is null.</exception>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails.</exception>
    public static bool RemoveCredentials(string target, CredentialType type = CredentialType.Generic)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        var cred = new Credential(
            target,
            type
        );
        return cred.RemoveCredential();
    }

    /// <summary>
    /// Generates a string that can be used for "Auth" headers in web requests, "username:password" encoded in Base64.
    /// </summary>
    /// <param name="cred">The credential to encode.</param>
    /// <returns>Base64-encoded "username:password" string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when cred is null.</exception>
    public static string GetBasicAuthString(this NetworkCredential cred)
    {
        if (cred == null) throw new ArgumentNullException(nameof(cred));

        byte[] credentialBuffer = new UTF8Encoding().GetBytes(cred.UserName + ":" + cred.Password);
        try
        {
            return Convert.ToBase64String(credentialBuffer);
        }
        finally
        {
            Array.Clear(credentialBuffer, 0, credentialBuffer.Length);
        }
    }

    /// <summary>
    /// Extract the stored credential from Windows Credential store as an ICredential.
    /// </summary>
    /// <param name="target">Name of the application/URL where the credential is used for.</param>
    /// <param name="type">Credential type.</param>
    /// <returns>The ICredential if found, null if target not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when target is null.</exception>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails (other than not found).</exception>
    public static ICredential? GetICredential(string target, CredentialType type = CredentialType.Generic)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        IntPtr nCredPtr;

        // Make the API call using the P/Invoke signature
        bool isSuccess = NativeCode.CredRead(target, (UInt32)type, 0, out nCredPtr);
        if (!isSuccess)
        {
            var lastError = Marshal.GetLastWin32Error();
            if (lastError == (int)NativeCode.CredentialUIReturnCodes.NotFound)
                return null;
            throw new CredentialAPIException(SR.UnableToReadCredential, "CredRead", lastError);
        }

        try
        {
            using var critCred = new CriticalCredentialHandle(nCredPtr);
            Credential cred = critCred.GetCredential();
            return cred;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read credential: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }
}
