using System;
using System.ComponentModel;

namespace CocoaManifest
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class CocoaManifestAttribute : Attribute
    {
        public virtual bool AllowMultiple => false;
    }
}
