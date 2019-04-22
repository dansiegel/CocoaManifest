using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal sealed class ManifestKeyPropertyAttribute : Attribute
    {
        public ManifestKeyPropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
