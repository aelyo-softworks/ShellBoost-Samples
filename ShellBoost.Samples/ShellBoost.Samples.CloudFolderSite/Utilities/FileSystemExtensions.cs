using System.IO;
using ShellBoost.Samples.CloudFolderSite.FileSystem;

namespace ShellBoost.Samples.CloudFolderSite.Utilities
{
    internal static class FileSystemExtensions
    {
        public static bool IsFolder(this IFileSystemInfo info) => (info?.Attributes).GetValueOrDefault().HasFlag(FileAttributes.Directory);
    }
}
