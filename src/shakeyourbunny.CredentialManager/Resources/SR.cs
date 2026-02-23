using System.Globalization;
using System.Resources;

namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Internal string resource accessor. Loads localized strings from embedded .resx resources
/// based on <see cref="CultureInfo.CurrentUICulture"/>.
/// </summary>
internal static class SR
{
    private static readonly ResourceManager s_resourceManager =
        new ResourceManager("shakeyourbunny.CredentialManager.Resources.Strings", typeof(SR).Assembly);

    internal static string GetString(string name) =>
        s_resourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;

    internal static string Format(string name, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, GetString(name), args);

    // --- Credential validation ---

    internal static string CommentTooLong => GetString(nameof(CommentTooLong));
    internal static string TargetNameNullOrEmpty => GetString(nameof(TargetNameNullOrEmpty));
    internal static string TargetNameTooLong => GetString(nameof(TargetNameTooLong));
    internal static string CredentialBlobNullOrEmpty => GetString(nameof(CredentialBlobNullOrEmpty));
    internal static string CredentialBlobTooLong => GetString(nameof(CredentialBlobTooLong));
    internal static string TooManyAttributes => GetString(nameof(TooManyAttributes));
    internal static string AttributeNameTooLong => GetString(nameof(AttributeNameTooLong));
    internal static string AttributeValueNull => GetString(nameof(AttributeValueNull));
    internal static string AttributeValueTooLong => GetString(nameof(AttributeValueTooLong));

    // --- API errors ---

    internal static string UnableToSaveCredential => GetString(nameof(UnableToSaveCredential));
    internal static string UnableToParseUserName => GetString(nameof(UnableToParseUserName));
    internal static string UnableToDeleteCredential => GetString(nameof(UnableToDeleteCredential));
    internal static string UnableToEnumerateCredentials => GetString(nameof(UnableToEnumerateCredentials));
    internal static string UnableToReadCredential => GetString(nameof(UnableToReadCredential));

    // --- Handle errors ---

    internal static string InvalidCriticalHandle => GetString(nameof(InvalidCriticalHandle));
}
