using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Events
{
    public class FileSystemEvents : IFileSystemEvents
    {
        public FileSystemEvents(IHubContext<FileSystemHub> hub, ILogger<FileSystemEvents> logger)
        {
            Hub = hub;
            Logger = logger;
        }

        public IHubContext<FileSystemHub> Hub { get; }
        public ILogger<FileSystemEvents> Logger { get; }

        public void Change(Guid id, Guid parentId, WatcherChangeTypes types)
        {
            Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ": Change event id: " + id + " parentId: " + parentId + " action: " + types);
            Hub.Clients.All.SendAsync(nameof(Change), id, parentId, types);
        }
    }
}
