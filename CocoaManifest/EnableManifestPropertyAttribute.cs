using System;

namespace CocoaManifest
{
    [ManifestKeyProperty(nameof(Key))]
    [ManifestValueProperty(nameof(IsEnabled))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class EnableManifestPropertyAttribute : CocoaManifestAttribute
    {
        public EnableManifestPropertyAttribute(string key)
            : this(key, true)
        {
        }

        public EnableManifestPropertyAttribute(string key, bool isEnabled)
        {
            Key = key;
            IsEnabled = isEnabled;
        }

        public override bool AllowMultiple => true;

        public string Key { get; }

        public bool IsEnabled { get; }
    }
}
