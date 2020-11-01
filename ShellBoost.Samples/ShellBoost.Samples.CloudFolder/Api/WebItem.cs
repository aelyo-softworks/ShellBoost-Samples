using System;
using System.IO;
using System.Text.Json.Serialization;
using ShellBoost.Core.Utilities;

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

        private static bool Equals(FileAttributes atts1, FileAttributes atts2)
        {
            if (atts1 == atts2)
                return true;

            if (atts1.HasFlag(FileAttributes.Directory) && !atts2.HasFlag(FileAttributes.Directory))
                return false;

            if (!atts1.HasFlag(FileAttributes.Directory) && atts2.HasFlag(FileAttributes.Directory))
                return false;

            return true;
        }

        public string GetLocalStatus(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var atts = IOUtilities.PathGetAttributes(path);
            if (!atts.HasValue)
                return "In Sync (no local presence)";

            if (IsFolder && !atts.Value.HasFlag(FileAttributes.Directory) ||
                (!IsFolder && atts.Value.HasFlag(FileAttributes.Directory)))
                return "Folder / File discrepancy";

            if (!Equals(atts.Value, Attributes))
                return "Attributes discrepancy";

            if (IsFolder)
                return string.Empty; // we don't say in sync as it would mean "whole folder hierarchy is in sync" but we don't that

            var lw = IOUtilities.PathGetLastWriteTime(path);
            if (!lw.HasValue)
                return "Error. Retry in a few seconds.";

            // note we don't use ms
            var lwv = lw.Value.ToUniversalTime().RemoveMilliseconds();

            if (LastWriteTimeUtc > lwv)
                return "Local is older";

            if (LastWriteTimeUtc < lwv)
                return "Local is more recent";

            return "In Sync";
        }
    }
}
