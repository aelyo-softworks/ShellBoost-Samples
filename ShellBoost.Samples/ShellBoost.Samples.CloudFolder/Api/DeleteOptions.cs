using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class DeleteOptions
    {
        public bool Recursive { get; set; }

        public override string ToString()
        {
            var list = new List<string>();
            if (Recursive)
            {
                list.Add("recursive:true");
            }
            return string.Join("|", list);
        }
    }
}
