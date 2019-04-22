using System;
using System.Globalization;

namespace CocoaManifest.Build.Mono
{
    class PDate : PValueObject<DateTime>
    {
        public PDate(DateTime value) : base(value)
        {
        }

        public override PObject Clone()
        {
            return new PDate(Value);
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return (NSDate) Value;
		}
#endif

        public override PObjectType Type
        {
            get { return PObjectType.Date; }
        }

        public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
        {
            DateTime result;
            if (DateTime.TryParse(text, formatProvider, DateTimeStyles.None, out result))
            {
                Value = result;
                return true;
            }
            return false;
        }
    }

}
