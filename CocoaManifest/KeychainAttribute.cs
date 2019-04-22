using System;

namespace CocoaManifest
{
    [ManifestKey("keychain-access-groups")]
    [ManifestValueProperty(nameof(Groups))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class KeychainAttribute : CocoaEntitlementsAttribute
    {
        public KeychainAttribute() 
            : this(new[] { "$(AppIdentifierPrefix)$(CFBundleIdentifier)" })
        {
        }

        public KeychainAttribute(string[] groups)
        {
            Groups = groups;
        }

        public string[] Groups { get; }
    }
}
