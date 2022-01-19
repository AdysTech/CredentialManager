using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdysTech.CredentialManager;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;

namespace CredentialManagerTest
{
    [TestClass]
    public class CredentialManagerTest
    {
        private const string uName = "ZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrh";
        private const string pwd = "5NJuqKfJBtAZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrhcvnnDPFHEn3L";
        private const string domain = "AdysTech.com";

        [Serializable]
        struct SampleAttribute
        {
#pragma warning disable CA2235 // Mark all non-serializable fields
            public string role;
            public DateTime created;
#pragma warning restore CA2235 // Mark all non-serializable fields
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestSaveCredentials()
        {
            try
            {
                var cred = new NetworkCredential(uName, pwd, domain);
                Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestGetCredentials()
        {

            try
            {
                var cred = CredentialManager.GetCredentials("TestSystem");
                Assert.IsNotNull(cred, "GetCredential failed");
                Assert.IsTrue(uName == cred.UserName && pwd == cred.Password && domain == cred.Domain, "Saved and retrieved data doesn't match");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }


        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_Comment()
        {
            try
            {
                var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential();
                cred.TargetName = "TestSystem_comment";
                cred.Comment = "This comment is only visible via API, not in Windows UI";
                Assert.IsTrue(cred.SaveCredential(), "SaveCredential on ICredential failed");

                var cred1 = CredentialManager.GetICredential(cred.TargetName);
                Assert.IsNotNull(cred, "GetICredential failed");
                Assert.IsTrue(cred1.UserName == cred.UserName && cred1.CredentialBlob == cred.CredentialBlob && cred1.Comment == cred.Comment, "Saved and retrieved data doesn't match");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_Attributes()
        {

            try
            {
                var cred = (new NetworkCredential(uName, pwd, domain)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                cred.Attributes = new Dictionary<string, Object>();

                var sample = new SampleAttribute() { role = "regular", created = DateTime.UtcNow };
                cred.Attributes.Add("sampleAttribute", sample);

                Assert.IsTrue(cred.SaveCredential(), "SaveCredential on ICredential failed");
                var cred1 = CredentialManager.GetICredential(cred.TargetName);
                Assert.IsNotNull(cred, "GetICredential failed");
                Assert.IsTrue(cred1.UserName == cred.UserName && cred1.CredentialBlob == cred.CredentialBlob && cred1.Attributes?.Count == cred.Attributes?.Count, "Saved and retrieved data doesn't match");
                //Assert.IsTrue(cred.Attributes.All(a=>a.Value == cred1.Attributes[a.Key]), "Saved and retrieved data doesn't match");
                Assert.IsTrue(((SampleAttribute)cred1.Attributes["sampleAttribute"]).role == sample.role, "Saved and retrieved data doesn't match");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestEnumerateCredentials()
        {
            try
            {
                var creds = CredentialManager.EnumerateCredentials();
                Assert.IsNotNull(creds, "EnumerateCredentials failed");
                Assert.IsTrue(creds?.Count > 0, "No credentials stored in the system");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestEnumerateICredentials()
        {
            try
            {
                var creds = CredentialManager.EnumerateICredentials();
                Assert.IsNotNull(creds, "EnumerateICredentials failed");
                Assert.IsTrue(creds?.Count > 0, "No credentials stored in the system");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        /// <summary>
        /// This test assumes you have a Generic Credential for https://github.com stored on your system.
        /// </summary>
        public void TestEnumerateCredentialWithTarget()
        {
            try
            {
                var creds = CredentialManager.EnumerateCredentials(@"git:https://github.com");
                Assert.IsNotNull(creds, "EnumerateCredentials failed");
                Assert.IsTrue(creds?.Count > 0, "No credentials stored in the system");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod]
        public void TestPromptForCredentials()
        {

            try
            {
                bool save = false;
                Assert.IsNotNull(CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service"), "PromptForCredentials failed");

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        /// <summary>
        /// Not working as Console window can't be seen during test
        /// </summary>
        //[TestMethod]
        // public void TestPromptForCredentialsConsole()
        // {

        //     try
        //     {
        //         bool save = false;
        //         Assert.IsNotNull (CredentialManager.PromptForCredentialsConsole ("Some Webservice"), "PromptForCredentialsConsole failed");

        //     }
        //     catch ( Exception e )
        //     {
        //         Assert.Fail ("Unexpected exception of type {0} caught: {1}",
        //                     e.GetType (), e.Message);
        //         return;
        //     }
        // }

        [TestMethod]
        public void IntegrationTest()
        {

            try
            {
                bool save = true;
                var cred = CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service");
                Assert.IsNotNull(cred, "PromptForCredentials failed");
                if (save)
                {
                    var usr = cred.UserName;
                    var pwd = cred.Password;
                    var dmn = cred.Domain;
                    Debug.WriteLine("Usr:{0}, Pwd{1}, Dmn{2}", usr, pwd, dmn);
                    Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
                    cred = CredentialManager.GetCredentials("TestSystem");
                    Assert.IsNotNull(cred, "GetCredential failed");
                    Assert.IsTrue(usr == cred.UserName && pwd == cred.Password && dmn == cred.Domain, "Saved and retrieved data doesn't match");
                }

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1} on {2}",
                            e.GetType(), e.Message, e.StackTrace);
                return;
            }
        }

        [TestMethod]
        public void IntegrationTest_with_prefilled_username()
        {
            try
            {
                bool save = true;
                var cred = CredentialManager.PromptForCredentials("Some Webservice", ref save, "Please provide credentials", "Credentials for service", "mike.flemming@domain.com");
                Assert.IsNotNull(cred, "PromptForCredentials failed");
                if (save)
                {
                    var usr = cred.UserName;
                    var pwd = cred.Password;
                    var dmn = cred.Domain;
                    Debug.WriteLine("Usr:{0}, Pwd{1}, Dmn{2}", usr, pwd, dmn);
                    Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem1", cred), "SaveCredential failed");
                    cred = CredentialManager.GetCredentials("TestSystem1");
                    Assert.IsNotNull(cred, "GetCredential failed");
                    Assert.IsTrue(usr == cred.UserName && pwd == cred.Password && dmn == cred.Domain, "Saved and retrieved data doesn't match");
                }

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1} on {2}",
                    e.GetType(), e.Message, e.StackTrace);
                return;
            }
        }



        [TestMethod, TestCategory("AppVeyor")]
        public void TestSaveCredentials_Windows()
        {
            var cred = new NetworkCredential("admin", "P@$$w0rd");
            var res = CredentialManager.SaveCredentials("TestWindowsCredential", cred, CredentialType.Windows);
            var cred1 = CredentialManager.GetCredentials("TestWindowsCredential", CredentialType.Windows);
            //https://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx
            //CredentialType.Windows internally gets translated to CRED_TYPE_DOMAIN_PASSWORD
            //as per MSDN, for this type CredentialBlob can only be read by the authentication packages.
            //I am not able to get the password even while running in elevated mode. more to come.
            Assert.IsTrue(cred1 != null && cred1.UserName == cred.UserName, "Saved and retrieved data doesn't match");
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestGetCredentials_NullUserName()
        {
            var cred = new NetworkCredential(string.Empty, "P@$$w0rd");
            var res = CredentialManager.SaveCredentials("TestCredWithoutUserName", cred);
            var cred1 = CredentialManager.GetCredentials("TestCredWithoutUserName");
            Assert.IsTrue(cred1.UserName == cred.UserName && cred1.Password == cred.Password && cred1.Domain == cred.Domain, "Saved and retrieved data doesn't match");
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestGetCredentials_NonExistantCredential()
        {
            var cred = CredentialManager.GetCredentials("TotallyNonExistingTarget");
            Assert.IsNull(cred);
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void Test_ParseUserName_supports_long_name()
        {
            var longUserName = "ksdqkdbkbqskdbqskdqsdsqdqsdjsqdjqsdjlqsjd@domain.com";
            string domain;
            string user;
            Assert.IsTrue(CredentialManager.ParseUserName(longUserName, 100, 100, out user, out domain));

            Assert.AreEqual(longUserName, user);
            Assert.AreEqual("", domain);
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void Test_ParseUserName_returns_false_if_buffer_is_too_small()
        {
            var longUserName = "ksdqkdbkbqskdbqskdqsdsqdqsdjsqdjqsdjlqsjd@domain.com";
            string domain;
            string user;
            Assert.IsFalse(CredentialManager.ParseUserName(longUserName, 10, 100, out user, out domain));
            Assert.AreEqual("", user);
            Assert.AreEqual("", domain);
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void Test_ParseUserName_supports_domain_name()
        {
            string user;
            string domain;
            Assert.IsTrue(CredentialManager.ParseUserName("domain.com\\mike", 100, 100, out user, out domain));

            Assert.AreEqual("mike", user);
            Assert.AreEqual("domain.com", domain);
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_LongComment()
        {

            try
            {
                string test = "test";
                var cred = (new NetworkCredential(test, test, test)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                cred.Comment = new String('*', 257);
                Assert.ThrowsException<ArgumentException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentException for larger than 256 byte Comment");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_LongPassword()
        {
            try
            {
                int tooLong = 2 * Credential.MaxCredentialBlobSize;
                string test = "test";
                var cred = (new NetworkCredential(test, new String('*', tooLong), test)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                Assert.ThrowsException<ArgumentException>(() => cred.SaveCredential(), 
                    $"SaveCredential didn't throw ArgumentException for exceeding {Credential.MaxCredentialBlobSize} bytes.");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_LongTokenShouldWork()
        {
            // Tokens can be rather large. 1040: a size that can be stored.
            const int tokenLength = 1040;
            Assert.IsTrue(tokenLength < Credential.MaxCredentialBlobSize, "This test is supposed to verify a valid length.");

            string test = "longPasswordTest";
            var net = new NetworkCredential(test, new String('1', tokenLength), test);
            ICredential cred = net.ToICredential();
            cred.TargetName = "TestSystem_LongPassword";
            Assert.IsNotNull(cred.SaveCredential(), "SaveCredential should handle passwords of token size");

            var cred1 = CredentialManager.GetCredentials("TestSystem_LongPassword");
            Assert.IsTrue(cred1.Password == net.Password, "Saved and retrieved password doesn't match");
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_AttributesNullValue()
        {

            try
            {
                string test = "test";
                var cred = (new NetworkCredential(test, test, test)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                cred.Attributes = new Dictionary<string, Object>();
                cred.Attributes.Add("sampleAttribute", null);

                Assert.ThrowsException<ArgumentNullException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentNullException for null valued Attribute");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }

#if !NET45
        [TestMethod, TestCategory("AppVeyor")]
        public void TestICredential_AttributesLargeValue()
        {

            try
            {
                string test = "test";
                var cred = (new NetworkCredential(test, test, test)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                cred.Attributes = new Dictionary<string, Object>();
                cred.Attributes.Add("sampleAttribute", ValueTuple.Create("RegularUser", DateTime.UtcNow));

                Assert.ThrowsException<ArgumentException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentException for larger than 256 byte Attribute");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }
#endif

        [TestMethod, TestCategory("AppVeyor")]
        public void TestDeleteCredentials_Windows()
        {
            var cred = new NetworkCredential("admin", "P@$$w0rd");
            var saved = CredentialManager.SaveCredentials("TestDeletingWindowsCredential", cred, CredentialType.Windows);
            Assert.IsNotNull(saved, "SaveCredential on ICredential failed");

            var cred1 = CredentialManager.GetICredential(saved.TargetName, CredentialType.Windows);
            Assert.IsNotNull(cred1, "GetICredential failed");
            Assert.IsTrue(cred1.UserName == saved.UserName, "Saved and retrieved data doesn't match");
            Assert.IsTrue(CredentialManager.RemoveCredentials(saved.TargetName, saved.Type), "RemoveCredentials returned false");

            cred1 = CredentialManager.GetICredential(saved.TargetName);
            Assert.IsNull(cred1, "Deleted credential was read");
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestDeleteCredentials_Enumerated()
        {
            var credentials = CredentialManager.EnumerateICredentials();

            if (credentials != null)
            {

                credentials.ForEach(x => { if (x.Type == CredentialType.Windows) Assert.IsTrue(x.RemoveCredential(),"RemoveCredentials returned false"); });
            }
        }

        [TestMethod, TestCategory("AppVeyor")]
        public void TestGetCredentials_PasswordLengthOne()
        {
	        var cred = new NetworkCredential("admin", "P");
	        var res = CredentialManager.SaveCredentials("TestCredWithPasswordSingleCharacter", cred);
	        var cred1 = CredentialManager.GetCredentials("TestCredWithPasswordSingleCharacter");
	        Assert.IsTrue(cred1.Password == cred.Password, "Saved and retrieved password doesn't match");
        }
        [TestMethod, TestCategory("AppVeyor")]
        
        public void TestSaveCredentials_EmptyPassword()
        {
            try
            {
                var cred = new NetworkCredential(uName, "", domain);
                Assert.IsNotNull(CredentialManager.SaveCredentials("TestSystem_nullPwd", cred,AllowNullPassword:true), "SaveCredential failed");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }
    }
}
