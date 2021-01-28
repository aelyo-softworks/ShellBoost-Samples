using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class EnumerateOptions
    {
        public bool IncludeHidden { get; set; }

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (IncludeHidden)
            {
                op["includehidden"] = IncludeHidden;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
