using System;
using System.Runtime.InteropServices;

namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Exception thrown when a Windows Credential Manager API call fails.
/// Contains the API name and Win32 error code for diagnostic purposes.
/// </summary>
[Serializable]
public class CredentialAPIException : ExternalException
{
    /// <summary>
    /// Gets the name of the Windows API function that failed.
    /// </summary>
    public string? APIName { get; internal set; }

    /// <summary>
    /// Creates a new exception for a failed credential API call.
    /// </summary>
    /// <param name="message">Description of the failure.</param>
    /// <param name="api">Name of the API function that failed (e.g. "CredWrite").</param>
    /// <param name="errorCode">Win32 error code from Marshal.GetLastWin32Error().</param>
    public CredentialAPIException(string message, string api, int errorCode) : base(message, errorCode)
    {
        APIName = api;
    }

    /// <summary>
    /// Creates a new exception with a message and error code.
    /// </summary>
    public CredentialAPIException(string message, int errorCode) : base(message, errorCode)
    {
    }

    /// <summary>
    /// Creates a new exception with a message.
    /// </summary>
    public CredentialAPIException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new exception with a message and inner exception.
    /// </summary>
    public CredentialAPIException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new exception with default message.
    /// </summary>
    public CredentialAPIException()
    {
    }

#if !NET8_0_OR_GREATER
    /// <summary>
    /// Serialization constructor for cross-AppDomain scenarios.
    /// </summary>
    protected CredentialAPIException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
#endif
}
