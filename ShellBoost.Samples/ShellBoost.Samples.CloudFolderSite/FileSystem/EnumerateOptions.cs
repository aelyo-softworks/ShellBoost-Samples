using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class EnumerateOptions
    {
        public EnumerateOptions()
        {
            IncludeFolders = true;
            IncludeFiles = true;
        }

        public bool IncludeFolders { get; set; }
        public bool IncludeFiles { get; set; }
        public bool IncludeHidden { get; set; }
        public bool IncludeTemps { get; set; }
        public bool FoldersFirst { get; set; }
        public bool SortByName { get; set; }

        public override string ToString()
        {
            var list = new List<string>();
            list.Add("IncludeFolders:" + IncludeFolders);
            list.Add("IncludeFiles:" + IncludeFiles);
            list.Add("IncludeHidden:" + IncludeHidden);
            list.Add("IncludeTemps:" + IncludeTemps);
            list.Add("FoldersFirst:" + FoldersFirst);
            list.Add("SortByName:" + SortByName);
            return string.Join(", ", list);
        }
    }
}
