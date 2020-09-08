using System;
using System.IO;

namespace ShellBoost.Samples.CloudFolderSite.Controllers
{
    // must match client's UpdateRequest
    public class UpdateRequest
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public FileAttributes Attributes { get; set; }
        public string Name { get; set; }
        public DateTime? LastAccessTimeUtc { get; set; }
        public DateTime? CreationTimeUtc { get; set; }
        public DateTime? LastWriteTimeUtc { get; set; }
    }
}
