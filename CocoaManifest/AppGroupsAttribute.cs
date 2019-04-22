using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.security.application-groups")]
    [ManifestValueProperty(nameof(Groups))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class AppGroupsAttribute : CocoaEntitlementsAttribute
    {
        public AppGroupsAttribute()
            : this("group.$(CFBundleIdentifier)")
        {

        }

        public AppGroupsAttribute(params string[] groups)
        {
            Groups = groups;
        }

        public string[] Groups { get; }
    }
}
