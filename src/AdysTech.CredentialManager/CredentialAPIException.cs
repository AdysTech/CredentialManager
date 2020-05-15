using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace AdysTech.CredentialManager
{
    [Serializable]
    public class CredentialAPIException : ExternalException
    {
        public string APIName { get; internal set; }

        public CredentialAPIException(string message,string api, int errorCode) : base(message, errorCode)
        {
            APIName = api;
        }

        public CredentialAPIException(string message, int errorCode): base(message, errorCode)
        {

        }

        public CredentialAPIException(string message) : base(message)
        {
        }

        public CredentialAPIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CredentialAPIException()
        {
        }

        protected CredentialAPIException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext):base(serializationInfo,streamingContext)
        {
            
        }
    }
}
