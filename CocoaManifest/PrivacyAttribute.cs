using System;

namespace CocoaManifest
{
    [ManifestKeyProperty(nameof(Permission))]
    [ManifestValueProperty(nameof(Description))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class PrivacyAttribute : CocoaManifestAttribute
    {
        public PrivacyAttribute(string permission, string description)
        {
            Permission = permission;
            Description = description;
        }

        public override bool AllowMultiple => true;

        public string Permission { get; } // i.e. Camera
        public string Description { get; }  // We want to take fun photos of your dinner for Instagram
    }
}
