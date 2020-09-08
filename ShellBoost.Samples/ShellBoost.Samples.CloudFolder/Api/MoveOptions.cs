using System.Collections.Generic;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class MoveOptions
    {
        public bool Copy { get; set; } // default is move only

        public override string ToString()
        {
            var list = new List<string>();
            if (Copy)
            {
                list.Add("copy:true");
            }
            return string.Join("|", list);
        }
    }
}
