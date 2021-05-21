using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class DeleteOptions
    {
        public bool Recursive { get; set; }

        public override string ToString()
        {
            var list = new List<string>();
            list.Add("Recursive:" + Recursive);
            return string.Join(", ", list);
        }
    }
}
