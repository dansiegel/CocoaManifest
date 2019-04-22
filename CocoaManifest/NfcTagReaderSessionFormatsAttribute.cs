using System;

namespace CocoaManifest
{
    [ManifestKey("com.apple.developer.nfc.readersession.formats")]
    [ManifestValueProperty(nameof(Formats))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class NfcTagReaderSessionFormatsAttribute : CocoaEntitlementsAttribute
    {
        public NfcTagReaderSessionFormatsAttribute() 
            : this(EntitlementsPlist.NfcReaderFormat.NfcDataExchange)
        {

        }

        public NfcTagReaderSessionFormatsAttribute(params string[] formats)
        {
            Formats = formats;
        }

        public string[] Formats { get; }
    }
}
