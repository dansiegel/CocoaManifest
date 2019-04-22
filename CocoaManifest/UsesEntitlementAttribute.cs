using System;

namespace CocoaManifest
{
    [ManifestKeyProperty(nameof(Entitlement))]
    [ManifestValueProperty(nameof(Enabled))]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class UsesEntitlementAttribute : CocoaEntitlementsAttribute
    {
        public UsesEntitlementAttribute(string entitlement) 
            : this(entitlement, true)
        {

        }

        public UsesEntitlementAttribute(string entitlement, bool enabled)
        {
            Entitlement = entitlement;
            Enabled = enabled;
        }

        public override bool AllowMultiple => true;

        public string Entitlement { get; }

        public bool Enabled { get; }
    }
}
