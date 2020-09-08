using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public interface IFolderInfo : IFileSystemInfo
    {
        bool IsRoot { get; }

        // list all children of a folder
        IAsyncEnumerable<IFileSystemInfo> EnumerateAsync(EnumerateOptions options = null);

        // create an item, with a body (optional)
        Task<IFileSystemInfo> CreateAsync(string name, CreateOptions options = null);
    }
}
