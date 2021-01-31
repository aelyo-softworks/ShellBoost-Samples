using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class EnumerateOptions
    {
        public EnumerateOptions()
        {
            IncludeFolders = true;
            IncludeFiles = true;
        }

        public bool IncludeHidden { get; set; }
        public bool IncludeFolders { get; set; }
        public bool IncludeFiles { get; set; }
        public bool FoldersFirst { get; set; }
        public bool SortByName { get; set; }

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (IncludeHidden)
            {
                op[nameof(IncludeHidden)] = true;
            }

            if (!IncludeFiles)
            {
                op[nameof(IncludeFiles)] = false;
            }

            if (!IncludeFolders)
            {
                op[nameof(IncludeFolders)] = false;
            }

            if (FoldersFirst)
            {
                op[nameof(FoldersFirst)] = true;
            }

            if (SortByName)
            {
                op[nameof(SortByName)] = true;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
