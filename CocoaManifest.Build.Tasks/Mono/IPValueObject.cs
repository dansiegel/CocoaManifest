using System;

namespace CocoaManifest.Build.Mono
{
    interface IPValueObject
    {
        object Value { get; set; }
        bool TrySetValueFromString(string text, IFormatProvider formatProvider);
    }

}
