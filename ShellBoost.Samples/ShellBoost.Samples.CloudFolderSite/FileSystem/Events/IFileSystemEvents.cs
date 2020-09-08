using System;
using System.IO;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Events
{
    public interface IFileSystemEvents
    {
        void Change(Guid id, Guid parentId, WatcherChangeTypes types);
    }
}
