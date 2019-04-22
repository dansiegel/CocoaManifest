using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class ManifestValuePropertyAttribute : Attribute
    {
        public ManifestValuePropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }
}
