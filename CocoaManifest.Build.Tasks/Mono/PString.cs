using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CocoaManifest.Build.Mono
{
    class PString : PValueObject<string>
    {
        public PString(string value) : base(value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
        }

        public override PObject Clone()
        {
            return new PString(Value);
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return new NSString (Value);
		}
#endif

        public override PObjectType Type
        {
            get { return PObjectType.String; }
        }

        public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
        {
            Value = text;
            return true;
        }
    }

}
