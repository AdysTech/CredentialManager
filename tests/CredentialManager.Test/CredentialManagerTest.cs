using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using shakeyourbunny.CredentialManager;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.Json;

#pragma warning disable SCS0015 // Hardcoded passwords — test credentials are intentional

namespace CredentialManagerTest;

[TestClass]
public class CredentialManagerTest
{
    private const string uName = "ZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrh";
    private const string pwd = "5NJuqKfJBtAZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrhcvnnDPFHEn3L";
    private const string domain = "test.example.com";

    private static readonly JsonSerializerOptions s_jsonOptions = new() { IncludeFields = true };

    /// <summary>
    /// All target names created by tests. Cleaned up in TestCleanup to ensure
    /// tests are self-contained and don't depend on execution order.
    /// </summary>
    private static readonly string[] TestTargets =
    {
        "TestSystem", "TestCredWithoutUserName", "TestCredWithPasswordSingleCharacter",
        "TestSystem_comment", "TestSystem_Attributes", "TestSystem_LongComment",
        "TestSystem_LongPassword", "TestSystem_nullPwd", "TestWindowsCredential",
        "TestDeletingWindowsCredential", "TestSystem_DefaultPersist",
        "TestSystem_Session", "TestSystem_Enterprise", "TestSystem_JsonRoundTrip_Str",
        "TestSystem_JsonRoundTrip_Num", "TestSystem_JsonRoundTrip_Struct",
        "TestSystem1"
    };

    [TestCleanup]
    public void TestCleanup()
    {
        foreach (var target in TestTargets)
        {
            try { CredentialManager.RemoveCredentials(target); } catch (CredentialAPIException) { /* may not exist */ }
            try { CredentialManager.RemoveCredentials(target, CredentialType.Windows); } catch (CredentialAPIException) { /* may not exist */ }
        }
    }

    struct SampleAttribute
    {
        public string role;
        public DateTime created;
    }

    // -------------------------------------------------------------------
    // Basic credential operations
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials()
    {
        var cred = new NetworkCredential(uName, pwd, domain);
        Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetCredentials()
    {
        // Self-contained: save first, then retrieve
        var cred = new NetworkCredential(uName, pwd, domain);
        CredentialManager.SaveCredentials("TestSystem", cred);

        var retrieved = CredentialManager.GetCredentials("TestSystem");
        Assert.IsNotNull(retrieved, "GetCredential failed");
        Assert.IsTrue(string.Equals(uName, retrieved.UserName, StringComparison.Ordinal) && string.Equals(pwd, retrieved.Password, StringComparison.Ordinal) && string.Equals(domain, retrieved.Domain, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetCredentials_NonExistantCredential()
    {
        var cred = CredentialManager.GetCredentials("TotallyNonExistingTarget");
        Assert.IsNull(cred);
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetCredentials_NullUserName()
    {
        var cred = new NetworkCredential(string.Empty, "P@$$w0rd");
        CredentialManager.SaveCredentials("TestCredWithoutUserName", cred);
        var cred1 = CredentialManager.GetCredentials("TestCredWithoutUserName");
        Assert.IsTrue(string.Equals(cred1!.UserName, cred.UserName, StringComparison.Ordinal) && string.Equals(cred1.Password, cred.Password, StringComparison.Ordinal) && string.Equals(cred1.Domain, cred.Domain, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetCredentials_PasswordLengthOne()
    {
        var cred = new NetworkCredential("admin", "P");
        CredentialManager.SaveCredentials("TestCredWithPasswordSingleCharacter", cred);
        var cred1 = CredentialManager.GetCredentials("TestCredWithPasswordSingleCharacter");
        Assert.IsTrue(string.Equals(cred1!.Password, cred.Password, StringComparison.Ordinal), "Saved and retrieved password doesn't match");
    }

    // -------------------------------------------------------------------
    // ICredential: comments and attributes
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestICredential_Comment()
    {
        var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential()!;
        cred.TargetName = "TestSystem_comment";
        cred.Comment = "This comment is only visible via API, not in Windows UI";
        Assert.IsTrue(cred.SaveCredential(), "SaveCredential on ICredential failed");

        var cred1 = CredentialManager.GetICredential(cred.TargetName);
        Assert.IsNotNull(cred1, "GetICredential failed");
        Assert.IsTrue(string.Equals(cred1.UserName, cred.UserName, StringComparison.Ordinal) && string.Equals(cred1.CredentialBlob, cred.CredentialBlob, StringComparison.Ordinal) && string.Equals(cred1.Comment, cred.Comment, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_Attributes()
    {
        var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential()!;
        cred.TargetName = "TestSystem_Attributes";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);

        var sample = new SampleAttribute() { role = "regular", created = DateTime.UtcNow };
        cred.Attributes.Add("sampleAttribute", sample);

        Assert.IsTrue(cred.SaveCredential(), "SaveCredential on ICredential failed");
        var cred1 = CredentialManager.GetICredential(cred.TargetName);
        Assert.IsNotNull(cred1, "GetICredential failed");
        Assert.IsTrue(string.Equals(cred1.UserName, cred.UserName, StringComparison.Ordinal) && string.Equals(cred1.CredentialBlob, cred.CredentialBlob, StringComparison.Ordinal) && cred1.Attributes?.Count == cred.Attributes?.Count, "Saved and retrieved data doesn't match");

        // Attributes come back as JsonElement — deserialize to verify round-trip
        var jsonOptions = s_jsonOptions;
        var retrieved = ((JsonElement)cred1.Attributes!["sampleAttribute"]).Deserialize<SampleAttribute>(jsonOptions);
        Assert.IsTrue(string.Equals(retrieved.role, sample.role, StringComparison.Ordinal), "Saved and retrieved attribute data doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_LongComment()
    {
        string test = "test";
        var cred = (new NetworkCredential(test, test, test)).ToICredential()!;
        cred.TargetName = "TestSystem_LongComment";
        cred.Comment = new String('*', 257);
        Assert.ThrowsException<InvalidOperationException>(() => cred.SaveCredential(), "SaveCredential didn't throw InvalidOperationException for larger than 256 byte Comment");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_LongPassword()
    {
        int tooLong = 2 * Credential.MaxCredentialBlobSize;
        string test = "test";
        var cred = (new NetworkCredential(test, new String('*', tooLong), test)).ToICredential()!;
        cred.TargetName = "TestSystem_LongPassword";
        Assert.ThrowsException<InvalidOperationException>(() => cred.SaveCredential(),
            $"SaveCredential didn't throw InvalidOperationException for exceeding {Credential.MaxCredentialBlobSize} bytes.");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_LongTokenShouldWork()
    {
        // Tokens can be rather large. 1040: a size that can be stored.
        const int tokenLength = 1040;
        Assert.IsTrue(tokenLength < Credential.MaxCredentialBlobSize, "This test is supposed to verify a valid length.");

        string test = "longPasswordTest";
        var net = new NetworkCredential(test, new String('1', tokenLength), test);
        ICredential cred = net.ToICredential()!;
        cred.TargetName = "TestSystem_LongPassword";
        Assert.IsNotNull(cred.SaveCredential(), "SaveCredential should handle passwords of token size");

        var cred1 = CredentialManager.GetCredentials("TestSystem_LongPassword");
        Assert.IsTrue(string.Equals(cred1!.Password, net.Password, StringComparison.Ordinal), "Saved and retrieved password doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_AttributesNullValue()
    {
        string test = "test";
        var cred = (new NetworkCredential(test, test, test)).ToICredential()!;
        cred.TargetName = "TestSystem_Attributes";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);
        cred.Attributes.Add("sampleAttribute", null!);

        Assert.ThrowsException<ArgumentNullException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentNullException for null valued Attribute");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_AttributesLargeValue()
    {
        string test = "test";
        var cred = (new NetworkCredential(test, test, test)).ToICredential()!;
        cred.TargetName = "TestSystem_Attributes";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);
        // A 300-char string serializes to 302+ bytes in JSON (with quotes), exceeding the 256-byte limit
        cred.Attributes.Add("sampleAttribute", new string('x', 300));

        Assert.ThrowsException<ArgumentException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentException for larger than 256 byte Attribute");
    }

    // -------------------------------------------------------------------
    // Enumeration
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestEnumerateCredentials()
    {
        // Ensure at least one credential exists
        CredentialManager.SaveCredentials("TestSystem", new NetworkCredential(uName, pwd, domain));

        var creds = CredentialManager.EnumerateCredentials();
        Assert.IsNotNull(creds, "EnumerateCredentials failed");
        Assert.IsTrue(creds?.Count > 0, "No credentials found after saving one");
    }

    [TestMethod, TestCategory("CI")]
    public void TestEnumerateICredentials()
    {
        // Ensure at least one credential exists
        CredentialManager.SaveCredentials("TestSystem", new NetworkCredential(uName, pwd, domain));

        var creds = CredentialManager.EnumerateICredentials();
        Assert.IsNotNull(creds, "EnumerateICredentials failed");
        Assert.IsTrue(creds?.Count > 0, "No credentials found after saving one");
    }

    /// <summary>
    /// This test assumes you have a Generic Credential for https://github.com stored on your system.
    /// </summary>
    [TestMethod, Ignore("Requires git:https://github.com credential on the host system")]
    public void TestEnumerateCredentialWithTarget()
    {
        var creds = CredentialManager.EnumerateCredentials(@"git:https://github.com");
        Assert.IsNotNull(creds, "EnumerateCredentials failed");
        Assert.IsTrue(creds?.Count > 0, "No credentials stored in the system");
    }

    // -------------------------------------------------------------------
    // Credential types and deletion
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_Windows()
    {
        var cred = new NetworkCredential("admin", "P@$$w0rd");
        CredentialManager.SaveCredentials("TestWindowsCredential", cred, CredentialType.Windows);
        var cred1 = CredentialManager.GetCredentials("TestWindowsCredential", CredentialType.Windows);
        //https://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx
        //CredentialType.Windows internally gets translated to CRED_TYPE_DOMAIN_PASSWORD
        //as per MSDN, for this type CredentialBlob can only be read by the authentication packages.
        Assert.IsTrue(cred1 != null && string.Equals(cred1.UserName, cred.UserName, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
    }

    [TestMethod, TestCategory("CI")]
    public void TestDeleteCredentials_Windows()
    {
        var cred = new NetworkCredential("admin", "P@$$w0rd");
        var saved = CredentialManager.SaveCredentials("TestDeletingWindowsCredential", cred, CredentialType.Windows);
        Assert.IsNotNull(saved, "SaveCredential on ICredential failed");

        var cred1 = CredentialManager.GetICredential(saved.TargetName, CredentialType.Windows);
        Assert.IsNotNull(cred1, "GetICredential failed");
        Assert.IsTrue(string.Equals(cred1.UserName, saved.UserName, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
        Assert.IsTrue(CredentialManager.RemoveCredentials(saved.TargetName, saved.Type), "RemoveCredentials returned false");

        cred1 = CredentialManager.GetICredential(saved.TargetName);
        Assert.IsNull(cred1, "Deleted credential was read");
    }

    [TestMethod, TestCategory("CI")]
    public void TestDeleteCredentials_Enumerated()
    {
        var credentials = CredentialManager.EnumerateICredentials();

        if (credentials != null)
        {
            credentials.ForEach(x => { if (x.Type == CredentialType.Windows) Assert.IsTrue(x.RemoveCredential(), "RemoveCredentials returned false"); });
        }
    }

    // -------------------------------------------------------------------
    // Empty/blank password support
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_EmptyPassword()
    {
        var cred = new NetworkCredential(uName, "", domain);
        Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem_nullPwd", cred, AllowNullPassword: true), "SaveCredential failed");
    }

    // -------------------------------------------------------------------
    // Username parsing
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void Test_ParseUserName_supports_long_name()
    {
        var longUserName = "ksdqkdbkbqskdbqskdqsdsqdqsdjsqdjqsdjlqsjd@domain.com";
        Assert.IsTrue(CredentialManager.ParseUserName(longUserName, 100, 100, out string user, out string domain_parsed));

        Assert.AreEqual(longUserName, user);
        Assert.AreEqual("", domain_parsed);
    }

    [TestMethod, TestCategory("CI")]
    public void Test_ParseUserName_returns_false_if_buffer_is_too_small()
    {
        var longUserName = "ksdqkdbkbqskdbqskdqsdsqdqsdjsqdjqsdjlqsjd@domain.com";
        Assert.IsFalse(CredentialManager.ParseUserName(longUserName, 10, 100, out string user, out string domain_parsed));
        Assert.AreEqual("", user);
        Assert.AreEqual("", domain_parsed);
    }

    [TestMethod, TestCategory("CI")]
    public void Test_ParseUserName_supports_domain_name()
    {
        Assert.IsTrue(CredentialManager.ParseUserName("domain.com\\mike", 100, 100, out string user, out string domain_parsed));

        Assert.AreEqual("mike", user);
        Assert.AreEqual("domain.com", domain_parsed);
    }

    // -------------------------------------------------------------------
    // User prompting (interactive — not run in CI)
    // -------------------------------------------------------------------

    [TestMethod]
    public void TestPromptForCredentials()
    {
        bool save = false;
        Assert.IsNotNull(CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service"), "PromptForCredentials failed");
    }

    [TestMethod]
    public void IntegrationTest()
    {
        bool save = true;
        var cred = CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service");
        Assert.IsNotNull(cred, "PromptForCredentials failed");
        if (save)
        {
            var usr = cred.UserName;
            var pwdLocal = cred.Password;
            var dmn = cred.Domain;
            Debug.WriteLine("Usr:{0}, Pwd{1}, Dmn{2}", usr, pwdLocal, dmn);
            Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
            cred = CredentialManager.GetCredentials("TestSystem");
            Assert.IsNotNull(cred, "GetCredential failed");
            Assert.IsTrue(string.Equals(usr, cred.UserName, StringComparison.Ordinal) && string.Equals(pwdLocal, cred.Password, StringComparison.Ordinal) && string.Equals(dmn, cred.Domain, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
        }
    }

    [TestMethod]
    public void IntegrationTest_with_prefilled_username()
    {
        bool save = true;
        var cred = CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service", "mike.flemming@domain.com");
        Assert.IsNotNull(cred, "PromptForCredentials failed");
        if (save)
        {
            var usr = cred.UserName;
            var pwdLocal = cred.Password;
            var dmn = cred.Domain;
            Debug.WriteLine("Usr:{0}, Pwd{1}, Dmn{2}", usr, pwdLocal, dmn);
            Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem1", cred), "SaveCredential failed");
            cred = CredentialManager.GetCredentials("TestSystem1");
            Assert.IsNotNull(cred, "GetCredential failed");
            Assert.IsTrue(string.Equals(usr, cred.UserName, StringComparison.Ordinal) && string.Equals(pwdLocal, cred.Password, StringComparison.Ordinal) && string.Equals(dmn, cred.Domain, StringComparison.Ordinal), "Saved and retrieved data doesn't match");
        }
    }

    // -------------------------------------------------------------------
    // Persistence parameter (v3.1.0)
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_DefaultPersistence_IsLocalMachine()
    {
        var cred = new NetworkCredential(uName, pwd, domain);
        var saved = CredentialManager.SaveCredentials("TestSystem_DefaultPersist", cred);
        Assert.IsNotNull(saved, "SaveCredential failed");
        Assert.AreEqual(Persistence.LocalMachine, saved.Persistence, "Default persistence should be LocalMachine");
        CredentialManager.RemoveCredentials("TestSystem_DefaultPersist");
    }

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_SessionPersistence()
    {
        var cred = new NetworkCredential(uName, pwd, domain);
        var saved = CredentialManager.SaveCredentials("TestSystem_Session", cred, persistence: Persistence.Session);
        Assert.IsNotNull(saved, "SaveCredential with Session persistence failed");
        Assert.AreEqual(Persistence.Session, saved.Persistence, "Persistence should be Session");
        CredentialManager.RemoveCredentials("TestSystem_Session");
    }

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_EnterprisePersistence()
    {
        var cred = new NetworkCredential(uName, pwd, domain);
        var saved = CredentialManager.SaveCredentials("TestSystem_Enterprise", cred, persistence: Persistence.Enterprise);
        Assert.IsNotNull(saved, "SaveCredential with Enterprise persistence failed");
        Assert.AreEqual(Persistence.Enterprise, saved.Persistence, "Persistence should be Enterprise");
        CredentialManager.RemoveCredentials("TestSystem_Enterprise");
    }

    // -------------------------------------------------------------------
    // JSON attribute serialization round-trip (v3.1.0)
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestICredential_JsonAttributeRoundTrip_String()
    {
        var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential()!;
        cred.TargetName = "TestSystem_JsonRoundTrip_Str";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal)
        {
            { "greeting", "hello" }
        };

        Assert.IsTrue(cred.SaveCredential(), "SaveCredential failed");

        var cred1 = CredentialManager.GetICredential(cred.TargetName);
        Assert.IsNotNull(cred1);
        Assert.AreEqual(1, cred1.Attributes?.Count);

        var stringVal = ((JsonElement)cred1.Attributes!["greeting"]).GetString();
        Assert.AreEqual("hello", stringVal);

        CredentialManager.RemoveCredentials("TestSystem_JsonRoundTrip_Str");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_JsonAttributeRoundTrip_Number()
    {
        var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential()!;
        cred.TargetName = "TestSystem_JsonRoundTrip_Num";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal)
        {
            { "count", 42 }
        };

        Assert.IsTrue(cred.SaveCredential(), "SaveCredential failed");

        var cred1 = CredentialManager.GetICredential(cred.TargetName);
        Assert.IsNotNull(cred1);

        var intVal = ((JsonElement)cred1.Attributes!["count"]).GetInt32();
        Assert.AreEqual(42, intVal);

        CredentialManager.RemoveCredentials("TestSystem_JsonRoundTrip_Num");
    }

    [TestMethod, TestCategory("CI")]
    public void TestICredential_JsonAttributeRoundTrip_Struct()
    {
        var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential()!;
        cred.TargetName = "TestSystem_JsonRoundTrip_Struct";
        cred.Attributes = new Dictionary<string, Object>(StringComparer.Ordinal);

        var sample = new SampleAttribute() { role = "admin", created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        cred.Attributes.Add("userInfo", sample);

        Assert.IsTrue(cred.SaveCredential(), "SaveCredential failed");

        var cred1 = CredentialManager.GetICredential(cred.TargetName);
        Assert.IsNotNull(cred1);

        var jsonOptions = s_jsonOptions;
        var retrieved = ((JsonElement)cred1.Attributes!["userInfo"]).Deserialize<SampleAttribute>(jsonOptions);
        Assert.AreEqual("admin", retrieved.role);
        Assert.AreEqual(sample.created, retrieved.created);

        CredentialManager.RemoveCredentials("TestSystem_JsonRoundTrip_Struct");
    }

    // -------------------------------------------------------------------
    // Null parameter handling (v3.1.0)
    // -------------------------------------------------------------------

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_NullTarget_Throws()
    {
        var cred = new NetworkCredential(uName, pwd, domain);
        Assert.ThrowsException<ArgumentNullException>(() =>
            CredentialManager.SaveCredentials(null!, cred));
    }

    [TestMethod, TestCategory("CI")]
    public void TestSaveCredentials_NullCredential_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            CredentialManager.SaveCredentials("TestTarget", null!));
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetCredentials_NullTarget_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            CredentialManager.GetCredentials(null!));
    }

    [TestMethod, TestCategory("CI")]
    public void TestRemoveCredentials_NullTarget_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            CredentialManager.RemoveCredentials(null!));
    }

    [TestMethod, TestCategory("CI")]
    public void TestGetICredential_NullTarget_Throws()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            CredentialManager.GetICredential(null!));
    }
}
