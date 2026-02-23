using System;
using System.Runtime.InteropServices;
using System.Text;

namespace shakeyourbunny.CredentialManager;

internal static class NativeCode
{
    /// <summary>
    /// Maximum username length for credential UI prompts (CREDUI_MAX_USERNAME_LENGTH).
    /// </summary>
    internal const int CREDUI_MAX_USERNAME_LENGTH = 513;

    /// <summary>
    /// Maximum password length for credential UI prompts (CREDUI_MAX_PASSWORD_LENGTH).
    /// </summary>
    internal const int CREDUI_MAX_PASSWORD_LENGTH = 256;

    [Flags]
    internal enum CredentialUIFlags
    {
        IncorrectPassword = 0x1,
        DoNotPersist = 0x2,
        RequestAdministrator = 0x4,
        ExcludeCertificates = 0x8,
        RequireCertificate = 0x10,
        ShowSaveCheckBox = 0x40,
        AlwaysShowUi = 0x80,
        RequireSmartcard = 0x100,
        PasswordOnlyOk = 0x200,
        ValidateUsername = 0x400,
        CompleteUsername = 0x800,
        Persist = 0x1000,
        ServerCredential = 0x4000,
        ExpectConfirmation = 0x20000,
        GenericCredentials = 0x40000,
        UsernameTargetCredentials = 0x80000,
        KeepUsername = 0x100000
    }

    internal enum CredentialUIReturnCodes : uint
    {
        Success = 0,
        Cancelled = 1223,
        NoSuchLogonSession = 1312,
        NotFound = 1168,
        InvalidAccountName = 1315,
        InsufficientBuffer = 122,
        InvalidParameter = 87,
        InvalidFlags = 1004
    }

    /// <summary>
    /// Contains information about the appearance and behavior of a credential dialog box.
    /// Corresponds to the native CREDUI_INFO structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct CredentialUIInfo
    {
        /// <summary>Size of this structure, in bytes.</summary>
        public int cbSize;
        /// <summary>Handle to the parent window of the dialog box.</summary>
        public IntPtr hwndParent;
        /// <summary>Message to display in the dialog box.</summary>
        public string pszMessageText;
        /// <summary>Title for the dialog box.</summary>
        public string pszCaptionText;
        /// <summary>Handle to a bitmap to display in the dialog box.</summary>
        public IntPtr hbmBanner;
    }

    /// <summary>
    /// Represents a credential stored in the Windows Credential Store.
    /// Corresponds to the native CREDENTIALW structure.
    /// </summary>
    /// <remarks>
    /// See: https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NativeCredential
    {
        /// <summary>Bit member that identifies characteristics of the credential.</summary>
        public UInt32 Flags;
        /// <summary>Type of credential (Generic, Windows, Certificate).</summary>
        public UInt32 Type;
        /// <summary>Name of the credential target.</summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string TargetName;
        /// <summary>Comment associated with the credential (max 256 bytes).</summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? Comment;
        /// <summary>Time the credential was last written, as a FILETIME.</summary>
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        /// <summary>Size, in bytes, of the CredentialBlob member.</summary>
        public UInt32 CredentialBlobSize;
        /// <summary>Pointer to the credential data (e.g. password).</summary>
        public IntPtr CredentialBlob;
        /// <summary>Persistence type (Session, LocalMachine, Enterprise).</summary>
        public UInt32 Persist;
        /// <summary>Number of credential attributes.</summary>
        public UInt32 AttributeCount;
        /// <summary>Pointer to an array of CREDENTIAL_ATTRIBUTE structures.</summary>
        public IntPtr Attributes;
        /// <summary>Alias for the target name (reserved, typically null).</summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? TargetAlias;
        /// <summary>User name associated with the credential.</summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? UserName;
    }

    /// <summary>
    /// Represents a key-value attribute attached to a credential.
    /// Corresponds to the native CREDENTIAL_ATTRIBUTE structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct NativeCredentialAttribute
    {
        /// <summary>Name of the attribute (max 256 characters).</summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Keyword;
        /// <summary>Reserved flags (must be zero).</summary>
        public UInt32 Flags;
        /// <summary>Size, in bytes, of the Value member.</summary>
        public UInt32 ValueSize;
        /// <summary>Pointer to the attribute value data.</summary>
        public IntPtr Value;
    }

    [Flags]
    internal enum PromptForWindowsCredentialsFlags : uint
    {
        GenericCredentials = 0x1,
        ShowCheckbox = 0x2,
        AuthpackageOnly = 0x10,
        InCredOnly = 0x20,
        EnumerateAdmins = 0x100,
        EnumerateCurrentUser = 0x200,
        SecurePrompt = 0x1000,
        Pack32Wow = 0x10000000
    }

    // --- credui.dll imports ---

    [DllImport("credui.dll", EntryPoint = "CredUIParseUserNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern CredentialUIReturnCodes CredUIParseUserName(
            string userName,
            StringBuilder user,
            int userMaxChars,
            StringBuilder domain,
            int domainMaxChars);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredPackAuthenticationBuffer(
        Int32 dwFlags,
        StringBuilder pszUserName,
        StringBuilder pszPassword,
        IntPtr pPackedCredentials,
        ref Int32 pcbPackedCredentials
    );

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CredUnPackAuthenticationBuffer(int dwFlags,
        IntPtr pAuthBuffer,
        uint cbAuthBuffer,
        StringBuilder pszUserName,
        ref int pcchMaxUserName,
        StringBuilder pszDomainName,
        ref int pcchMaxDomainame,
        StringBuilder pszPassword,
        ref int pcchMaxPassword);

    [DllImport("credui.dll", EntryPoint = "CredUIPromptForWindowsCredentialsW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int CredUIPromptForWindowsCredentials(ref CredentialUIInfo creditUR,
        int authError,
        ref uint authPackage,
        IntPtr inAuthBuffer,
        int inAuthBufferSize,
        out IntPtr refOutAuthBuffer,
        out uint refOutAuthBufferSize,
        ref bool fSave,
        PromptForWindowsCredentialsFlags flags);

    [DllImport("credui.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern CredentialUIReturnCodes CredUICmdLinePromptForCredentials(
        string targetName,
        IntPtr reserved1,
        int iError,
        StringBuilder userName,
        int maxUserName,
        StringBuilder password,
        int maxPassword,
        [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
        CredentialUIFlags flags);

    // --- Advapi32.dll imports ---

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CredDelete([MarshalAs(UnmanagedType.LPWStr)] string target, uint type, int reservedFlag);

    [DllImport("Advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CredEnumerate([MarshalAs(UnmanagedType.LPWStr)] string? target, UInt32 flags, out UInt32 count, out IntPtr credentialsPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CredRead([MarshalAs(UnmanagedType.LPWStr)] string target, uint type, int reservedFlag, out IntPtr CredentialPtr);

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern bool CredWrite([In] ref NativeCredential userCredential, [In] UInt32 flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
    internal static extern bool CredFree([In] IntPtr cred);

    // --- ole32.dll imports ---

    [DllImport("ole32.dll", EntryPoint = "CoTaskMemFree", SetLastError = true)]
    internal static extern void CoTaskMemFree(IntPtr buffer);

    // --- kernel32.dll imports ---

    /// <summary>
    /// Fills a block of memory with zeros in a way that cannot be optimized away by the JIT.
    /// Uses RtlZeroMemory via P/Invoke — the external call boundary prevents dead store elimination.
    /// </summary>
    [DllImport("kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
    internal static extern void SecureZeroMemory(IntPtr dest, UIntPtr size);
}
