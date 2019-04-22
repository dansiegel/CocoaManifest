using System;
using System.Globalization;

namespace CocoaManifest.Build.Mono
{
    class PNumber : PValueObject<int>
    {
        public PNumber(int value) : base(value)
        {
        }

        public override PObject Clone()
        {
            return new PNumber(Value);
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return NSNumber.FromInt32 (Value);
		}
#endif

        public override PObjectType Type
        {
            get { return PObjectType.Number; }
        }

        public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
        {
            int result;
            if (int.TryParse(text, NumberStyles.Integer, formatProvider, out result))
            {
                Value = result;
                return true;
            }
            return false;
        }
    }

}
