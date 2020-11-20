using System;
using System.IO;
using System.Text.Json.Serialization;

namespace ShellBoost.Samples.CloudFolder.Api
{
    // combines folder & file
    public class WebItem
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public FileAttributes Attributes { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public string Name { get; set; }
        public long Length { get; set; } // file only
        public bool IsRoot { get; set; }

        [JsonIgnore]
        public bool IsFolder => Attributes.HasFlag(FileAttributes.Directory);

        [JsonIgnore]
        public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);

        [JsonIgnore]
        public string Extension => Name != null ? Path.GetExtension(Name) : null;

        public override string ToString() => Id + " - " + Name;
    }
}
