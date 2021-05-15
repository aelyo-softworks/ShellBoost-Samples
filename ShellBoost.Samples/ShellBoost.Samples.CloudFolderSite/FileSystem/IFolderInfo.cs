using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    // defines an abstract folder entry
    public interface IFolderInfo : IFileSystemInfo
    {
        // list all children of a folder
        IAsyncEnumerable<IFileSystemInfo> EnumerateAsync(EnumerateOptions options = null);

        // create an item, with a (optional) body 
        Task<IFileSystemInfo> CreateAsync(string name, CreateOptions options = null);

        // get an item by name
        Task<IFileSystemInfo> GetByNameAsync(string name);
    }
}
