using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Local
{
    public class LocalFileSystem : IFileSystem, IDisposable
    {
        private ConcurrentDictionary<string, EventImpl> _events = new ConcurrentDictionary<string, EventImpl>();
        private readonly LocalFileSystemWatcher _watcher;
        private readonly Timer _eventsTimer;
        private bool _disposedValue;

        public LocalFileSystem(IFileSystemEvents events, WebFolderConfigurationFileSystem configuration)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.Type != GetType())
                throw new ArgumentException(null, nameof(configuration));

            Options = new WebFolderOptions();
            var path = configuration.Properties.GetNullifiedValue(nameof(DirectoryPath));
            if (string.IsNullOrWhiteSpace(path))
                throw new WebFolderException("0002: Configuration is missing parameter '" + nameof(DirectoryPath) + "'.");

            if (!Path.IsPathRooted(path))
                throw new WebFolderException("0003: Configuration parameter '" + nameof(DirectoryPath) + "' must be a rooted path.");

            Events = events;
            DirectoryPath = IOExtensions.NormalizePath(path);
            ItemsPath = Path.Combine(DirectoryPath, "Items");
            ChangesPath = Path.Combine(DirectoryPath, "Changes");

            // prepare directories
            if (!FileSystemEntry.DirectoryExistsByPath(DirectoryPath))
            {
                FileSystemEntry.CreateDirectoryByPath(DirectoryPath);
            }

            if (!FileSystemEntry.DirectoryExistsByPath(ItemsPath))
            {
                FileSystemEntry.CreateDirectoryByPath(ItemsPath);
            }
            ItemsEntry = FileSystemEntry.FromPath(ItemsPath);

            if (!FileSystemEntry.DirectoryExistsByPath(ChangesPath))
            {
                FileSystemEntry.CreateDirectoryByPath(ChangesPath);
            }

            // clear old changes
            var max = Options.MaxChangesDays;
            if (max >= 0)
            {
                ClearOldChanges(max > 0 ? DateTime.Now.AddDays(-max) : DateTime.MaxValue);
            }

            max = Options.MaxTempFilesDays;
            if (max >= 0)
            {
                ClearOldTempFiles(max > 0 ? DateTime.Now.AddDays(-max) : DateTime.MaxValue);
            }

            SendEventsDelay = configuration.Properties.GetValue(nameof(SendEventsDelay), 500);
            _eventsTimer = new Timer(state => SendEvents(), null, Timeout.Infinite, Timeout.Infinite);

            _watcher = new LocalFileSystemWatcher(ItemsPath);
            _watcher.Event += (s, e) => AddLocalEvent(e);
            _watcher.Start();

            // identity server uniquely
            var id = FileSystemEntry.GetIdWithVolumeGuid(DirectoryPath);
            UniqueId = Conversions.ComputeGuidHash(id.Item1 + "\0" + id.Item2);
        }

        public string DirectoryPath { get; private set; }
        public string ItemsPath { get; private set; }
        public int SendEventsDelay { get; private set; }
        public FileSystemEntry ItemsEntry { get; private set; }
        public string ChangesPath { get; private set; }
        public WebFolderOptions Options { get; }
        public Guid UniqueId { get; private set; }
        public IFileSystemEvents Events { get; private set; }
        public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        internal void Log(string text, [CallerMemberName] string methodName = null) => Logger?.LogInformation(Thread.CurrentThread.ManagedThreadId + ": " + methodName + ": " + text);

        public int ClearOldTempFiles(DateTime startTime)
        {
            // these are files that were uploaded but never finished for some reason
            var count = 0;
            var entry = FileSystemEntry.FromPath(ItemsPath, false);
            if (entry != null)
            {
                foreach (var child in entry.EnumerateChildren())
                {
                    var evt = EventImpl.From(this, child);
                    if (evt == null)
                        continue;

                    if (evt.CreationTimeUtc < startTime && IsTempFile(evt.Name))
                    {
                        if (FileSystemEntry.DeleteById(child.Volume.Guid, child.Id))
                        {
                            count++;
                        }
                    }
                }
            }

            Log("Deleted:" + count);
            return count;
        }

        public int ClearOldChanges(DateTime startTime)
        {
            // remove old changes
            var count = 0;
            var entry = FileSystemEntry.FromPath(ChangesPath, false);
            if (entry != null)
            {
                foreach (var child in entry.EnumerateChildren())
                {
                    var evt = EventImpl.From(this, child);
                    if (evt == null)
                        continue;

                    if (evt.CreationTimeUtc < startTime)
                    {
                        if (FileSystemEntry.DeleteById(child.Volume.Guid, child.Id))
                        {
                            count++;
                        }
                    }
                }
            }

            Log("Deleted:" + count);
            return count;
        }

        internal static bool IsTempFile(string name) => name?.IndexOf(Core.Synchronization.ContentMover.DefaultTemporaryEntryMarker, StringComparison.OrdinalIgnoreCase) >= 0;

        private void AddLocalEvent(LocalFileSystemWatcherEventArgs e)
        {
            if (IsTempFile(Path.GetFileName(e.Path)))
                return;

            try
            {
                _eventsTimer?.Change(SendEventsDelay, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // race condition
            }

            var name = e.OldPath != null ? Path.GetFileName(e.OldPath) : null;
            var oldName = name != null ? IOExtensions.UnescapeFileNameToName(name) : null;
            var evt = new EventImpl(this) { ItemId = e.Id, ParentId = e.ParentId, Type = e.Action, OldName = oldName, OldParentId = e.OldParentId };
            _events[evt.ToString()] = evt;
        }

        private void SendEvents(bool inline = false)
        {
            var events = Interlocked.Exchange(ref _events, new ConcurrentDictionary<string, EventImpl>());
            if (events.IsEmpty)
                return;

            if (inline)
            {
                send();
            }
            else
            {
                Task.Run(send);
            }

            void send()
            {
                foreach (var evt in events.Values.OrderBy(v => v.CreationTimeUtc))
                {
                    evt.Save();
                    Events?.Change(evt);
                }
            }
        }

        private class EventImpl : IFileSystemEvent
        {
            private readonly LocalFileSystem _fs;

            public EventImpl(LocalFileSystem fs)
            {
                CreationTimeUtc = DateTime.UtcNow;
                Id = Guid.NewGuid();
                _fs = fs;
            }

            public void Save()
            {
                if (OldName == null)
                {
                    File.WriteAllBytes(FilePath, Array.Empty<byte>());
                }
                else
                {
                    File.WriteAllText(FilePath, OldName, Encoding.UTF8);
                }
                File.SetCreationTimeUtc(FilePath, CreationTimeUtc); // touch
            }

            public static EventImpl From(LocalFileSystem fs, FileSystemEntry file)
            {
                var ext = Path.GetExtension(file.Name).Nullify();
                if (string.IsNullOrEmpty(ext) || ext[0] != '.')
                    return null;

                // min is
                // Id.ItemId.ParentId.ext
                // Id.ItemId.ParentId.OldParentId.o.ext
                var split = file.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length < 4)
                    return null;

                var type = Conversions.ChangeType<WatcherChangeTypes>(split[split.Length - 1]);
                if (type != WatcherChangeTypes.Changed && type != WatcherChangeTypes.Created &&
                    type != WatcherChangeTypes.Deleted && type != WatcherChangeTypes.Renamed)
                    return null;

                var evt = new EventImpl(fs);
                evt.CreationTimeUtc = file.CreationTimeUtc;
                evt.Type = type;
                if (!Guid.TryParse(split[0], out var id))
                    return null;

                evt.Id = id;
                if (!Guid.TryParse(split[1], out var itemId))
                    return null;

                evt.ItemId = itemId;
                if (!Guid.TryParse(split[2], out var pid))
                    return null;

                evt.ParentId = pid;
                if (Guid.TryParse(split[3], out var oldPid))
                {
                    evt.OldParentId = oldPid;
                }

                if (split[split.Length - 2].EqualsIgnoreCase("o"))
                {
                    evt.OldName = File.ReadAllText(file.FullName, Encoding.UTF8).Nullify();
                }

                return evt;
            }

            public string FilePath => Path.Combine(_fs.ChangesPath, Name);
            public string Name
            {
                get
                {
                    var name = Id.ToString("N") + "." + ItemId.ToString("N") + "." + ParentId.ToString("N");
                    if (OldParentId.HasValue)
                    {
                        name += "." + OldParentId.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(OldName))
                    {
                        name += ".o";
                    }

                    name += "." + Type;
                    return name;
                }
            }

            public Guid Id { get; set; }
            public Guid ItemId { get; set; }
            public Guid ParentId { get; set; }
            public WatcherChangeTypes Type { get; set; }
            public string OldName { get; set; }
            public Guid? OldParentId { get; set; }
            public DateTime CreationTimeUtc { get; set; }

            public override string ToString()
            {
                var s = Type + ":" + ItemId + ":" + ParentId;
                if (OldParentId != null)
                {
                    s += ":" + OldParentId;
                }

                if (OldName != null)
                {
                    s += ":" + OldName;
                }
                return s;
            }
        }

        public async IAsyncEnumerable<IFileSystemEvent> EnumerateChangesAsync(DateTime startTime)
        {
            var changes = FileSystemEntry.FromPath(ChangesPath);
            foreach (var file in changes.EnumerateChildren())
            {
                var evt = EventImpl.From(this, file);
                if (evt != null)
                    yield return evt;
            }
            await Task.CompletedTask.ConfigureAwait(false); // to make compiler happy
        }

        private bool IsChildOfItems(FileSystemEntry entry)
        {
            if (ItemsEntry.Volume.Guid != entry.Volume.Guid)
                return false;

            if (entry.Id == ItemsEntry.Id)
                return true;

            return IsChildOfItems(entry.Parent);
        }

        public async Task<IFileSystemInfo> GetItemAsync(Guid id) => await GetLocalItemAsync(id).ConfigureAwait(false);
        public Task<LocalItem> GetLocalItemAsync(Guid id)
        {
            if (id == Guid.Empty)
                return Task.FromResult(new LocalItem(this, ItemsEntry));

            var entry = FileSystemEntry.FromId(ItemsEntry.Volume.Guid, id, false);
            if (entry == null || !IsChildOfItems(entry))
                return Task.FromResult<LocalItem>(null);

            return Task.FromResult(new LocalItem(this, entry));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    _eventsTimer?.Dispose();
                    _watcher?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                _disposedValue = true;
            }
        }

        ~LocalFileSystem() { Dispose(disposing: false); }
        public void Dispose() { Dispose(disposing: true); GC.SuppressFinalize(this); }
    }
}
