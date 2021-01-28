using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class DeleteOptions
    {
        public bool Recursive { get; set; }

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (Recursive)
            {
                op["recursive"] = Recursive;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
