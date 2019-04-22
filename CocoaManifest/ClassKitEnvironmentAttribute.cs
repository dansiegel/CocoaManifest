using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.ClassKit-environment")]
    [ManifestValueProperty(nameof(Environment))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ClassKitEnvironmentAttribute : CocoaEntitlementsAttribute
    {
        public ClassKitEnvironmentAttribute(string environment)
        {
            Environment = environment;
        }

        public string Environment { get; }
    }
}
