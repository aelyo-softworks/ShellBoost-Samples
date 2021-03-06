﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem
{
    // defines an abstract file system
    public interface IFileSystem
    {
        // a unique id that can be used as a cache root key by clients
        Guid UniqueId { get; }

        Guid RootId { get; }

        // logger for diags
        ILogger Logger { get; set; }

        // get an item by it's id
        Task<IFileSystemInfo> GetItemAsync(Guid id);

        // list all changes from a date
        IAsyncEnumerable<IFileSystemEvent> EnumerateChangesAsync(DateTime startTime);
    }
}
