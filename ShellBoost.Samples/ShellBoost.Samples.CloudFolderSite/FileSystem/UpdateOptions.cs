using System;
using System.IO;
using ShellBoost.Samples.CloudFolderSite.Controllers;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class UpdateOptions
    {
        public bool EnsureUniqueName { get; set; }
        public bool Overwrite { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public DateTime? LastAccessTimeUtc { get; set; }
        public DateTime? CreationTimeUtc { get; set; }
        public DateTime? LastWriteTimeUtc { get; set; }
        public FileAttributes? Attributes { get; set; }

        public static UpdateOptions FromUpdateRequest(UpdateRequest request)
        {
            var options = new UpdateOptions();
            var needed = false;
            if (request != null)
            {
                if (request.Name != null)
                {
                    options.Name = request.Name;
                    needed = true;
                }

                // note: parent id cannot be changed from upload requests, only from rename (or move) requests

                if (request.CreationTimeUtc.HasValue)
                {
                    options.CreationTimeUtc = request.CreationTimeUtc.Value;
                    needed = true;
                }

                if (request.LastAccessTimeUtc.HasValue)
                {
                    options.LastAccessTimeUtc = request.LastAccessTimeUtc.Value;
                    needed = true;
                }

                if (request.LastWriteTimeUtc.HasValue)
                {
                    options.LastWriteTimeUtc = request.LastWriteTimeUtc.Value;
                    needed = true;
                }

                if (request.Attributes.HasValue)
                {
                    options.Attributes = request.Attributes;
                    needed = true;
                }
            }
            return needed ? options : null;
        }
    }
}
