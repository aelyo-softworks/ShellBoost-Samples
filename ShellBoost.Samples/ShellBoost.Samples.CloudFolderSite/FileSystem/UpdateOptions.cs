using System;
using ShellBoost.Samples.CloudFolderSite.Controllers;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public class UpdateOptions
    {
        public bool EnsureUniqueName { get; set; }
        public string Name { get; set; }
        public DateTime? LastAccessTimeUtc { get; set; }
        public DateTime? CreationTimeUtc { get; set; }
        public DateTime? LastWriteTimeUtc { get; set; }

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
            }
            return needed ? options : null;
        }
    }
}
