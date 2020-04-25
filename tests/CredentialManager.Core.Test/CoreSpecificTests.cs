using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using AdysTech.CredentialManager;

namespace CredentialManagerTest
{
    [TestClass]
    public class CoreSpecificTests
    {
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
                string test = "test";
                var cred = (new NetworkCredential(test, new String('*', 512), test)).ToICredential();
                cred.TargetName = "TestSystem_Attributes";
                Assert.ThrowsException<ArgumentException>(() => cred.SaveCredential(), "SaveCredential didn't throw ArgumentException for larger than 512 byte password");
            }
            catch (Exception e)
            {
                Assert.Fail("Unexpected exception of type {0} caught: {1}",
                            e.GetType(), e.Message);
                return;
            }
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
    }


}
