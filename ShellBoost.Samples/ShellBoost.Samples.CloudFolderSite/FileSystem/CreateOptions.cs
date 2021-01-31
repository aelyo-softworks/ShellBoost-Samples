using System.IO;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class CreateOptions
    {
        public FileAttributes Attributes { get; set; }
        public Stream InputStream { get; set; }
        public bool EnsureUniqueName { get; set; }
        public bool Overwrite { get; set; }
    }
}
