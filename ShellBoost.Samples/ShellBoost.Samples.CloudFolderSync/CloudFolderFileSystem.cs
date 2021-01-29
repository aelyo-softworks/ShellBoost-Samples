using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderSync
{
    // implements what ShellBoost expects for synchronization with other file systems
    public sealed class CloudFolderFileSystem : ISyncFileSystem, ISyncFileSystemEvents, ISyncFileSystemRead, ISyncFileSystemWriteAsync, IDisposable
    {
        private CloudFolderEvents _serverEvents;
        private readonly EventHandlerList _events = new EventHandlerList();
        private static readonly object _eventEvent = new object();

        public CloudFolderFileSystem(ILogger logger = null)
        {
            Logger = logger;
        }

        public ILogger Logger { get; }

        // raise an event to ShellBoost synchronization
        internal void OnEvent(SyncFileSystemEventArgs e) => ((EventHandler<SyncFileSystemEventArgs>)_events[_eventEvent])?.Invoke(this, e);

        // various conversion routines
        internal static string ToId(Guid id) => id.ToString();
        private static Guid ToId(string id) => string.IsNullOrWhiteSpace(id) ? Guid.Empty : Guid.Parse(id);

        private static SyncEntryAttributes ToAttributes(WebItem item)
        {
            var atts = SyncEntryAttributes.None;
            if (item.IsFolder)
            {
                atts |= SyncEntryAttributes.Directory;
            }

            if (item.IsHidden)
            {
                atts |= SyncEntryAttributes.Hidden;
            }
            return atts;
        }

        private static FileAttributes ToAttributes(StateSyncEntry item)
        {
            var atts = FileAttributes.Normal;
            if (item.IsHidden)
            {
                atts |= FileAttributes.Hidden;
            }

            if (item.IsDirectory)
            {
                atts |= FileAttributes.Directory;
            }
            return atts;
        }

        private static StateSyncEntry ToEntry(WebItem item)
        {
            var entry = new StateSyncEntry();
            CopyToEntry(item, entry);
            return entry;
        }

        private static void CopyToEntry(WebItem item, StateSyncEntry entry)
        {
            entry.Id = ToId(item.Id);
            entry.Name = item.Name;
            entry.Attributes = ToAttributes(item);
            entry.CreationTime = item.CreationTimeUtc.ToLocalTime();
            entry.LastWriteTime = item.LastWriteTimeUtc.ToLocalTime();
            entry.ParentId = ToId(item.ParentId);
            entry.Size = item.Length;
        }

        private static Task<WebItem> CreateEntryAsync(StateSyncEntry entry) => WebApi.CreateAsync(ToId(entry.ParentId), entry.Name, ToAttributes(entry));
        private async Task<WebItem> GetOrCreateFileAsync(StateSyncEntry entry, SyncGetEntryOptions options)
        {
            WebItem item;
            if (entry.Id == null)
            {
                // temporary files are used to move content
                if (!options.IsTemporary)
                    return null;

                // get parent path;
                Guid parentId;
                var parent = options.JobChange.Change.FilePathSegments.Parent;
                if (parent == null)
                {
                    // root
                    parentId = Guid.Empty;
                }
                else
                {
                    // check entry exists.
                    // it should since we init the endpoint synchronizer with UploadsWaitForParents = true
                    var parentEntry = EndPointSynchronizer.StateProvider.GetEntryByFilePath(options.JobChange.DestinationSynchronizer, parent.ToString());
                    parentId = ToId(parentEntry.Id);
                }

                item = await WebApi.GetChildAsync(parentId, entry.Name).ConfigureAwait(false);
                if (item != null)
                    return item;

                Logger?.Log(TraceLevel.Info, "Cannot find temp entry '" + entry.Name + "' with parent id '" + parentId + "'.");
                if (!options.CanCreate)
                    return null;

                // create temp files as hidden files
                return await WebApi.CreateAsync(parentId, entry.Name, FileAttributes.Hidden).ConfigureAwait(false);
            }

            item = await WebApi.GetAsync(ToId(entry.Id)).ConfigureAwait(false);
            if (item != null)
                return item;

            Logger?.Log(TraceLevel.Warning, "Cannot find entry with id '" + entry.Id + "' name '" + entry.Name + "'.");
            if (!options.CanCreate)
                return null;

            return await CreateEntryAsync(entry).ConfigureAwait(false);
        }

        // at init time, we scan the stored changes in case we missed something while we were not running
        private void ProcessChanges()
        {
            var dt = CloudFolderEvents.GetLastEventTime();
            if (dt == DateTime.MaxValue)
                return;

            Logger?.Log(TraceLevel.Info, "Processing events from " + dt);

            DateTime? last = null;
            var count = 0;
            foreach (var change in WebApi.EnumerateChanges(dt))
            {
                StateSyncEntry entry;
                SyncFileSystemEventArgs e = null;
                switch (change.Type)
                {
                    case WatcherChangeTypes.Deleted:
                        entry = CloudFolderEvents.ToEntry(change.ItemId, change.ParentId);
                        e = new SyncFileSystemEventArgs(SyncFileSystemEventType.Deleted, dt, entry);
                        break;

                    case WatcherChangeTypes.Created:
                        entry = CloudFolderEvents.ToEntry(change.ItemId, change.ParentId);
                        e = new SyncFileSystemEventArgs(SyncFileSystemEventType.Created, dt, entry);
                        break;

                    case WatcherChangeTypes.Changed:
                        entry = CloudFolderEvents.ToEntry(change.ItemId, change.ParentId);
                        e = new SyncFileSystemEventArgs(SyncFileSystemEventType.Changed, dt, entry);
                        break;

                    case WatcherChangeTypes.Renamed:
                        entry = CloudFolderEvents.ToEntry(change.ItemId, change.ParentId);

                        var oldEntry = CloudFolderEvents.ToEntry(change.ItemId, change.ParentId);
                        oldEntry.FileName = change.OldName;
                        e = new SyncFileSystemEventArgs(SyncFileSystemEventType.Moved, dt, entry, oldEntry);
                        break;
                }

                if (e != null)
                {
                    OnEvent(e);
                    count++;
                }

                last = change.CreationTimeUtc;
            }

            if (last.HasValue)
            {
                CloudFolderEvents.SetLastEventTime(last.Value);
            }

            Logger?.Log(TraceLevel.Info, "Events processed. " + count + " event(s) created.");
        }

        public void Dispose() => _serverEvents?.Dispose();

        #region ISyncFileSystem
        public string RootId => ToId(Guid.Empty); // for this server, root is id = empty
        public EndPointSynchronizer EndPointSynchronizer { get; set; } // set by MultiPointSynchronizer
        public bool HasCapability(SyncFileSystemCapability capability) => false; // we have no advanced (get/set partial content, full children enum) capability
        #endregion

        #region ISyncFileSystemEvents
        public event EventHandler<SyncFileSystemEventArgs> Event { add { _events.AddHandler(_eventEvent, value); } remove { _events.RemoveHandler(_eventEvent, value); } }

        public void StartEventMonitoring()
        {
            ProcessChanges();
            _serverEvents ??= new CloudFolderEvents(this);
            _serverEvents.Start();
        }

        public void StopEventMonitoring() => _serverEvents?.Stop();
        #endregion

        #region ISyncFileSystemRead
        public IEnumerable<StateSyncEntry> EnumerateEntries(SyncContext context, StateSyncEntry parentEntry, SyncEnumerateEntriesOptions options = null)
        {
            var item = new WebItem { Id = ToId(parentEntry.Id) };
            foreach (var child in WebApi.EnumerateChildren(item))
            {
                // never send back temp (being uploaded) files
                if (EndPointSynchronizer.MultiPointSynchronizer.ContentMover.IsTemporaryFile(child.Name))
                    continue;

                var entry = ToEntry(child);
                Logger?.Log(TraceLevel.Info, "entry '" + entry.Name + "'");
                yield return entry;
            }
        }

        public Task GetEntryContentAsync(SyncContext context, StateSyncEntry entry, Stream output, SyncGetEntryContentOptions options = null)
        {
            var item = new WebItem { Id = ToId(entry.Id) };
            if (item.Id == Guid.Empty)
                return Task.CompletedTask;

            return item.DownloadAsync(output, context);
        }
        #endregion

        #region ISyncFileSystemWriteAsync
        public async Task GetOrCreateEntryAsync(SyncContext context, StateSyncEntry entry, SyncGetEntryOptions options = null)
        {
            var file = await GetOrCreateFileAsync(entry, options).ConfigureAwait(false);
            if (file == null && !options.CanCreate)
                return;

            CopyToEntry(file, entry);
        }

        public async Task UpdateEntryAsync(SyncContext context, StateSyncEntry entry, SyncUpdateEntryOptions options = null)
        {
            WebItem item;
            var pid = ToId(entry.ParentId);
            if (string.IsNullOrEmpty(entry.Id))
            {
                // no id, it's a temporary file, so get it by its name and parent
                item = await WebApi.GetChildAsync(pid, entry.Name).ConfigureAwait(false);
            }
            else
            {
                item = await WebApi.GetAsync(ToId(entry.Id)).ConfigureAwait(false);
            }

            if (item == null)
            {
                if (entry.Id != null)
                {
                    Logger?.Log(TraceLevel.Warning, "Cannot find entry '" + entry.Id + "' name '" + entry.Name + "'.");
                }
                if (!options.CanCreate)
                    return;

                item = await CreateEntryAsync(entry).ConfigureAwait(false);
            }
            else
            {
                // is this the rename/move case?
                if (!entry.Name.EqualsIgnoreCase(item.Name))
                {
                    // avoids looping trying to upload files that already exist
                    var renameOptions = new RenameOptions();
                    if (EndPointSynchronizer.MultiPointSynchronizer.ContentMover.IsTemporaryFile(item.Name))
                    {
                        renameOptions.Overwrite = true;
                    }

                    // move case
                    if (pid != item.ParentId)
                    {
                        renameOptions.NewParentId = pid;
                    }

                    item = await WebApi.RenameAsync(item, entry.Name, renameOptions).ConfigureAwait(false);
                }
            }

            // should we update some metadata?
            if (context.MultiPointSynchronizer.NormalizeDateTime(entry.LastWriteTime) != context.MultiPointSynchronizer.NormalizeDateTime(item.LastWriteTimeUtc.ToLocalTime()) ||
                context.MultiPointSynchronizer.NormalizeDateTime(entry.CreationTime) != context.MultiPointSynchronizer.NormalizeDateTime(item.CreationTimeUtc.ToLocalTime()) ||
                entry.Attributes != ToAttributes(item))
            {
                item.Attributes = ToAttributes(entry);
                item.LastWriteTimeUtc = entry.LastWriteTime.UtcDateTime;
                item.CreationTimeUtc = entry.CreationTime.UtcDateTime;
                item = await WebApi.UploadAsync(item).ConfigureAwait(false);
            }

            CopyToEntry(item, entry);
        }

        public Task DeleteEntryAsync(SyncContext context, StateSyncEntry entry, SyncDeleteEntryOptions options = null)
        {
            var item = new WebItem { Id = ToId(entry.Id) };
            if (item.Id == Guid.Empty)
                return Task.CompletedTask;

            return item.DeleteAsync(new DeleteOptions { Recursive = options?.Recursive == true });
        }

        public async Task SetEntryContentAsync(SyncContext context, StateSyncEntry entry, Stream input, SyncSetEntryContentOptions options = null)
        {
            var item = new WebItem { Id = ToId(entry.Id) };
            if (item.Id == Guid.Empty)
                return;

            item.Attributes = ToAttributes(entry);
            item.CreationTimeUtc = entry.CreationTime.ToUniversalTime().UtcDateTime;
            item.LastWriteTimeUtc = entry.LastWriteTime.ToUniversalTime().UtcDateTime;
            item.Length = entry.Size;
            item.Name = entry.Name;
            item.ParentId = ToId(entry.ParentId);
            var newItem = await item.UploadAsync(input).ConfigureAwait(false);
            if (newItem != null)
            {
                entry.Id = ToId(newItem.Id);
            }
        }
        #endregion
    }
}
