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
        [TestMethod]
        public void TestSaveCredentials()
        {
            try
            {
                var cred = new NetworkCredential ("TestUser", "Pwd");
                Assert.IsTrue (CredentialManager.SaveCredentials ("TestSystem", cred), "SaveCredential failed");


            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1}",
                            e.GetType (), e.Message);
                return;
            }
        }


        [TestMethod]
        public void TestGetCredentials()
        {

            try
            {
                Assert.IsNotNull (CredentialManager.GetCredentials ("TestSystem"), "GetCredential failed");

            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1}",
                            e.GetType (), e.Message);
                return;
            }
        }

        [TestMethod]
        public void TestPromptForCredentials()
        {

            try
            {
                bool save = false;
                Assert.IsNotNull (CredentialManager.PromptForCredentials ("Some Webservice", ref save, "Please provide credentials", "Credentials for service"), "PromptForCredentials failed");

            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1}",
                            e.GetType (), e.Message);
                return;
            }
        }

        /// <summary>
        /// Not working as Console window can't be seen during test
        /// </summary>
       [TestMethod]
        public void TestPromptForCredentialsConsole()
        {

            try
            {
                bool save = false;
                Assert.IsNotNull (CredentialManager.PromptForCredentialsConsole ("Some Webservice"), "PromptForCredentialsConsole failed");

            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1}",
                            e.GetType (), e.Message);
                return;
            }
        }
        [TestMethod]
        public void IntegrationTest()
        {

            try
            {
                bool save = true;
                var cred = CredentialManager.PromptForCredentials ("Some Webservice", ref save, "Please provide credentials", "Credentials for service");
                Assert.IsNotNull (cred, "PromptForCredentials failed");
                if ( save )
                {
                    var usr = cred.UserName;
                    var pwd = cred.Password;
                    var dmn = cred.Domain;
                    Debug.WriteLine ("Usr:{0}, Pwd{1}, Dmn{2}", usr, pwd, dmn);
                    Assert.IsTrue (CredentialManager.SaveCredentials ("TestSystem", cred), "SaveCredential failed");
                    cred = CredentialManager.GetCredentials ("TestSystem");
                    Assert.IsNotNull (cred, "GetCredential failed");
                    Assert.IsTrue (usr == cred.UserName && pwd == cred.Password && dmn == cred.Domain, "Saved and retreived data doesn't match");
                }

            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1} on {2}",
                            e.GetType (), e.Message, e.StackTrace);
                return;
            }
        }

   
    }
}
