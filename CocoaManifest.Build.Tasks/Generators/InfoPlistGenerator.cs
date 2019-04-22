using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CocoaManifest.Build.Generators
{
    internal class InfoPlistGenerator : ManifestGeneratorBase
    {
        public InfoPlistGenerator(Action<TraceLevel, string> logger, ITaskItem manifestItem, IEnumerable<CocoaManifestAttribute> attributes) : base(logger, manifestItem, attributes)
        {
        }

        protected override IEnumerable<CocoaManifestAttribute> GetAttributesToProcess()
        {
            return Attributes.Where(x => !(x is CocoaEntitlementsAttribute));
        }
    }
}
