using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderSync
{
    // handles events from the server using SignalR (websockets)
    public sealed class CloudFolderEvents : IDisposable
    {
        private readonly HubConnection _connection;
        private readonly CloudFolderFileSystem _fileSystem;
        private bool _started;

        public CloudFolderEvents(CloudFolderFileSystem fileSystem)
        {
            if (fileSystem == null)
                throw new ArgumentNullException(nameof(fileSystem));

            _fileSystem = fileSystem;

            // build SignalR connection
            _connection = new HubConnectionBuilder()
                .WithUrl(WebApi.RootUrl + "events")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                _fileSystem.Logger?.Log(TraceLevel.Warning, "Server events closed. Error: " + error?.Message + " Restart: " + _started);
                // restart?
                if (_started)
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000).ConfigureAwait(false);
                    await _connection.StartAsync().ConfigureAwait(false);
                }
            };

            // must match server's IFileSystemEvents method signature
            // Change(Guid id, Guid itemId, Guid parentId, WatcherChangeTypes types, DateTime creationTimeUtc, string oldName);
            _connection.On<Guid, Guid, Guid, WatcherChangeTypes, DateTime, string>("Change", (id, itemId, parentId, type, dt, oldName) =>
            {
                _fileSystem.Logger?.Log(TraceLevel.Verbose, "id: " + id + " itemid: " + itemId + " parentId: " + parentId + " type: " + type + " oldName:" + oldName);

                StateSyncEntry entry;
                switch (type)
                {
                    case WatcherChangeTypes.Deleted:
                        entry = ToEntry(itemId, parentId);
                        _fileSystem.OnEvent(new SyncFileSystemEventArgs(SyncFileSystemEventType.Deleted, dt, entry));
                        break;

                    case WatcherChangeTypes.Created:
                        entry = ToEntry(itemId, parentId);
                        _fileSystem.OnEvent(new SyncFileSystemEventArgs(SyncFileSystemEventType.Created, dt, entry));
                        break;

                    case WatcherChangeTypes.Changed:
                        entry = ToEntry(itemId, parentId);
                        _fileSystem.OnEvent(new SyncFileSystemEventArgs(SyncFileSystemEventType.Changed, dt, entry));
                        break;

                    case WatcherChangeTypes.Renamed:
                        entry = ToEntry(itemId, parentId);

                        var oldEntry = ToEntry(itemId, parentId);
                        oldEntry.FileName = oldName;
                        _fileSystem.OnEvent(new SyncFileSystemEventArgs(SyncFileSystemEventType.Moved, dt, entry, oldEntry));
                        break;
                }

                SetLastEventTime(dt);
            });
        }

        private static string GetLastEventTimeFilePath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Program.Name + ".txt");
        public static void SetLastEventTime(DateTime dt)
        {
            var path = GetLastEventTimeFilePath();
            if (dt == DateTimeOffset.MaxValue)
            {
                IOUtilities.FileDelete(path, true, false);
                return;
            }

            IOUtilities.FileCreateDirectory(path);
            var s = dt.ToString("O", CultureInfo.InvariantCulture);
            File.WriteAllText(path, s);
        }

        public static DateTime GetLastEventTime()
        {
            var path = GetLastEventTimeFilePath();
            if (IOUtilities.FileExists(path))
            {
                try
                {
                    var text = File.ReadAllText(path).Nullify();
                    if (text != null && DateTime.TryParse(text, out var dt))
                        return dt;
                }
                catch
                {
                    // continue
                }
            }
            return DateTime.MaxValue;
        }

        internal static StateSyncEntry ToEntry(Guid id, Guid parentId)
        {
            var entry = new StateSyncEntry();
            entry.Id = CloudFolderFileSystem.ToId(id);
            entry.ParentId = CloudFolderFileSystem.ToId(parentId);
            return entry;
        }

        public void Start()
        {
            if (_started)
                return;

            _connection.StartAsync().Wait();
            _started = true;
            _fileSystem.Logger?.Log(TraceLevel.Info, "Server events started.");
        }

        public void Stop()
        {
            if (!_started)
                return;

            _connection.StopAsync().Wait();
            _started = false;
            _fileSystem.Logger?.Log(TraceLevel.Info, "Server events stopped.");
        }

        public async void Dispose()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
