using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace AdysTech.CredentialManager
{
    internal static class CredentialExtensions
    {
        internal static ICredential ToICredential(this NetworkCredential cred)
        {
            if (cred == null)
            {
                return null;
            }

            return new Credential(cred);
        }
    }
}
