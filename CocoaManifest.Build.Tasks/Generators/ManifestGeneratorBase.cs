using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CocoaManifest.Build.Mono;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CocoaManifest.Build.Generators
{
    internal abstract class ManifestGeneratorBase
    {
        private Action<TraceLevel, string> Logger;
        protected IEnumerable<CocoaManifestAttribute> Attributes { get; }
        protected ITaskItem ManifestItem { get; }

        public ManifestGeneratorBase(Action<TraceLevel, string> logger, ITaskItem manifestItem, IEnumerable<CocoaManifestAttribute> attributes)
        {
            Attributes = attributes;
            Logger = logger;
            ManifestItem = manifestItem;
        }

        public ITaskItem Execute()
        {
            var dict = PDictionary.FromFile(ManifestItem.ItemSpec);
            Log(TraceLevel.Info, dict.ToXml());
            File.WriteAllBytes(ManifestItem.ItemSpec, dict.ToByteArray(PropertyListFormat.Binary));
            return ManifestItem;
        }

        protected abstract IEnumerable<CocoaManifestAttribute> GetAttributesToProcess();

        protected void Log(TraceLevel level, string message) => Logger(level, message);
    }
}
