 [![Build status](https://ci.appveyor.com/api/projects/status/b6osdeuob7qeuivr?svg=true)](https://ci.appveyor.com/project/AdysTech/credentialmanager)

 
### Nuget Package
[AdysTech.CredentialManager](https://www.nuget.org/packages/AdysTech.CredentialManager/)

Supports .NET Framework 4.5+, and .NET Standard 2.1+.

#### Latest Download
[AdysTech.CredentialManager](https://ci.appveyor.com/api/buildjobs/so3ev8bmq51pp2im/artifacts/AdysTech.CredentialManager%2Fbin%2FCredentialManager.zip)

# CredentialManager
C# wrapper around CredWrite / CredRead functions to store and retrieve from Windows Credential Store.
Windows OS comes equipped with a very secure robust [Credential Manager](https://technet.microsoft.com/en-us/library/jj554668.aspx) from Windows XP onwards, and [good set of APIs](https://msdn.microsoft.com/en-us/library/windows/desktop/aa374731(v=vs.85).aspx#credentials_management_functions) to interact with it. However .NET Framework did not provide any standard way to interact with this vault [until Windows 8.1](https://msdn.microsoft.com/en-us/library/windows/apps/windows.security.credentials.aspx).

Microsoft Peer Channel blog (WCF team) has written [a blog post](http://blogs.msdn.com/b/peerchan/archive/2005/11/01/487834.aspx) in 2005 which provided basic structure of using the Win32 APIs for credential management in .NET.
I used their code, and improved up on it to add `PromptForCredentials` function to display a dialog to get the credentials from user.

Need: Many web services and REST Urls use basic authentication. .Net does not have a way to generate basic auth text (username:password encoded in Base64) for the current logged in user, with their credentials.
`ICredential.GetCredential (uri, "Basic")` does not provide a way to get current user security context either as it will expose the current password in plain text. So only way to retrieve Basic auth text is to prompt the user for the credentials and storing it, or assume some stored credentials in Windows store, and retrieving it.

This project provides access to all three
#### 1. Prompt user for Credentials
```C#
var cred = CredentialManager.PromptForCredentials ("Some Webservice", ref save, "Please provide credentials", "Credentials for service");
```            

#### 2. Save Credentials
```C#
var cred = new NetworkCredential ("TestUser", "Pwd");
CredentialManager.SaveCredentials ("TestSystem", cred);
```            

#### 3. Retrieve saved Credentials
```C#
var cred = CredentialManager.GetCredentials ("TestSystem");
```            

With v2.0 release exposes raw credential, with additional information not available in normal `NetworkCredential` available in previous versions. This library also allows to store comments and additional attributes associated with a Credential object. The attributes are serialized using `BinaryFormatter` and API has 256 byte length. `BinaryFormatter` generates larger than what you think the object size is going to be, si keep an eye on that.

Comments and attributes  are only accessible programmatically. Windows always supported such a feature (via `CREDENTIALW` [structure](https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw)) but `Windows Credential Manager applet` does not have any way to show this information to user. So if an user edits the saved credentials using control panel comments and attributes gets lost. The lack of this information may be used as a tamper check. Note that this information is accessible all programs with can read write to credential store, so don't assume the information is secure from everything. 

#### 4. Save and retrieve credentials with comments and attributes
```C#
    var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential();
    cred.TargetName = "TestSystem_Attributes";
    cred.Attributes = new Dictionary<string, Object>();
    var sample = new SampleAttribute() { role = "regular", created = DateTime.UtcNow };
    cred.Attributes.Add("sampleAttribute", sample);
    cred.Comment = "This comment is only visible via API, not in Windows UI";
    cred.SaveCredential();
```  

#### 5. Getting ICredential from previously saved credential
```C#
    var cred =  CredentialManager.GetICredential(TargetName);
    cred.Comment = "Update the credential data and save back";
    cred.SaveCredential();
``` 