namespace shakeyourbunny.CredentialManager;

/// <summary>
/// Specifies the type of credential stored in the Windows Credential Store.
/// </summary>
public enum CredentialType : uint
{
    /// <summary>
    /// Generic credential, not associated with any particular authentication package.
    /// This is the most common type for application-managed credentials.
    /// </summary>
    Generic = 1,

    /// <summary>
    /// Windows domain credential (CRED_TYPE_DOMAIN_PASSWORD).
    /// The credential blob can only be read by the authentication packages.
    /// </summary>
    Windows = 2,

    /// <summary>
    /// Certificate-based credential (CRED_TYPE_CERTIFICATE).
    /// </summary>
    Certificate = 3
}

/// <summary>
/// Specifies how a credential is persisted in the Windows Credential Store.
/// </summary>
public enum Persistence : uint
{
    /// <summary>
    /// The credential persists for the life of the logon session.
    /// It is not visible to other logon sessions of the same user and is
    /// not persisted across reboots.
    /// </summary>
    Session = 1,

    /// <summary>
    /// The credential persists on the local machine. It is not visible to
    /// other machines and is not replicated to domain controllers.
    /// This is the recommended default for most applications.
    /// </summary>
    LocalMachine = 2,

    /// <summary>
    /// The credential persists on the local machine and is replicated to
    /// Active Directory domain controllers. Use with caution — this
    /// broadens the attack surface by syncing credentials across the domain.
    /// </summary>
    Enterprise = 3
}
