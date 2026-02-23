using System.Net;

namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Extension methods for credential types.
/// </summary>
public static class CredentialExtensions
{
    /// <summary>
    /// Converts a <see cref="NetworkCredential"/> to an <see cref="ICredential"/>
    /// that can be used with the Windows Credential Store (supports comments,
    /// attributes, and persistence configuration).
    /// </summary>
    /// <param name="cred">The network credential to convert. May be null.</param>
    /// <returns>An <see cref="ICredential"/> wrapping the credential, or null if input is null.</returns>
    public static ICredential? ToICredential(this NetworkCredential? cred)
    {
        if (cred == null)
        {
            return null;
        }

        return new Credential(cred);
    }
}
