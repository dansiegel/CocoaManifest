using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class AppDelegateAttribute : CocoaManifestAttribute
    {
        [ManifestKey("CFBundleName")]
        public string Name { get; set; } // AwesomeApp

        [ManifestKey("CFBundleDisplayName")]
        public string DisplayName { get; set; } // Awesome App

        [ManifestKey("CFBundleIdentifier")]
        public string Identifier { get; set; }  // com.contoso.awesomeapp

        [ManifestKey("MinimumOSVersion")]
        public string MinimumOSVersion { get; set; } // 10.0

        [ManifestKey("CFBundleVersion")]
        public string Version { get; set; } // 1.0.0.1234

        [ManifestKey("CFBundleShortVersionString")]
        public string ShortVersion { get; set; } // 1.0

        [ManifestKey("NSHumanReadableCopyright")]
        public string Copyright { get; set; }

        [ManifestKey("UIDeviceFamily")]
        public int[] DeviceFamily { get; set; }

        [ManifestKey("UIAppFonts")]
        public string[] Fonts { get; set; }

        public bool AutoLoadFonts { get; set; } // if true add any ttf/otf that is a BundleResource
    }
}
