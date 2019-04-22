using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.networking.vpn.api")]
    [ManifestValue("allow-vpn")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class PersonalVPNAttribute : CocoaEntitlementsAttribute
    {

    }
}
