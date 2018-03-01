using System;
using System.IO;

namespace ShellBoost.Samples.WebFolder.Api
{
    public class Item
    {
        // public API R/W properties
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public FileAttributes Attributes { get; set; }
        public ItemType Type { get; set; }

        // public API R/O properties
        public long Length { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
    }
}
