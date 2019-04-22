using System;

namespace CocoaManifest
{
    [ManifestKey("UIBackgroundModes")]
    [ManifestValueProperty(nameof(Mode))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class UsesBackgroundModeAttribute : CocoaManifestAttribute
    {
        public UsesBackgroundModeAttribute(string mode)
        {
            Mode = mode;
        }

        public override bool AllowMultiple => true;

        public string Mode { get; }
    }
}
