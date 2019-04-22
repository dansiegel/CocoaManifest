using System;

namespace CocoaManifest.Build.Mono
{
    class PData : PValueObject<byte[]>
    {
        static readonly byte[] Empty = new byte[0];

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			// Work around a bug in NSData.FromArray as it cannot (currently) handle
			// zero length arrays
			if (Value.Length == 0)
				return new NSData ();
			else
				return NSData.FromArray (Value);
		}
#endif

        public PData(byte[] value) : base(value ?? Empty)
        {
        }

        public override PObject Clone()
        {
            return new PData(Value);
        }

        public override PObjectType Type
        {
            get { return PObjectType.Data; }
        }

        public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
        {
            return false;
        }
    }

}
