using System.Net;

namespace AdysTech.CredentialManager
{
    public static class CredentialExtensions
    {
        public static ICredential ToICredential(this NetworkCredential cred)
        {
            if (cred == null)
            {
                return null;
            }

            return new Credential(cred);
        }
    }
}
