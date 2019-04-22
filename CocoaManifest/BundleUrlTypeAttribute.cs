using System;

namespace CocoaManifest
{
    [ManifestKey("CFBundleURLTypes", true)]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class BundleUrlTypeAttribute : CocoaManifestAttribute
    {
        public BundleUrlTypeAttribute()
        {

        }

        public BundleUrlTypeAttribute(BundleUrlTypeRole role, string name, params string[] schemes)
        {
            Role = role;
            Name = name;
            Schemes = schemes;
        }

        public override bool AllowMultiple => true;

        [ManifestKey("CFBundleTypeRole")]
        public BundleUrlTypeRole Role { get; set; }

        [ManifestKey("CFBundleURLName")]
        public string Name { get; set; }

        [ManifestKey("CFBundleURLSchemes")]
        public string[] Schemes { get; set; }

        [ManifestKey("CFBundleURLIconFile")]
        public string Icon { get; set; }
    }
}
