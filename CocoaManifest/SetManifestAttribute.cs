using System;

namespace CocoaManifest
{
    [ManifestKeyProperty(nameof(Key))]
    [ManifestValueProperty(nameof(Value))]
    public sealed class SetManifestAttribute : CocoaManifestAttribute
    {
        public SetManifestAttribute(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public string Value { get; }
    }
}
