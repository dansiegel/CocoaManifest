using System;

namespace CocoaManifest
{
    [ManifestKey("UIRequiredDeviceCapabilities")]
    [ManifestValueProperty(nameof(Capability))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class UsesCapabilityAttribute : CocoaManifestAttribute
    {
        public UsesCapabilityAttribute(string capability)
        {
            Capability = capability;
        }

        public override bool AllowMultiple => true;

        public string Capability { get; }
    }
}
