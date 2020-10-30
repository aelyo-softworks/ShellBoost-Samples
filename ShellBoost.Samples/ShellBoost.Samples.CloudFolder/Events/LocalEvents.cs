using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolder.Events
{
    // handles events from the local directory
    public sealed class LocalEvents : IDisposable
    {
        private readonly LocalFileSystemWatcher _watcher;
        private Timer _synchronizeTimer;

        public LocalEvents(WebShellFolderServer server, int synchronizePeriod = 60000)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
            SynchronizePeriod = synchronizePeriod;

            _watcher = new LocalFileSystemWatcher(WebApi.LocalDirectory.FullName);
            _watcher.Event += OnWatcherEvent;
            _watcher.Start();

            _synchronizeTimer = new Timer(SynchronizeCallback);
            _synchronizeTimer.Change(SynchronizePeriod, Timeout.Infinite);
        }

        public int SynchronizePeriod { get; }
        public WebShellFolderServer Server { get; }

        private void OnWatcherEvent(object sender, LocalFileSystemWatcherEventArgs e)
        {
            // basic checks
            if (!IOUtilities.PathIsChildOrEqual(WebApi.LocalDirectory.FullName, e.Path))
                return;

            // skip some well known files (office, etc.)
            if (!ConsiderForRemote(Path.GetFileName(e.Path)))
                return;

            var isDir = IOUtilities.PathIsDirectory(e.Path);
            Server.Log(TraceLevel.Verbose, "Action:" + e.Action + " Get remote item for local: " + e.Path);
            var item = Server.GetRemoteItem(e.Path);
            if (item != null)
            {
                if (e.Action == WatcherChangeTypes.Deleted)
                {
                    // a delete in a local file doesn't mean a delete in the server
                    ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEITEM, 0, item.IdList);
                }
                else if (e.Action == WatcherChangeTypes.Changed)
                {
                    ShellUtilities.ChangeNotify(isDir ? SHCNE.SHCNE_UPDATEDIR : SHCNE.SHCNE_UPDATEITEM, 0, item.IdList);
                    if (!isDir)
                    {
                        SynchronizeFile(e.Path);
                    }
                }
                else if (e.Action == WatcherChangeTypes.Renamed)
                {
                    ShellUtilities.ChangeNotify(isDir ? SHCNE.SHCNE_RENAMEFOLDER : SHCNE.SHCNE_RENAMEITEM, 0, item.IdList);
                    if (!isDir)
                    {
                        SynchronizeFile(e.Path);
                    }
                }
                // else create do nothing
            }
            else // try parent
            {
                var parentPath = Path.GetDirectoryName(e.Path);
                item = Server.GetRemoteItem(parentPath);
                if (item != null)
                {
                    // new item creation in the local directories? could come from the "New ..." menu in virtual folder's context
                    // we don't use created just changed
                    if (e.Action != WatcherChangeTypes.Deleted && e.Action != WatcherChangeTypes.Created && item is IObjectWithApiItem owa)
                    {
                        var atts = IOUtilities.PathGetAttributes(e.Path);
                        if (atts.HasValue &&
                            !atts.Value.HasFlag(FileAttributes.Directory) &&
                            !atts.Value.HasFlag(FileAttributes.Hidden) &&
                            !atts.Value.HasFlag(FileAttributes.System))
                        {
                            WebApi.CreateAsync(owa.ApiItem.Id, e.Path, null, atts.Value);
                        }
                    }

                    ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEDIR, 0, item.IdList);
                }
            }
        }

        private void SynchronizeCallback(object state)
        {
            // stop timer
            _synchronizeTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                foreach (var file in Win32FindData.EnumerateFileSystemEntries(WebApi.LocalDirectory.FullName, Win32FindDataEnumerateOptions.Recursive))
                {
                    if (_synchronizeTimer == null)
                        break;

                    SynchronizeFile(file);
                }
            }
            catch (Exception e)
            {
                // do nothing
                Server.Log(TraceLevel.Error, "An error ocurred during synchronization: " + e);
            }

            // restart
            _synchronizeTimer?.Change(SynchronizePeriod, Timeout.Infinite);
        }

        private bool ConsiderForRemote(string name)
        {
            if (name == null)
                return false;

            // Powerpoint does this
            if (name.StartsWith("ppt") && name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                return false;

            // Word does this
            if (name.StartsWith("~") && name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                return false;

            if (name.StartsWith("~$"))
                return false;

            // TODO: add more apps heuristics

            return true;
        }

        private bool ShouldUpload(Win32FindData file, WebItem item)
        {
            // note we could use other stuff if the server supports it (server version, etag, etc.)
            if (file.LastWriteTimeUtc > item.LastWriteTimeUtc)
            {
                Server.Log(TraceLevel.Verbose, file.FullName + " LastWriteTimeUtc/content is different file: " + file.LastWriteTimeUtc + " remote: " + item.LastWriteTimeUtc);
                return true;
            }

            return false;
        }

        // synchronize from local to remote
        public void SynchronizeFile(string path) => SynchronizeFile(Win32FindData.FromPath(path));
        private void SynchronizeFile(Win32FindData file)
        {
            if (file == null)
                return;

            // we're not interested by folders or hidden files
            if (file.IsDirectory || file.Attributes.HasFlag(FileAttributes.Hidden))
                return;

            Server.Log(TraceLevel.Info, "File " + file.FullName);

            // creation (or deletion) only happens on remote
            // if a file appears (or is deleted) here, we don't propagate to remote
            // locally, we only deal with read and updates
            if (!(Server.GetRemoteItem(file.FullName) is IObjectWithApiItem remote))
                return;

            // should we update?
            var upload = ShouldUpload(file, remote.ApiItem);
            if (!upload)
                return;

            Task.Run(() => UpdateRemoteAsync(file, remote.ApiItem));
        }

        private async Task<WebItem> UpdateRemoteAsync(Win32FindData file, WebItem item)
        {
            Server.Log(TraceLevel.Info, "UpdateRemote " + file.FullName);
            item.CreationTimeUtc = file.CreationTimeUtc;
            item.LastAccessTimeUtc = file.LastAccessTimeUtc;
            item.LastWriteTimeUtc = file.LastWriteTimeUtc;
            // note if a sharing violation occurs, we'll retry later
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return await item.UploadAsync(stream).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            var sync = Interlocked.Exchange(ref _synchronizeTimer, null);
            if (sync != null)
            {
                sync.Dispose();
            }

            _watcher?.Dispose();
        }
    }
}
