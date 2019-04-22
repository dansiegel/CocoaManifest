using System;

namespace CocoaManifest
{
    [ManifestKey("aps-environment")]
    [ManifestValueProperty(nameof(Environment))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class PushNotificationAttribute : CocoaEntitlementsAttribute
    {
        public PushNotificationAttribute(string environment)
        {
            Environment = environment;
        }

        public string Environment { get; }
    }
}
