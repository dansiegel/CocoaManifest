using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CocoaManifest.Build.Generators;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace CocoaManifest.Build.Tasks
{
    public class GenerateAttributedPlistsTask : Task
    {
        [Required]
        public string AssemblyPath { get; set; }

        public ITaskItem InfoPlistItem { get; set; }

        public ITaskItem EntitlementsItem { get; set; }

        [Output]
        public ITaskItem[] CompiledAppManifests { get; set; }

        public override bool Execute()
        {
            var assembly = Assembly.LoadFrom(AssemblyPath);
            if(FileExists(InfoPlistItem.ItemSpec)
                && FileExists(EntitlementsItem.ItemSpec)
                && assembly != null
                && assembly.GetExportedTypes().Any(x => x.Name.EndsWith("AppDelegate")))
            {
                Log.LogMessage("Found a valid Assembly");
                var logger = this.CreateTaskLogger();
                var manifestAttributes = assembly.GetCustomAttributes<CocoaManifestAttribute>();

                var appDelegateType = assembly.GetExportedTypes().First(x => x.Name.EndsWith("AppDelegate"));
                var adAttributes = appDelegateType.GetCustomAttributes<CocoaManifestAttribute>();
                if(adAttributes.Any())
                {
                    var list = manifestAttributes.ToList();
                    list.AddRange(adAttributes);
                    manifestAttributes = list;
                }

                foreach(var attr in adAttributes)
                {
                    Log.LogMessage(MessageImportance.High, $"Found: {attr.GetType().Name}");
                }

                var items = new[] 
                {
                    new InfoPlistGenerator(logger, InfoPlistItem, manifestAttributes).Execute(),
                    new EntitlementsGenerator(logger, EntitlementsItem, manifestAttributes).Execute()
                };

                CompiledAppManifests = items.Where(x => x != null).ToArray();
            }
            else if(assembly is null)
            {
                Log.LogWarning($"Something unexpected has occurred, no assembly could be found at the output path: {AssemblyPath}");
            }

            if(CompiledAppManifests is null)
            {
                CompiledAppManifests = Array.Empty<ITaskItem>();
            }
            return !Log.HasLoggedErrors;
        }

        private bool FileExists(string path) =>
            !string.IsNullOrEmpty(path) && File.Exists(path);
    }
}
