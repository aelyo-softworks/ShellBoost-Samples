using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class MoveOptions
    {
        public bool Copy { get; set; } // default is move only
        public bool Overwrite { get; set; }
        public bool EnsureUniqueName { get; set; }

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (Copy)
            {
                op["copy"] = Copy;
            }

            if (Overwrite)
            {
                op["overwrite"] = Overwrite;
            }

            if (EnsureUniqueName)
            {
                op["ensureUniqueName"] = EnsureUniqueName;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
