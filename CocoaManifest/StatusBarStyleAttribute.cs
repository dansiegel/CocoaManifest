using System;

namespace CocoaManifest
{
    [ManifestKey("UIStatusBarStyle")]
    [ManifestValueProperty(nameof(Style))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class StatusBarStyleAttribute : CocoaManifestAttribute
    {
        public StatusBarStyleAttribute(string style)
        {
            Style = style;
        }

        public string Style { get; }
    }
}
