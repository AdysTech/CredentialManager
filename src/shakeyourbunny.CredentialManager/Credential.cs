using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;


namespace shakeyourbunny.CredentialManager;

internal class Credential : ICredential
{
    /// <summary>
    /// Maximum size in bytes of a credential that can be stored. While the API
    /// documentation lists 512 as the max size, the current Windows SDK sets
    /// it to 5*512 via CRED_MAX_CREDENTIAL_BLOB_SIZE in wincred.h. This has
    /// been verified to work on Windows Server 2016 and later.
    /// <para>
    /// API Doc: https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentiala
    /// </para>
    /// </summary>
    /// <remarks>
    /// This only controls the guard in the library. The actual underlying OS
    /// controls the actual limit. Operating Systems older than Windows Server
    /// 2016 may only support 512 bytes.
    /// <para>
    /// Tokens often are 1040 bytes or more.
    /// </para>
    /// </remarks>
    internal const int MaxCredentialBlobSize = 2560;

    /// <summary>
    /// JSON serialization options used for credential attribute serialization.
    /// Includes fields to support structs with public fields (common pattern for simple attribute types).
    /// </summary>
    internal static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        IncludeFields = true,
        WriteIndented = false
    };

    public CredentialType Type { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime LastWritten { get; set; }
    /// <summary>
    /// The credential secret (password or token) as a managed string.
    /// Note: Managed strings are immutable and cannot be reliably zeroed from memory.
    /// The GC may retain or copy the value. For maximum security, keep the lifetime
    /// of Credential objects short and avoid caching CredentialBlob values.
    /// </summary>
    public string? CredentialBlob { get; set; }
    public Persistence Persistence { get; set; }
    public IDictionary<string, Object>? Attributes { get; set; }
    public string? UserName { get; set; }

    public UInt32 Flags { get; set; }
    public string? TargetAlias { get; set; }

    internal Credential(NativeCode.NativeCredential ncred)
    {
        Flags = ncred.Flags;
        TargetName = ncred.TargetName;
        Comment = ncred.Comment;
        try
        {
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
            LastWritten = DateTime.FromFileTime((long)((ulong)ncred.LastWritten.dwHighDateTime << 32 | (uint)ncred.LastWritten.dwLowDateTime));
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        }
        catch (ArgumentOutOfRangeException)
        {
            // Invalid FILETIME — leave LastWritten as default (DateTime.MinValue)
        }

        if (ncred.CredentialBlobSize >= 2)
        {
            CredentialBlob = Marshal.PtrToStringUni(ncred.CredentialBlob, (int)ncred.CredentialBlobSize / 2);
        }
        Persistence = (Persistence)ncred.Persist;

        var AttributeCount = ncred.AttributeCount;
        if (AttributeCount > 0)
        {
            var attribSize = Marshal.SizeOf(typeof(NativeCode.NativeCredentialAttribute));
            Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);
            byte[] rawData = new byte[AttributeCount * attribSize];
            var buffer = Marshal.AllocHGlobal(attribSize);

            try
            {
                Marshal.Copy(ncred.Attributes, rawData, 0, (int)AttributeCount * attribSize);
                for (int i = 0; i < AttributeCount; i++)
                {
                    Marshal.Copy(rawData, i * attribSize, buffer, attribSize);
                    var attr = (NativeCode.NativeCredentialAttribute)Marshal.PtrToStructure(buffer,
                     typeof(NativeCode.NativeCredentialAttribute))!;
                    var key = attr.Keyword;
                    var val = new byte[attr.ValueSize];
                    Marshal.Copy(attr.Value, val, 0, (int)attr.ValueSize);

                    try
                    {
                        // Deserialize as JSON (current format)
                        var jsonStr = Encoding.UTF8.GetString(val);
                        Attributes.Add(key, JsonSerializer.Deserialize<JsonElement>(jsonStr));
                    }
                    catch (JsonException)
                    {
                        Debug.WriteLine($"Could not deserialize attribute '{key}' — data may be corrupted or in an unsupported format");
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        TargetAlias = ncred.TargetAlias;
        UserName = ncred.UserName;
        Type = (CredentialType)ncred.Type;
    }

    public Credential(NetworkCredential credential)
    {
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        CredentialBlob = credential.Password;
        UserName = String.IsNullOrWhiteSpace(credential.Domain) ? credential.UserName : credential.Domain + "\\" + credential.UserName;
        Attributes = null;
        Comment = null;
        TargetAlias = null;
        Type = CredentialType.Generic;
        Persistence = Persistence.Session;
    }

    public Credential(ICredential credential)
    {
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));

        CredentialBlob = credential.CredentialBlob;
        UserName = credential.UserName;
        if (credential.Attributes?.Count > 0)
        {
            this.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);
            foreach (var a in credential.Attributes)
            {
                Attributes.Add(a);
            }
        }
        Comment = credential.Comment;
        TargetAlias = null;
        Type = credential.Type;
        Persistence = credential.Persistence;
    }

    public Credential(string target, CredentialType type)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        Type = type;
        TargetName = target;
    }

    public NetworkCredential ToNetworkCredential()
    {
        if (!string.IsNullOrEmpty(UserName))
        {
            var userBuilder = new StringBuilder(UserName!.Length + 2);
            var domainBuilder = new StringBuilder(UserName.Length + 2);

            var returnCode = NativeCode.CredUIParseUserName(UserName, userBuilder, userBuilder.Capacity, domainBuilder, domainBuilder.Capacity);
            var lastError = Marshal.GetLastWin32Error();

            //assuming invalid account name to be not meeting condition for CredUIParseUserName
            //"The name must be in UPN or down-level format, or a certificate"
            if (returnCode == NativeCode.CredentialUIReturnCodes.InvalidAccountName)
            {
                userBuilder.Append(UserName);
            }
            else if (returnCode != NativeCode.CredentialUIReturnCodes.Success)
            {
                throw new CredentialAPIException(SR.UnableToParseUserName, "CredUIParseUserName", lastError);
            }

            return new NetworkCredential(userBuilder.ToString(), this.CredentialBlob, domainBuilder.ToString());
        }
        else
        {
            return new NetworkCredential(UserName, this.CredentialBlob);
        }
    }

    public bool SaveCredential(bool AllowBlankPassword = false)
    {
        IntPtr buffer = default;
        GCHandle pinned = default;

        if (!String.IsNullOrEmpty(this.Comment) && Encoding.Unicode.GetByteCount(this.Comment) > 256)
            throw new InvalidOperationException(SR.CommentTooLong);

        if (String.IsNullOrEmpty(this.TargetName))
            throw new InvalidOperationException(SR.TargetNameNullOrEmpty);
        else if (this.TargetName.Length > 32767)
            throw new InvalidOperationException(SR.TargetNameTooLong);

        if (!AllowBlankPassword && String.IsNullOrEmpty(this.CredentialBlob))
            throw new InvalidOperationException(SR.CredentialBlobNullOrEmpty);

        var credentialBlob = this.CredentialBlob ?? "";
        NativeCode.NativeCredential ncred = new NativeCode.NativeCredential
        {
            Comment = this.Comment,
            TargetAlias = null,
            Type = (UInt32)this.Type,
            Persist = (UInt32)this.Persistence,
            UserName = this.UserName,
            TargetName = this.TargetName,
            CredentialBlobSize = (UInt32)Encoding.Unicode.GetByteCount(credentialBlob)
        };
        if (ncred.CredentialBlobSize > MaxCredentialBlobSize)
            throw new InvalidOperationException(string.Format(SR.CredentialBlobTooLong, MaxCredentialBlobSize));

        ncred.CredentialBlob = Marshal.StringToCoTaskMemUni(credentialBlob);
        if (this.LastWritten != DateTime.MinValue)
        {
            var fileTime = this.LastWritten.ToFileTimeUtc();
            ncred.LastWritten.dwLowDateTime = (int)(fileTime & 0xFFFFFFFFL);
            ncred.LastWritten.dwHighDateTime = (int)((fileTime >> 32) & 0xFFFFFFFFL);
        }

        NativeCode.NativeCredentialAttribute[]? nativeAttribs = null;
        try
        {
            if (Attributes == null || Attributes.Count == 0)
            {
                ncred.AttributeCount = 0;
                ncred.Attributes = IntPtr.Zero;
            }
            else
            {
                if (Attributes.Count > 64)
                    throw new InvalidOperationException(SR.TooManyAttributes);

                ncred.AttributeCount = (UInt32)Attributes.Count;
                nativeAttribs = new NativeCode.NativeCredentialAttribute[Attributes.Count];
                var attribSize = Marshal.SizeOf(typeof(NativeCode.NativeCredentialAttribute));
                byte[] rawData = new byte[Attributes.Count * attribSize];
                buffer = Marshal.AllocHGlobal(attribSize);

                var i = 0;
                foreach (var a in Attributes)
                {
                    if (a.Key.Length > 256)
                        throw new ArgumentException(string.Format(SR.AttributeNameTooLong, a.Key), a.Key);
                    if (a.Value == null)
                        throw new ArgumentNullException(a.Key, string.Format(SR.AttributeValueNull, a.Key));

                    var value = JsonSerializer.SerializeToUtf8Bytes(a.Value, a.Value.GetType(), s_jsonOptions);

                    if (value.Length > 256)
                        throw new ArgumentException(string.Format(SR.AttributeValueTooLong, a.Key), a.Key);

                    var attrib = new NativeCode.NativeCredentialAttribute
                    {
                        Keyword = a.Key,
                        ValueSize = (UInt32)value.Length
                    };

                    attrib.Value = Marshal.AllocHGlobal(value.Length);
                    Marshal.Copy(value, 0, attrib.Value, value.Length);
                    nativeAttribs[i] = attrib;

                    Marshal.StructureToPtr(attrib, buffer, false);
                    Marshal.Copy(buffer, rawData, i * attribSize, attribSize);
                    i++;
                }
                pinned = GCHandle.Alloc(rawData, GCHandleType.Pinned);
                ncred.Attributes = pinned.AddrOfPinnedObject();
            }
            // Write the info into the CredMan storage.

            if (NativeCode.CredWrite(ref ncred, 0))
            {
                return true;
            }
            else
            {
                int lastError = Marshal.GetLastWin32Error();
                throw new CredentialAPIException(SR.UnableToSaveCredential, "CredWrite", lastError);
            }
        }

        finally
        {
            if (ncred.CredentialBlob != default)
                Marshal.FreeCoTaskMem(ncred.CredentialBlob);
            if (nativeAttribs != null)
            {
                foreach (var a in nativeAttribs)
                {
                    if (a.Value != default)
                        Marshal.FreeHGlobal(a.Value);
                }
                if (pinned.IsAllocated)
                    pinned.Free();
                if (buffer != default)
                    Marshal.FreeHGlobal(buffer);
            }
        }
    }

    public bool RemoveCredential()
    {
        // Make the API call using the P/Invoke signature
        var isSuccess = NativeCode.CredDelete(TargetName, (uint)Type, 0);

        if (isSuccess)
            return true;

        int lastError = Marshal.GetLastWin32Error();
        throw new CredentialAPIException(SR.UnableToDeleteCredential, "CredDelete", lastError);
    }
}
