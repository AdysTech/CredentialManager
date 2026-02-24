# Changelog

All notable changes to this project will be documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/).

## [3.1.0] - 2026-02-24

### Breaking Changes

- **`Persistance` renamed to `Persistence`** — The misspelled enum, property, and parameter
  names have been corrected across the entire API surface. Update all references:
  `Persistance` → `Persistence`, `persistance:` → `persistence:`.
- **BinaryFormatter fallback removed entirely** — The netstandard2.0 migration path for
  legacy BinaryFormatter-encoded attributes has been removed. Legacy attributes are now
  silently skipped with a debug message. This eliminates the CWE-502 attack surface
  completely rather than keeping it as a migration convenience.
- **`PromptForCredentialsConsole` return type** changed from `NetworkCredential` to
  `NetworkCredential?` — returns null when the user cancels, throws
  `CredentialAPIException` on other failures. Previously ignored the return code entirely.
- **`EnumerateICredentials` error handling** — Unexpected exceptions now throw
  `CredentialAPIException` instead of silently returning null (which was indistinguishable
  from "no credentials found").

### Security

- **Zero credential buffer in `GetBasicAuthString`** — The intermediate UTF-8 byte array
  containing `username:password` is now cleared via `Array.Clear()` after Base64 encoding.
- **Managed string limitation documented** — `CredentialBlob` property now has XML
  documentation explaining that managed strings cannot be reliably zeroed from memory.

### Fixed

- **`GetInputBuffer` buffer sizing** — Now queries the required buffer size from
  `CredPackAuthenticationBuffer` instead of using a hardcoded 1024-byte buffer.
- **Comment byte count** — `SaveCredential` now uses `Encoding.Unicode.GetByteCount()`
  instead of allocating a temporary byte array with `GetBytes().Length`.
- **`SecureZeroMemory` P/Invoke** — Added XML remarks explaining why `RtlZeroMemory` is
  used as the entry point and why it provides the same guarantees as the compiler intrinsic
  across the P/Invoke boundary.

### Added

- **6 Roslyn analyzer suites** — Microsoft.CodeAnalysis.NetAnalyzers, StyleCop,
  SecurityCodeScan, Roslynator, SonarAnalyzer, and Meziantou.Analyzer. All configured
  in `Directory.Build.props` and `.editorconfig` with zero warnings across ~2,000+ rules.
- **Source Link** — PDB files map to GitHub source for debugger step-through from NuGet.
- **NuGet package metadata** — README included in package, search tags, release notes.
- **Demo application** — `samples/CredentialManager.Demo` exercises the full API surface.

### Changed

- **Test self-containment** — Added `[TestCleanup]` to remove test credentials after each
  test. `TestGetCredentials` now saves its own credential instead of depending on
  `TestSaveCredentials` running first.
- **`TestEnumerateCredentialWithTarget`** — Added missing `[TestMethod]` attribute with
  `[Ignore]` (requires specific host-local credential).
- **Exception types** — Object state validation in `SaveCredential` now throws
  `InvalidOperationException` instead of `ArgumentException` with property names. Matches
  .NET conventions (CA2208).
- **String comparisons** — All string equality checks use `string.Equals()` with
  `StringComparison.Ordinal` (MA0006). All `Dictionary<string, ...>` constructors
  specify `StringComparer.Ordinal` (MA0002).
- **Unused parameter removed** — Private `PromptForCredentials` overload no longer accepts
  an unused `target` parameter (S1172).

---

## [3.0.0] - 2026-02-23

### Overview

Major security and modernization release following a comprehensive code audit of the
upstream AdysTech/CredentialManager library. This fork addresses critical security
vulnerabilities, modernizes the codebase to current .NET standards, and improves
overall code quality.

### Security Audit Findings

The following issues were identified during the audit:

1. **BinaryFormatter Deserialization (CRITICAL, CWE-502)** — Credential attributes
   were serialized using `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter`,
   which is deprecated (SYSLIB0011) and vulnerable to arbitrary code execution through
   crafted payloads. Any application reading credential attributes from the Windows
   Credential Store could be exploited if a malicious payload was written to the
   attribute field by another process with store access.

2. **Persistence Hardcoded to Enterprise (CRITICAL, #69)** — All credentials were
   saved with `Persistence.Enterprise` regardless of caller intent. Enterprise
   persistence syncs credentials to Active Directory domain controllers, meaning
   credentials intended to be local-only were being replicated across the domain.
   This is both a security exposure (broader attack surface) and a correctness bug.

3. **Incomplete Memory Zeroing (HIGH)** — `CriticalCredentialHandle.ReleaseHandle()`
   calls `CredFree()` without zeroing the native buffer first. The existing zeroing
   in `CredentialManager.cs` uses `Marshal.Copy` with a zero-byte array, which the
   JIT compiler is permitted to optimize away as a dead store. Credential material
   could persist in freed memory.

4. **P/Invoke Declarations Missing SetLastError (MEDIUM)** — Several `DllImport`
   attributes lack `SetLastError = true`, meaning `Marshal.GetLastWin32Error()` may
   return stale values on failure, leading to incorrect error reporting.

5. **Buffer Size Hardcoding (MEDIUM)** — Username and password buffers in
   `CredUIPromptForCredentials` are hardcoded to 100 characters instead of using
   the Windows-defined `CREDUI_MAX_USERNAME_LENGTH` (513) and
   `CREDUI_MAX_PASSWORD_LENGTH` (256). Long usernames or domain\user combinations
   could be silently truncated.

6. **Unused P/Invoke Declaration** — An unused `CredUIPromptForCredentials` extern
   declaration existed in `NativeCode.cs` (the codebase uses
   `CredUIPromptForWindowsCredentials` instead).

7. **Outdated Target Frameworks** — The library targets `net45` (out of Microsoft
   support since January 2016) and `netstandard2.0`. No modern .NET target.

### Security Fixes

- **Replace BinaryFormatter with System.Text.Json** — Credential attributes are now
  serialized as JSON-encoded UTF-8 byte arrays using `System.Text.Json.JsonSerializer`.
  A one-time migration path reads legacy BinaryFormatter-encoded attributes and
  re-saves them as JSON on next write (netstandard2.0 only; .NET 8+ cannot read
  legacy data as BinaryFormatter is disabled by the runtime).
- **Expose Persistence parameter** — `SaveCredentials()` now accepts an optional
  `Persistence` parameter (default: `LocalMachine`). Previously hardcoded to
  `Enterprise`. Fixes #69.
- **JIT-safe memory zeroing** — Native credential buffers are zeroed using
  `RtlZeroMemory` (via P/Invoke to `kernel32.dll`) before `CredFree()`. As an
  external function call, the JIT cannot optimize this away, unlike `Marshal.Copy`
  with zero bytes. `StringBuilder` buffers from credential prompts are also cleared
  after use.

### Changed

- **Target frameworks**: Drop `net45`, add `net8.0` as primary target, keep
  `netstandard2.0` for broad compatibility
- **C# language version**: Upgraded to 12.0
- **Nullable reference types**: Enabled project-wide with annotations throughout
- **File-scoped namespaces**: All source files converted
- **P/Invoke**: `SetLastError = true` added to all `DllImport` attributes
- **P/Invoke**: Buffer allocations use `CREDUI_MAX_USERNAME_LENGTH` (513) and
  `CREDUI_MAX_PASSWORD_LENGTH` (256) constants
- **P/Invoke**: Removed unused `CredUIPromptForCredentials` declaration
- **Exception handling**: Bare `catch(Exception)` blocks replaced with diagnostic output
- **Default persistence**: Changed from `Enterprise` to `LocalMachine`
- **Package rename**: Hard fork — namespace, assembly, and NuGet package renamed from
  `AdysTech.CredentialManager` to `shakeyourbunny.CredentialManager`. Upstream MIT
  attribution preserved.
- **Test dependencies**: Updated to current versions (MSTest 3.3.1,
  Microsoft.NET.Test.Sdk 17.9.0, coverlet 6.0.2)

### Added

- XML documentation on all public API methods and native structs
- `ArgumentNullException` guards on all public methods
- Migration path: legacy BinaryFormatter attributes auto-converted to JSON on read
  (netstandard2.0 only)
- `LICENSE` file (standard filename, MIT, both copyright holders)
- Comprehensive unit tests for persistence parameter variations, JSON attribute
  round-trips (string, number, struct), and null parameter handling
- `SecureZeroMemory` P/Invoke (`kernel32.dll!RtlZeroMemory`) for JIT-safe zeroing
- `CriticalCredentialHandle` now zeros credential blobs (both single and enumeration)
  before calling `CredFree`
- Internationalization: All user-facing strings extracted to .NET resource files (.resx)
  with translations for German, French, Spanish, and Italian via satellite assemblies

### Fixed

- Credential blob null handling when `AllowBlankPassword` is true and password is null
- `GetCredentialsFromOutputBuffer` buffer variable names were swapped (passwordBuf used
  maxDomain, domainBuf used maxPassword — harmless when all were 100, incorrect with
  proper constants)

---

## [2.6.0] - 2022-01-18

### Changed

- Allow saving blank passwords with an additional `AllowBlankPassword` parameter.
  Fixes [#67](https://github.com/AdysTech/CredentialManager/issues/67).

## [2.5.0] - 2021-11-10

### Changed

- Update max credential length to 2560 bytes.
  Fixes [#65](https://github.com/AdysTech/CredentialManager/issues/65).
  Thanks to @ldattilo.

## [2.4.0] - 2021-08-30

### Fixed

- Allow saving single character passwords.
  Fixes [#62](https://github.com/AdysTech/CredentialManager/issues/62).
  Thanks to @aschoelzhorn.

## [2.3.0] - 2020-12-20

### Added

- `RemoveCredential` method on `ICredential`.
  Fixes [#59](https://github.com/AdysTech/CredentialManager/issues/59).

## [2.2.0] - 2020-05-27

### Added

- `EnumerateICredentials` method.
  [#46](https://github.com/AdysTech/CredentialManager/pull/46).
  Thanks to @strzalkowski.

### Fixed

- [#47](https://github.com/AdysTech/CredentialManager/issues/47): Getting Multiple
  targets.

### Changed

- Common project properties moved to `Directory.Build.props`.

## [2.1.0] - 2020-05-20

### Changed

- Merged .NET Framework and .NET Core projects into one multi-target project.
  [#41](https://github.com/AdysTech/CredentialManager/pull/41).
  Thanks to @drewnoakes.

### Fixed

- [#42](https://github.com/AdysTech/CredentialManager/pull/42): Fix
  `NullReferenceException` when credentials not found.
  Thanks to @LePtitDev.

### Breaking Change

- [AdysTech.CredentialManager.Core](https://www.nuget.org/packages/AdysTech.CredentialManager.Core/)
  NuGet package deprecated — main package now supports .NET Core.

## [2.0.0] - 2020-04-20

### Added

- `ICredential` interface exposing properties beyond `NetworkCredential`
  (comments, attributes, persistence type).
  [#30](https://github.com/AdysTech/CredentialManager/issues/30).
- Ability to add comments to saved credentials.
- Ability to read/write attributes to credentials (binary-serialized, max 256 bytes
  each, max 64 attributes).
- `CredentialAPIException` for detailed Windows API failure reporting.

### Fixed

- [#39](https://github.com/AdysTech/CredentialManager/issues/39): Password exposed
  in process memory after saving.

### Breaking Changes

- `SaveCredentials` return type changed from `bool` to `ICredential`.
- `ToNetworkCredential` throws `InvalidOperationException` or `CredentialAPIException`
  instead of `Win32Exception`.
- `CredentialManager.CredentialType` enum removed — use `CredentialType` directly.

## Earlier Versions

### v1.9.5.0 - 2019-12-22

- Add `CredentialType` as optional parameter when saving. Thanks to @esrahofstede.

### v1.0.1.0 - 2019-12-11

- Add strong name to NuGet. Thanks to @kvvinokurov.

### v1.9.0.0 - 2019-11-09

- [#31](https://github.com/AdysTech/CredentialManager/issues/31): Support .NET
  Standard. Thanks to @RussKie.

### v1.8.0.0 - 2019-02-25

- Add `EnumerateCredentials`. Thanks to @erikjon.

### v1.7.0.0 - 2018-09-26

- Allow prefilled user name. Thanks to @jairbubbles.

### v1.6.0.0 - 2018-09-24

- Fix buffer sizes in `ParseUserName`. Thanks to @jairbubbles.

### v1.2.1.0 - 2017-10-25

- Don't crash if credential not found. Thanks to @pmiossec.
- Corrections to error message. Thanks to @nguillermin.

### v1.1.0.0 - 2017-01-09

- Initial release.
