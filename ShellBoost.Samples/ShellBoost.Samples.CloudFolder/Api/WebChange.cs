using System;
using System.IO;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public class WebChange
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public Guid ParentId { get; set; }
        public WatcherChangeTypes Type { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public string OldName { get; set; }

        public override string ToString() => Type + " " + ItemId;
    }
}
