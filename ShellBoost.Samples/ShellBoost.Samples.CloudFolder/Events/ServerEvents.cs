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
            // Change(Guid id, Guid parentId, WatcherChangeTypes types);
            _connection.On<Guid, Guid, WatcherChangeTypes>("Change", (id, pid, action) =>
            {
                // invalidate the cache using server information
                // note we don't force files update. this will be done when the use opens it
                server.Log(System.Diagnostics.TraceLevel.Warning, "UpdateCache id: " + id + " pid: " + pid + " action: " + action);
                WebApi.UpdateCache(id, pid, action);

                // tell the Shell that this pidl has changed
                // which will eventually call back in views that may be opened to our folders
                var item = server.GetItem(id);
                if (item != null)
                {
                    ShellUtilities.ChangeNotify(SHCNE.SHCNE_UPDATEITEM, 0, item.IdList);
                }

                item = server.GetItem(pid);
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
