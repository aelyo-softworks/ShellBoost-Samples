using System;
using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolderSite
{
    public class WebFolderConfigurationFileSystem
    {
        public WebFolderConfigurationFileSystem()
        {
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsDefault { get; internal set; }
        public int Index { get; internal set; }
        public string TypeName { get; internal set; }
        public string Description { get; internal set; }
        public IDictionary<string, string> Properties { get; }
        public Type Type => Type.GetType(TypeName, true);
        public string DisplayName => Description ?? TypeName;

        public override string ToString() => Index + ":" + Description + ":" + TypeName;
    }
}
