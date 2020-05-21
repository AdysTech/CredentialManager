## v2.1.0 [May 20, 2020]

### Release Notes

This version merges the .Net framework and core projects into one multi target project. 

### Bugfixes

- [#42](https://github.com/AdysTech/CredentialManager/pull/42): Fix NullReferenceException when credentials not found. Thanks to @LePtitDev

### Features

- [#41](https://github.com/AdysTech/CredentialManager/pull/41): Use single project to target .NET Framework & Core. SDK-style projects allow multi-targeting which makes this much simpler. Thanks to @drewnoakes 

### Breaking Change
- since [main Nuget Package](https://www.nuget.org/packages/AdysTech.CredentialManager) supports .netcore core specific Nuget package will be deprecated.


## v2.0.0 [Apr 20, 2020]

### Release Notes

This version support to expose raw credential, with additional information not available in normal `NetworkCredential` available in previous versions. Currently there is a open [issue #30](https://github.com/AdysTech/CredentialManager/issues/30) for quite some asking for this feature.

This version also adds support to store comments and additional attributes associated with a Credential object. The attributes are serialized using `BinaryFormatter` and API has 256 byte length. `BinaryFormatter` generates larger than what you think the object size is going to be, si keep an eye on that.

Comments and attributes  are only accessible programmatically. Windows always supported such a feature (via `CREDENTIALW` [structure](https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw)) but `Windows Credential Manager applet` does not have any way to show this information to user. So if an user edits the saved credentials using control panel comments and attributes gets lost. The lack of this information may be used as a tamper check. Note that this information is accessible all programs with can read write to credential store, so don't assume the information is secure from everything. 

### Bugfixes

- [#39](https://github.com/AdysTech/CredentialManager/issues/39): Password is exposed in the process memory after saving in the Windows credentials storage

### Features

- [#30](https://github.com/AdysTech/CredentialManager/issues/30): Expose properties of Credential object which are not part of generic `NetworkCredential`
- Ability to add comments to saved credentials. Use the `ICredential` returned from `SaveCredentials`, and call save on the interface for the second time after updating comment.
- Ability to read write attributes to credentials. These Attributes can be any binary data, including strings, user roles etc, as it applies to use case.
- New `CredentialAPIException` to indicate the credential API failures, with API name.

### Breaking Change
- `SaveCredentials` return type changed from `bool` to `ICredential`, giving reference to just saved instance.
- `ToNetworkCredential` doesn't throw `Win32Exception` anymore. It will throw `InvalidOperationException` or new `CredentialAPIException` instead.
- `CredentialManager.CredentialType` enum is removed. Use `CredentialType` instead.



## Acknowledging external contributors to previous versions.

### v1.9.5.0 [Dec 22, 2019]
-	Add CredentialType as an extra optional parameter when saving credentials, Thanks to @esrahofstede 

### v1.0.1.0 [Dec 11, 2019]
-	add strong name to nuget, Thanks to @kvvinokurov

### v1.9.0.0 [Nov 9, 2019]
- [#31](https://github.com/AdysTech/CredentialManager/issues/31): Support .NET Standard, Thanks to @RussKie for the issue

### v1.8.0.0 [Feb 25, 2019]
-	Add EnumerateCredentials, Thanks to @erikjon 

### v1.7.0.0 [Sep 26, 2018]
-	Allow prefilled user name, Thanks to @jairbubbles 


### v1.6.0.0 [Sep 24, 2018]
-	Fix buffer sizes in ParseUserName, Thanks to @jairbubbles 

### v1.2.1.0 [Oct 25, 2017]
-	Don't crash if credential not found, Thanks to @pmiossec

### v1.2.1.0 [Oct 25, 2017]
-	Corrections to Error message, Thanks to @nguillermin

### v1.1.0.0 [Jan 9, 2017]
-	Initial Release