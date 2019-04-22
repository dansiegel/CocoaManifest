using System;

namespace CocoaManifest.Build.Mono
{
    abstract class PValueObject<T> : PObject, IPValueObject
    {
        T val;
        public T Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                OnChanged(EventArgs.Empty);
            }
        }

        object IPValueObject.Value
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        protected PValueObject(T value)
        {
            Value = value;
        }

        protected PValueObject()
        {
        }

        public static implicit operator T(PValueObject<T> pObj)
        {
            return pObj != null ? pObj.Value : default(T);
        }

        public abstract bool TrySetValueFromString(string text, IFormatProvider formatProvider);
    }

}
