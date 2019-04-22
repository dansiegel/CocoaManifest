using System;

namespace CocoaManifest
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class UsesJavaAttribute : CocoaManifestAttribute
    {
        public UsesJavaAttribute(string javaRoot, string javaPath)
        {
            Root = javaRoot;
            Path = javaPath;
        }

        [ManifestKey("NSJavaRoot")]
        public string Root { get; }

        [ManifestKey("NSJavaPath")]
        public string Path { get; }
    }
}
