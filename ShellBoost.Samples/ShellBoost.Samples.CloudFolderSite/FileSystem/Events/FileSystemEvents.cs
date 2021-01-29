using System;
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

        public void Change(IFileSystemEvent @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            Logger.LogDebug(Thread.CurrentThread.ManagedThreadId + ": Change id " + @event.Id + " itemId: " + @event.ItemId + " parentId: " + @event.ParentId + " type: " + @event.Type + " oldName: " + @event.OldName + " oldParentId: " + @event.OldParentId);
            Hub.Clients.All.SendAsync(nameof(Change), @event.Id, @event.ItemId, @event.ParentId, @event.Type, @event.CreationTimeUtc, @event.OldName, @event.OldParentId);
        }
    }
}
