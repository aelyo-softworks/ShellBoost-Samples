using System;
using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class RenameOptions
    {
        public bool Overwrite { get; set; } // default is create a new name
        public Guid? NewParentId { get; set; }

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (Overwrite)
            {
                op["overwrite"] = Overwrite;
            }

            if (NewParentId.HasValue)
            {
                op["pid"] = NewParentId;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
