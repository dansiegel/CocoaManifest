using System;
using System.Runtime.Serialization;

namespace CocoaManifest.Build.Tasks
{
    [Serializable]
    internal class ManifestException : Exception
    {
        public ManifestException()
        {
        }

        public ManifestException(string message) : base(message)
        {
        }

        public ManifestException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ManifestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}