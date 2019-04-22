using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.default-data-protection")]
    [ManifestValueProperty(nameof(ProtectionLevel))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class DataProtectionAttribute : CocoaEntitlementsAttribute
    {
        public DataProtectionAttribute(string protectionLevel)
        {
            ProtectionLevel = protectionLevel;
        }

        public string ProtectionLevel { get; }
    }
}
