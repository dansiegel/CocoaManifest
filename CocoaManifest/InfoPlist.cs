using System;

namespace CocoaManifest
{
    public sealed class InfoPlist
    {
        public const string FileSharing = "UIFileSharingEnabled";
        public const string RequiresPersistentWiFi = "UIRequiresPersistentWiFi";
        public const string StatusBarHidden = "UIStatusBarHidden";
        public const string EdgeAntialiasing = "UIViewEdgeAntialiasing";
        public const string GroupOpacity = "UIViewGroupOpacity";

        public sealed class Capabilities
        {
            public const string Accelerometer = "accelerometer";
            public const string ARKit = "arkit";
            public const string ArmV7 = "armv7";
            public const string Arm64 = "arm64";
            public const string AutoFocusCamera = "auto-focus-camera";
            public const string BluetoothLE = "bluetooth-le";
            public const string CameraFlash = "camera-flash";
            public const string FrontFacingCamera = "front-facing-camera";
            public const string GameKit = "gamekit";
            public const string GPS = "gps";
            public const string Gyroscope = "gyroscope";
            public const string HealthKit = "healthkit";
            public const string LocationServices = "location-services";
            public const string Magnetometer = "magnetometer";
            public const string Metal = "metal";
            public const string Microphone = "microphone";
            public const string NFC = "nfc";
            public const string OpenGles1 = "opengles-1";
            public const string OpenGles2 = "opengles-2";
            public const string OpenGles3 = "opengles-3";
            public const string PeerPeer = "peer-peer";
            public const string SMS = "sms";
            public const string StillCamera = "still-camera";
            public const string Telephony = "telephony";
            public const string VideoCamera = "video-camera";
            public const string WIFI = "wifi";
        }

        public sealed class DeviceFamily
        {
            public const int iPhone = 1;
            public const int iPad = 2;
        }

        public sealed class BackgroundMode
        {
            public const string AudioAirPlay = "audio";
            public const string VoiceOverIP = "voip";
            public const string ExternalAccessory = "external-accessory";
            public const string ActsAsBluetoothAccessory = "bluetooth-peripheral";
            public const string RemoteNotifications = "remote-notification";
            public const string LocationUpdates = "location";
            public const string NewsstandDownloads = "newsstand-content";
            public const string UsesBluetoothLEAccessory = "bluetooth-central";
            public const string BackgroundFetch = "fetch";
        }

        public sealed class PrivacyUsage
        {
            public const string NFCReader = "NFCReaderUsageDescription";
            public const string AppleMusic = "NSAppleMusicUsageDescription";
            public const string BluetoothPeripheral = "NSBluetoothPeripheralUsageDescription";
            public const string Calendars = "NSCalendarsUsageDescription";
            public const string Camera = "NSCameraUsageDescription";
            public const string Contacts = "NSContactsUsageDescription";
            public const string FaceID = "NSFaceIDUsageDescription";
            public const string HealthClinicalHealthRecordsShare = "NSHealthClinicalHealthRecordsShareUsageDescription";
            public const string HealthShare = "NSHealthShareUsageDescription";
            public const string HealthUpdate = "NSHealthUpdateUsageDescription";
            public const string HomeKit = "NSHomeKitUsageDescription";
            public const string LocationAlways = "NSLocationAlwaysUsageDescription";
            public const string Location = "NSLocationUsageDescription";
            public const string LocationWhenInUse = "NSLocationWhenInUseUsageDescription";
            public const string Microphone = "NSMicrophoneUsageDescription";
            public const string Motion = "NSMotionUsageDescription";
            public const string PhotoLibraryAdd = "NSPhotoLibraryAddUsageDescription";
            public const string PhotoLibrary = "NSPhotoLibraryUsageDescription";
            public const string Reminders = "NSRemindersUsageDescription";
            public const string Siri = "NSSiriUsageDescription";
            public const string SpeechRecognition = "NSSpeechRecognitionUsageDescription";
            public const string VideoSubscriberAccount = "NSVideoSubscriberAccountUsageDescription";
        }

        public sealed class StatusBarStyle
        {
            public const string Default = "UIStatusBarStyleDefault";
            public const string BlackTranslucent = "UIStatusBarStyleBlackTranslucent";
            public const string BlackOpaque = "UIStatusBarStyleBlackOpaque";
        }
    }
}
