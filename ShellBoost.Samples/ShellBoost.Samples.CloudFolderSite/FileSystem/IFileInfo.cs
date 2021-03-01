using System.IO;
using System.Threading.Tasks;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    // defines an abstract file entry
    public interface IFileInfo : IFileSystemInfo
    {
        // item's content size
        long Length { get; }

        // open the content for reading
        Task<Stream> OpenReadAsync(long? offset, long? count);

        // write the stream content
        Task WriteAsync(Stream stream);
    }
}
