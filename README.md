# CredentialManager

[![NuGet](https://img.shields.io/nuget/v/AdysTech.CredentialManager)](https://www.nuget.org/packages/AdysTech.CredentialManager)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A .NET library for storing and retrieving credentials from the Windows Credential Store.
Wraps the native `CredWrite`, `CredRead`, `CredEnumerate`, and `CredDelete` APIs via P/Invoke,
providing a safe managed interface for credential management in desktop applications, CLI tools,
and background services.

## Features

- Save, retrieve, enumerate, and remove credentials from Windows Credential Store
- Prompt users for credentials via Windows UI dialog or console
- Store comments and custom attributes (JSON-serialized, up to 64 per credential)
- Configurable persistence (Session, LocalMachine, Enterprise)
- JIT-safe memory zeroing of credential buffers via `RtlZeroMemory` P/Invoke
- Full nullable reference type annotations
- Source Link enabled for debugger integration

## Target Frameworks

| Framework | Purpose |
|-----------|---------|
| **.NET 8.0** | Primary target, full analyzer coverage |
| **.NET Standard 2.0** | Broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+, Mono, Unity) |

## Installation

### NuGet (recommended)

```
dotnet add package AdysTech.CredentialManager
```

Or in your `.csproj`:

```xml
<PackageReference Include="AdysTech.CredentialManager" Version="3.1.0" />
```

### Project Reference

For local development or when building from source:

```xml
<ProjectReference Include="path/to/src/AdysTech.CredentialManager/AdysTech.CredentialManager.csproj" />
```

## Quick Start

```csharp
using System.Net;
using AdysTech.CredentialManager;

// Save
var cred = new NetworkCredential("user", "password", "domain");
CredentialManager.SaveCredentials("MyApp:api-token", cred);

// Retrieve
var stored = CredentialManager.GetCredentials("MyApp:api-token");
if (stored != null)
    Console.WriteLine($"User: {stored.UserName}, Domain: {stored.Domain}");

// Remove
CredentialManager.RemoveCredentials("MyApp:api-token");
```

## Integration Guide

### Adding to Your Project

1. Install the NuGet package (see Installation above).

2. Add the using directive:
   ```csharp
   using AdysTech.CredentialManager;
   ```

3. Use `CredentialManager` as a static class — no instantiation needed.

### Target Naming Convention

Use a consistent prefix for your target names to avoid collisions with other applications:

```csharp
const string TargetPrefix = "MyApp:";
CredentialManager.SaveCredentials($"{TargetPrefix}api-key", cred);
CredentialManager.SaveCredentials($"{TargetPrefix}oauth-token", cred);
```

### Typical Integration Pattern

```csharp
public static class SecureStore
{
    private const string Prefix = "MyApp:";

    public static void StoreToken(string key, string token, string user = "token")
    {
        var cred = new NetworkCredential(user, token);
        CredentialManager.SaveCredentials($"{Prefix}{key}", cred,
            persistence: Persistence.LocalMachine);
    }

    public static string? GetToken(string key)
    {
        return CredentialManager.GetCredentials($"{Prefix}{key}")?.Password;
    }

    public static void RemoveToken(string key)
    {
        try { CredentialManager.RemoveCredentials($"{Prefix}{key}"); }
        catch (CredentialAPIException) { /* not found — already removed */ }
    }
}
```

### Error Handling

All public methods throw `ArgumentNullException` for null parameters and `CredentialAPIException`
for Windows API failures. `GetCredentials` and `GetICredential` return `null` when the target
is not found (not an error).

```csharp
try
{
    CredentialManager.SaveCredentials("target", cred);
}
catch (CredentialAPIException ex)
{
    // ex.Message  — human-readable description
    // ex.ApiName  — Win32 function that failed (e.g. "CredWrite")
    // ex.ErrorCode — Win32 error code
    Console.Error.WriteLine($"{ex.ApiName} failed: {ex.Message} (error {ex.ErrorCode})");
}
```

### Platform Considerations

This library is Windows-only (P/Invoke to `advapi32.dll` and `credui.dll`). For cross-platform
credential storage, use it behind an abstraction:

```csharp
public interface ICredentialStore
{
    void Save(string key, string user, string password);
    NetworkCredential? Get(string key);
    void Remove(string key);
}
```

Implement with `CredentialManager` on Windows and an alternative (e.g., libsecret/keyring)
on Linux/macOS.

## API Reference

### Static Methods on `CredentialManager`

| Method | Description |
|--------|-------------|
| `SaveCredentials(target, credential, ...)` | Save a `NetworkCredential` to the store. Returns `ICredential` on success. |
| `GetCredentials(target, type)` | Retrieve as `NetworkCredential`. Returns `null` if not found. |
| `GetICredential(target, type)` | Retrieve as `ICredential` (includes comment, attributes, persistence). |
| `EnumerateCredentials(target?)` | List all credentials, optionally filtered by target prefix. |
| `EnumerateICredentials(target?)` | Same as above, returning `ICredential` objects. |
| `RemoveCredentials(target, type)` | Delete a credential from the store. |
| `PromptForCredentials(target, ...)` | Show the Windows credential dialog. |
| `PromptForCredentialsConsole(target)` | Console-based credential prompt. |
| `GetBasicAuthString(credential)` | Extension method: Base64-encode `user:password` for HTTP Basic Auth. |

### ICredential Interface

`ICredential` exposes properties beyond `NetworkCredential`:

| Property | Type | Description |
|----------|------|-------------|
| `TargetName` | `string` | The target identifier |
| `UserName` | `string?` | Username |
| `CredentialBlob` | `string?` | Password or token |
| `Comment` | `string?` | Comment (visible via API only, not in Windows UI) |
| `Persistence` | `Persistence` | Storage scope |
| `Type` | `CredentialType` | Generic or Windows (domain) |
| `Attributes` | `IDictionary<string, Object>?` | JSON-serialized key-value pairs (max 64, 256 bytes each) |
| `LastWritten` | `DateTime` | Last modification timestamp |

### Persistence

| Value | Behavior |
|-------|----------|
| `Session` | Exists only for the current logon session, cleared on reboot |
| `LocalMachine` | **(Default)** Persisted locally, not synced to domain controllers |
| `Enterprise` | Persisted and synced to Active Directory domain controllers |

## Attributes

Store structured metadata alongside credentials using JSON-serialized attributes:

```csharp
var cred = (new NetworkCredential("user", "token")).ToICredential()!;
cred.TargetName = "MyApp:service";
cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal)
{
    { "role", "admin" },
    { "expiresAt", DateTime.UtcNow.AddHours(1).ToString("o") },
    { "retryCount", 3 }
};
cred.SaveCredential();

// Reading back — attributes are returned as JsonElement
var stored = CredentialManager.GetICredential("MyApp:service");
var role = ((JsonElement)stored!.Attributes!["role"]).GetString();
var count = ((JsonElement)stored.Attributes["retryCount"]).GetInt32();
```

Constraints: each attribute value must serialize to 256 bytes or less, maximum 64 attributes
per credential, attribute keys maximum 256 characters.

## Upgrading from v2.x

### BinaryFormatter Attributes

Legacy BinaryFormatter-encoded attributes can no longer be read. The BinaryFormatter
compatibility layer has been removed entirely. Legacy attributes are silently skipped
with a debug message. Re-save credentials to convert them to JSON format.

### Attribute Type Changes

Attribute values are now `JsonElement` objects instead of the original .NET types:

```csharp
// v2.x
var role = (string)cred.Attributes["role"];

// v3.x
var role = ((JsonElement)cred.Attributes["role"]).GetString();
// Complex types:
var info = ((JsonElement)cred.Attributes["info"]).Deserialize<MyType>();
```

### Spelling Corrections

The misspelled `Persistance` enum, properties, and parameters have been corrected to
`Persistence`. Update all references accordingly.

### Default Persistence

The default persistence changed from `Enterprise` to `LocalMachine`. If your application
relies on domain-replicated credentials, explicitly pass `Persistence.Enterprise`.

## Security

- **No BinaryFormatter** — attribute serialization uses `System.Text.Json` exclusively.
  BinaryFormatter was removed due to its critical deserialization vulnerability (CWE-502).
- **JIT-safe memory zeroing** — native credential buffers are zeroed using `RtlZeroMemory`
  via P/Invoke before being freed. The JIT cannot optimize away an external function call,
  unlike `Marshal.Copy` with zero bytes.
- **Configurable persistence** — credentials default to `LocalMachine` (not `Enterprise`),
  preventing unintended replication across domain controllers.
- **Correct buffer sizes** — credential UI prompts use the Windows-defined constants
  `CREDUI_MAX_USERNAME_LENGTH` (513) and `CREDUI_MAX_PASSWORD_LENGTH` (256).
- **Static analysis** — the library builds with zero warnings across 6 Roslyn analyzer suites:
  Microsoft.CodeAnalysis.NetAnalyzers, StyleCop, SecurityCodeScan, Roslynator,
  SonarAnalyzer, and Meziantou.Analyzer (~2,000+ combined rules).

> **Note:** `CredentialBlob` is a managed `string`, which the GC may copy or retain in memory.
> For maximum security, keep credential objects short-lived and avoid caching password values.

## Building

```
dotnet build -c Release
dotnet test -c Release
```

The project requires Windows for testing (credential store access via interactive logon session).

## License

[MIT](LICENSE) — Copyright (c) 2016-2026 Adys Tech
