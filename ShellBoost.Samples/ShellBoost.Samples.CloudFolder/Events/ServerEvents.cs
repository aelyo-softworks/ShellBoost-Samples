using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolder.Events
{
    // handles events from the server using SignalR (websockets)
    public sealed class ServerEvents : IDisposable
    {
        private readonly HubConnection _connection;

        public ServerEvents(WebShellFolderServer server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
            // build SignalR connection
            _connection = new HubConnectionBuilder()
                .WithUrl(WebApi.RootUrl + "events")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                // restart
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            // must match server's IFileSystemEvents method signature
            // Change(Guid id, Guid itemId, Guid parentId, WatcherChangeTypes types, DateTime creationTimeUtc, string oldName);
            _connection.On<Guid, Guid, Guid, WatcherChangeTypes, DateTime, string>("Change", (id, itemId, parentId, type, creationTimeUtc, oldName) =>
            {
                // invalidate the cache using server information
                // note we don't force files update. this will be done when the use opens it
                server.Log(System.Diagnostics.TraceLevel.Warning, "UpdateCache id: " + id + " itemId: " + itemId + " parentId: " + parentId + " type: " + type + " oldName: " + oldName);
                WebApi.UpdateCache(itemId, parentId, type);

                // tell the Shell that this pidl has changed
                // which will eventually call back in views that may be opened to our folders
                var item = server.GetItem(itemId);
                if (item != null)
                {
                    if (item.IsFolder)
                    {
                        switch (type)
                        {
                            case WatcherChangeTypes.Changed:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEDIR, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Created:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_MKDIR, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Deleted:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_RMDIR, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Renamed:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_RENAMEFOLDER, 0, item.IdList);
                                break;
                        }
                    }
                    else
                    {
                        switch (type)
                        {
                            case WatcherChangeTypes.Changed:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEITEM, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Created:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_CREATE, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Deleted:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_DELETE, 0, item.IdList);
                                break;

                            case WatcherChangeTypes.Renamed:
                                ShellUtilities.ChangeNotify(SHCNE.SHCNE_RENAMEITEM, 0, item.IdList);
                                break;
                        }
                    }
                }

                item = server.GetItem(parentId);
                if (item != null)
                {
                    ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEDIR, 0, item.IdList);
                }
            });

            _connection.StartAsync();
        }

        public WebShellFolderServer Server { get; }
        public void Dispose() => _connection?.DisposeAsync();
    }
}
