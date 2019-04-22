using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.networking.networkextension")]
    [ManifestValueProperty(nameof(Extensions))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class UsesNetworkExtensionAttribute : CocoaEntitlementsAttribute
    {
        public UsesNetworkExtensionAttribute(params string[] extensions)
        {
            Extensions = extensions;
        }

        public string[] Extensions { get; }
    }
}
