using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.associated-domains")]
    [ManifestValueProperty(nameof(Domains))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AssociatedDomainsAttribute : CocoaEntitlementsAttribute
    {
        public AssociatedDomainsAttribute(params string[] domains)
        {
            Domains = domains;
        }

        public string[] Domains { get; }
    }
}
