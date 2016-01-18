using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdysTech.CredentialManager;
using System.Net;

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
                Assert.IsNotNull (CredentialManager.GetCredentials ("localhost:8086"), "GetCredential failed");
            }
            catch ( Exception e )
            {
                Assert.Fail ("Unexpected exception of type {0} caught: {1}",
                            e.GetType (), e.Message);
                return;
            }
        }
    }
}
