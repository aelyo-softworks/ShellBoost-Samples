using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderClient.Events
{
    // handles events from the server using SignalR (websockets)
    public sealed class ServerEvents : IDisposable
    {
        private readonly HubConnection _connection;

        public event EventHandler<ServerEventArgs> Event;

        public ServerEvents()
        {
            // build SignalR connection
            _connection = new HubConnectionBuilder()
                .WithUrl(WebApi.RootUrl + "events")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                // restart
                await Task.Delay(new Random().Next(0, 5) * 1000).ConfigureAwait(false);
                await _connection.StartAsync().ConfigureAwait(false);
            };

            // must match server's IFileSystemEvents method signature
            // Change(Guid id, Guid itemId, Guid parentId, WatcherChangeTypes types, DateTime creationTimeUtc, string oldName);
            _connection.On<Guid, Guid, Guid, WatcherChangeTypes, DateTime, string, Guid?>("Change", (id, itemId, parentId, type, creationTimeUtc, oldName, oldParentId) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        Event?.Invoke(this, new ServerEventArgs(id, itemId, parentId, type, creationTimeUtc, oldName, oldParentId));
                    }
                    catch
                    {
                        // do nothing
                    }
                });
            });

            _connection.StartAsync();
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
