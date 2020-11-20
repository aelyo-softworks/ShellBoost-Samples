using System;
using System.IO;
using System.Threading.Tasks;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    // defines an abstract file system entry
    public interface IFileSystemInfo
    {
        IFileSystem System { get; }
        IFolderInfo Parent { get; }

        Guid Id { get; }
        Guid ParentId { get; }

        DateTime LastAccessTimeUtc { get; }
        DateTime CreationTimeUtc { get; }
        DateTime LastWriteTimeUtc { get; }
        string Name { get; }
        FileAttributes Attributes { get; }

        // delete the item
        Task<bool> DeleteAsync(DeleteOptions options = null);

        // move or copy the item
        Task<IFileSystemInfo> MoveToAsync(Guid newParentId, MoveOptions options = null);

        // rename or update the item (only metadata, not content)
        Task<IFileSystemInfo> UpdateAsync(UpdateOptions options); // options not optional
    }
}
