using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    public interface IFileSystem
    {
        // a unique id that can be used as a cache root key by clients
        Guid UniqueId { get; }

        // logger for diags
        ILogger Logger { get; set; }

        // initialize the file system from configurationb
        void Initialize(Action<WebFolderOptions> setupAction, IFileSystemEvents events, IDictionary<string, string> properties);

        // get an item by it's id
        Task<IFileSystemInfo> GetItemAsync(Guid id);

        // list all changes from a date
        IAsyncEnumerable<IFileSystemEvent> EnumerateChangesAsync(DateTime startTime);
    }
}
