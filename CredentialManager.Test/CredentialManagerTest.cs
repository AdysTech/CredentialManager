using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdysTech.CredentialManager;
using System.Net;
using System.Diagnostics;

namespace CredentialManagerTest
{
    [TestClass]
    public class CredentialManagerTest
    {
        private const string uName = "ZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrh";
        private const string pwd = "5NJuqKfJBtAZYYM3ufm3kFY9ZJZUAqYFQfzxcRc9rzdYxUwqEhBqqdrHttrhcvnnDPFHEn3L";
        private const string domain = "AdysTech.com";


        [TestMethod, TestCategory("AppVeyor")]
        public void TestSaveCredentials()
        {
            try
            {
                var cred = new NetworkCredential(uName, pwd, domain);
                Assert.IsTrue(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
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
                Assert.IsTrue(uName == cred.UserName && pwd == cred.Password && domain == cred.Domain, "Saved and retreived data doesn't match");
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
                    Assert.IsTrue(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
                    cred = CredentialManager.GetCredentials("TestSystem");
                    Assert.IsNotNull(cred, "GetCredential failed");
                    Assert.IsTrue(usr == cred.UserName && pwd == cred.Password && dmn == cred.Domain, "Saved and retreived data doesn't match");
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
                    Assert.IsTrue(CredentialManager.SaveCredentials("TestSystem", cred), "SaveCredential failed");
                    cred = CredentialManager.GetCredentials("TestSystem");
                    Assert.IsNotNull(cred, "GetCredential failed");
                    Assert.IsTrue(usr == cred.UserName && pwd == cred.Password && dmn == cred.Domain, "Saved and retreived data doesn't match");
                }

            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1} on {2}",
                    e.GetType(), e.Message, e.StackTrace);
                return;
            }
        }

        public void TestGetCredentials_Windows()
        {

            try
            {
                //https://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx
                //CredentialType.Windows internally gets translated to CRED_TYPE_DOMAIN_PASSWORD
                //as per MSDN, for this type CredentialBlob can only be read by the authentication packages.
                //I am not able to get the password even while running in elevated mode. more to come.
                var cred = CredentialManager.GetCredentials("192.168.23.1", CredentialManager.CredentialType.Windows);
                Assert.IsNotNull(cred, "GetCredential failed");
                Assert.IsTrue(uName == cred.UserName && pwd == cred.Password && domain == cred.Domain, "Saved and retreived data doesn't match");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
        }


        [TestMethod, TestCategory("AppVeyor")]
        public void TestGetCredentials_NullUserName()
        {
            var cred = new NetworkCredential(string.Empty, "P@$$w0rd");
            var res = CredentialManager.SaveCredentials("TestCredWithoutUserName", cred);
            var cred1 = CredentialManager.GetCredentials("TestCredWithoutUserName");
            Assert.IsTrue(cred1.UserName == cred.UserName && cred1.Password== cred.Password && cred1.Domain == cred.Domain, "Saved and retreived data doesn't match");
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

    }
}
