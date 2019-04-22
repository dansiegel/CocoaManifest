using System;

namespace CocoaManifest
{
    [ManifestKey("")]
    [ManifestValueProperty(nameof(Hidden))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class UIStatusBarAttribute : CocoaManifestAttribute
    {
        public UIStatusBarAttribute(bool hidden)
        {
            Hidden = hidden;
        }

        public bool Hidden { get; }
    }
}
