using System;
using System.Collections.Generic;
using System.IO;
using ShellBoost.Samples.CloudFolderSite.FileSystem;

namespace ShellBoost.Samples.CloudFolderSite.Utilities
{
    public static class FileSystemExtensions
    {
        public static bool IsFolder(this IFileSystemInfo info) => (info?.Attributes).GetValueOrDefault().HasFlag(FileAttributes.Directory);

        public static string GetDisplayName(this IFileSystemInfo info) => info != null ? (info.Id != Guid.Empty ? info.Name : "<Root>") : string.Empty;

        public static IEnumerable<IFileSystemInfo> GetThisAndParents(this IFileSystemInfo info)
        {
            if (info == null)
                yield break;

            yield return info;
            if (info.Id == Guid.Empty)
                yield break;

            var parent = info.Parent;
            if (parent == null)
                yield break;

            foreach (var p in GetThisAndParents(parent))
            {
                yield return p;
            }
        }
    }
}
