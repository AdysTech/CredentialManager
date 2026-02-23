# CredentialManager

C# wrapper around Windows Credential Manager APIs (`CredWrite` / `CredRead` / `CredEnumerate`)
to store and retrieve credentials from the Windows Credential Store.

Forked from [AdysTech/CredentialManager](https://github.com/AdysTech/CredentialManager) with
security hardening, modernized targets, and code quality improvements. See [CHANGELOG.md](CHANGELOG.md)
for the full audit findings and changes.

## Target Frameworks

- **.NET 8.0** (primary)
- **.NET Standard 2.0** (broad compatibility)

## Features

- Save, retrieve, enumerate, and remove credentials from Windows Credential Store
- Prompt users for credentials via Windows UI dialog or console
- Store comments and custom attributes with credentials (JSON-serialized)
- Configurable persistence (Session, LocalMachine, Enterprise)
- Secure memory zeroing of credential buffers via `RtlZeroMemory` P/Invoke
- Full nullable reference type annotations

## Installation

```
dotnet add package shakeyourbunny.CredentialManager
```

Or add a project reference:

```xml
<ProjectReference Include="path/to/src/shakeyourbunny.CredentialManager/shakeyourbunny.CredentialManager.csproj" />
```

## Usage

### 1. Save Credentials

```csharp
var cred = new NetworkCredential("TestUser", "Pwd");
CredentialManager.SaveCredentials("MyApp", cred);

// With explicit persistence (default is LocalMachine)
CredentialManager.SaveCredentials("MyApp", cred, persistance: Persistance.Session);
```

### 2. Retrieve Credentials

```csharp
var cred = CredentialManager.GetCredentials("MyApp");
if (cred != null)
    Console.WriteLine($"User: {cred.UserName}");
```

### 3. Prompt User for Credentials

```csharp
bool save = false;
var cred = CredentialManager.PromptForCredentials("My Service",
    ref save, "Please log in", "Credentials Required");
```

### 4. Save and Retrieve Attributes

Attributes are serialized as JSON. Each attribute value must be JSON-serializable and
the serialized form must not exceed 256 bytes. When read back, attribute values are
returned as `JsonElement` objects.

```csharp
var cred = (new NetworkCredential("user", "pass", "domain")).ToICredential();
cred.TargetName = "MyApp_WithAttribs";
cred.Attributes = new Dictionary<string, Object>
{
    { "role", "admin" },
    { "tokenExpiry", DateTime.UtcNow.AddHours(1) }
};
cred.Comment = "This comment is only visible via API, not in Windows UI";
cred.SaveCredential();

// Reading back — attributes are JsonElement
var stored = CredentialManager.GetICredential("MyApp_WithAttribs");
string role = ((JsonElement)stored.Attributes["role"]).GetString();
```

### 5. Enumerate and Remove Credentials

```csharp
var all = CredentialManager.EnumerateICredentials();
CredentialManager.RemoveCredentials("MyApp");
```

### 6. Basic Auth Header

```csharp
var cred = CredentialManager.GetCredentials("MyApp");
string authHeader = cred.GetBasicAuthString(); // Base64 "user:pass"
```

## Persistence

The `SaveCredentials` method accepts an optional `Persistance` parameter:

| Value | Behavior |
|-------|----------|
| `Session` | Credential exists only for the current logon session, not persisted across reboots |
| `LocalMachine` | **(Default)** Persisted locally, not synced to domain controllers |
| `Enterprise` | Persisted and synced to Active Directory domain controllers |

Previous versions hardcoded `Enterprise` for all credentials. v3.0.0 changes the default
to `LocalMachine` to avoid unintended credential replication across domain controllers.

## Security Improvements (v3.0.0)

- **Replaced BinaryFormatter** with `System.Text.Json` for attribute serialization.
  BinaryFormatter is deprecated (SYSLIB0011) and vulnerable to arbitrary code execution
  via deserialization gadgets (CWE-502).
- **JIT-safe memory zeroing** using `RtlZeroMemory` via P/Invoke. Unlike `Marshal.Copy`
  with zero bytes, the JIT cannot optimize away an external P/Invoke call.
- **Configurable persistence** — no longer hardcoded to `Enterprise`.
- **Correct buffer sizes** for credential UI prompts (`CREDUI_MAX_USERNAME_LENGTH`,
  `CREDUI_MAX_PASSWORD_LENGTH`).

## Migration from v2.x

### BinaryFormatter Attributes

If you stored credential attributes using v2.x (BinaryFormatter serialization), the
library includes a one-time migration path:

- **On .NET Standard 2.0**: Legacy BinaryFormatter attributes are automatically read and
  can be re-saved as JSON by calling `SaveCredential()` again.
- **On .NET 8.0+**: BinaryFormatter is disabled by the runtime. Legacy attributes cannot
  be read. Use the netstandard2.0 build for one-time migration if needed.

### Attribute Type Changes

Attribute values read back from the store are now `JsonElement` objects instead of the
original .NET types. Use `JsonElement.Deserialize<T>()` to convert:

```csharp
// v2.x
var role = (string)cred.Attributes["role"];

// v3.0.0
var role = ((JsonElement)cred.Attributes["role"]).GetString();
// or for complex types:
var info = ((JsonElement)cred.Attributes["info"]).Deserialize<MyType>();
```

### Default Persistence

The default persistence changed from `Enterprise` to `LocalMachine`. If your application
relies on domain-replicated credentials, explicitly pass `Persistance.Enterprise`.

## Building

```
dotnet build
dotnet test
```

## License

[MIT](LICENSE) — Copyright (c) 2018 Adys Tech, Copyright (c) 2026 shakeyourbunny
