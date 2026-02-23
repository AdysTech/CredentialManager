using System;
using System.Collections.Generic;
using System.Net;

namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Represents a credential stored in the Windows Credential Store, exposing
/// properties beyond what <see cref="NetworkCredential"/> provides (comments,
/// attributes, persistence type).
/// </summary>
public interface ICredential
{
    /// <summary>
    /// Gets or sets the type of the credential (Generic, Windows, or Certificate).
    /// </summary>
    CredentialType Type { get; set; }

    /// <summary>
    /// Gets or sets the target name that identifies this credential in the store.
    /// </summary>
    string TargetName { get; set; }

    /// <summary>
    /// Gets or sets an optional comment associated with the credential.
    /// Comments are only accessible programmatically (not visible in Windows Credential Manager UI).
    /// Maximum 256 bytes when encoded as Unicode.
    /// </summary>
    string? Comment { get; set; }

    /// <summary>
    /// Gets or sets the time the credential was last written.
    /// </summary>
    DateTime LastWritten { get; set; }

    /// <summary>
    /// Gets or sets the credential secret (typically a password or token).
    /// Maximum <see cref="Credential.MaxCredentialBlobSize"/> bytes when encoded as Unicode.
    /// </summary>
    string? CredentialBlob { get; set; }

    /// <summary>
    /// Gets or sets the persistence type (Session, LocalMachine, or Enterprise).
    /// </summary>
    Persistance Persistance { get; set; }

    /// <summary>
    /// Gets or sets custom attributes associated with the credential.
    /// Attributes are serialized as JSON. Each attribute value must be JSON-serializable
    /// and the serialized form must not exceed 256 bytes. Maximum 64 attributes per credential.
    /// When read back, attribute values are returned as <see cref="System.Text.Json.JsonElement"/>
    /// objects; use <c>JsonElement.Deserialize&lt;T&gt;()</c> to convert to the original type.
    /// </summary>
    IDictionary<string, Object>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the user name associated with the credential.
    /// </summary>
    string? UserName { get; set; }

    /// <summary>
    /// Converts this credential to a <see cref="NetworkCredential"/>, parsing
    /// domain\user format if present.
    /// </summary>
    NetworkCredential ToNetworkCredential();

    /// <summary>
    /// Saves this credential to the Windows Credential Store.
    /// </summary>
    /// <param name="AllowBlankPassword">If true, allows saving credentials with an empty password.</param>
    /// <returns>True if the credential was saved successfully.</returns>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails.</exception>
    bool SaveCredential(bool AllowBlankPassword = false);

    /// <summary>
    /// Removes this credential from the Windows Credential Store.
    /// </summary>
    /// <returns>True if the credential was removed successfully.</returns>
    /// <exception cref="CredentialAPIException">Thrown when the Windows API call fails.</exception>
    bool RemoveCredential();
}
