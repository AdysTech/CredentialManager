using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AdysTech.CredentialManager
{
    public interface ICredential
    {
        CredentialType Type { get; set; }
        string TargetName { get; set; }
        string Comment { get; set; }
        DateTime LastWritten { get; set; }
        string CredentialBlob { get; set; }
        Persistance Persistance { get; set; }
        IDictionary<string, Object> Attributes { get; set; }
        string UserName { get; set; }

        NetworkCredential ToNetworkCredential();
        bool SaveCredential(bool AllowBlankPassword=false);

        bool RemoveCredential();
    }
}
