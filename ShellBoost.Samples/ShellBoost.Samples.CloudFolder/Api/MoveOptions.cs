﻿using System.Collections.Generic;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class MoveOptions
    {
        public bool Copy { get; set; } // default is move only

        public override string ToString()
        {
            var op = new Dictionary<string, object>();
            if (Copy)
            {
                op["copy"] = Copy;
            }

            return DictionarySerializer<object>.Serialize((IDictionary<string, object>)op, separator: '|', assignment: ':');
        }
    }
}
