using System;

namespace CocoaManifest
{
    public sealed class EntitlementsPlist
    {
        public const string AccessWiFiInformation = "com.apple.developer.networking.wifi-info";
        public const string AutoFillCredentialProvider = "com.apple.developer.authentication-services.autofill-credential-provider";
        public const string Siri = "com.apple.developer.siri";
        public const string HotspotConfiguration = "com.apple.developer.networking.HotspotConfiguration";
        public const string MultiPath = "com.apple.developer.networking.multipath";
        public const string InterAppAudio = "inter-app-audio";
        public const string HomeKit = "com.apple.developer.homekit";
        public const string HealthKit = "com.apple.developer.healthkit";
        public const string WirelessConfiguration = "com.apple.external-accessory.wireless-configuration";

        public sealed class ClassKitEnvironment
        {
            public const string Development = "development";
            public const string Production = "production";
        }

        public sealed class DataProtection
        {
            public const string Complete = "NFFileProtectionComplete";
            public const string CompleteUnlessOpen = "NSFileProtectionCompleteUnlessOpen";
            public const string CompleteUntilFirstUserAuthentication = "NSFileProtectionCompleteUntilFirstUserAuthentication";
            public const string None = "NSFileProtectionNone";
        }

        public sealed class PushNotificationEnvironment
        {
            public const string Development = "development";
            public const string Production = "production";
        }

        public sealed class NetworkExtension
        {
            public const string AppProxyProvider = "app-proxy-provider";
            public const string ContentFilterProvider = "content-filter-provider";
            public const string DnsProxy = "dns-proxy";
            public const string PacketTunnelProvider = "packet-tunnel-provider";
        }

        public sealed class NfcReaderFormat
        {
            public const string NfcDataExchange = "NDEF";
        }
    }
}
