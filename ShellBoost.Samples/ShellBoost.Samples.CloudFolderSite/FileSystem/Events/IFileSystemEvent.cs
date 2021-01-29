using System;
using System.IO;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Events
{
    public interface IFileSystemEvent
    {
        // change id
        Guid Id { get; }

        Guid ItemId { get; }
        Guid ParentId { get; }
        WatcherChangeTypes Type { get; }
        DateTime CreationTimeUtc { get; }

        // old name (rename type)
        string OldName { get; }
        Guid? OldParentId { get; }
    }
}
