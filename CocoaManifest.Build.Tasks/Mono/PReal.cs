using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CocoaManifest.Build.Mono
{
    class PReal : PValueObject<double>
    {
        public PReal(double value) : base(value)
        {
        }

        public override PObject Clone()
        {
            return new PReal(Value);
        }

#if POBJECT_MONOMAC
		public override NSObject Convert ()
		{
			return NSNumber.FromDouble (Value);
		}
#endif

        public override PObjectType Type
        {
            get { return PObjectType.Real; }
        }

        public override bool TrySetValueFromString(string text, IFormatProvider formatProvider)
        {
            double result;
            if (double.TryParse(text, NumberStyles.AllowDecimalPoint, formatProvider, out result))
            {
                Value = result;
                return true;
            }
            return false;
        }
    }

}
