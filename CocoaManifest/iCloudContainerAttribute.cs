using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.icloud-container-identifiers")]
    [ManifestValueProperty(nameof(Containers))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class iCloudContainerAttribute : CocoaEntitlementsAttribute
    {
        public iCloudContainerAttribute() : this("$(TeamIdentifierPrefix)$(CFBundleIdentifier)")
        {
        }

        public iCloudContainerAttribute(params string[] containers)
        {
            Containers = containers;
        }

        public string[] Containers { get; }
    }
}
