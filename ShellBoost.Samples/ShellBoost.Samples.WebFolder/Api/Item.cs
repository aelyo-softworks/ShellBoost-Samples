using System;
using System.IO;
using System.Threading.Tasks;

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
        public string ContentETag { get; set; }

        public string DownloadContent() => Task.Run(async () => await WebFolderApi.DownloadContentToFileAsync(Id, ContentETag)).Result;
    }
}
