using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class ManifestKeyAttribute : Attribute
    {
        public ManifestKeyAttribute(string key, bool isDictionary = false)
        {
            Key = key;
            IsDictionary = isDictionary;
        }

        public string Key { get; }

        public bool IsDictionary { get; }
    }
}