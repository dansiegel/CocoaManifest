using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    internal class ManifestValueAttribute : Attribute
    {
        public ManifestValueAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}
