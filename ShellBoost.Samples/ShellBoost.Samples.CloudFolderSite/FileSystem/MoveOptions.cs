using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class MoveOptions
    {
        public bool Copy { get; set; } // default is move only
        public bool Overwrite { get; set; }
        public bool EnsureUniqueName { get; set; }

        public override string ToString()
        {
            var list = new List<string>();
            list.Add("Copy:" + Overwrite);
            list.Add("EnsureUniqueName:" + EnsureUniqueName);
            list.Add("Overwrite:" + Overwrite);
            return string.Join(", ", list);
        }
    }
}
