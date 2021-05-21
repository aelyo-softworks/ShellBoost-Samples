using System.Collections.Generic;
using System.IO;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class CreateOptions
    {
        public FileAttributes Attributes { get; set; }
        public Stream InputStream { get; set; }
        public bool EnsureUniqueName { get; set; }
        public bool Overwrite { get; set; }

        public override string ToString()
        {
            var list = new List<string>();
            list.Add("Attributes:" + Attributes);
            list.Add("EnsureUniqueName:" + EnsureUniqueName);
            list.Add("Overwrite:" + Overwrite);

            if (InputStream != null)
            {
                list.Add("InputStream:" + InputStream);
            }
            return string.Join(", ", list);
        }
    }
}
