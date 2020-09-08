using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Sql
{
    public class SqlFileSystem : IFileSystem
    {
        private ConcurrentDictionary<string, Event> _events = new ConcurrentDictionary<string, Event>();

        public SqlFileSystem()
        {
            Options = new WebFolderOptions();
        }

        public string ConnectionString { get; private set; }
        public WebFolderOptions Options { get; }
        public Guid UniqueId { get; private set; }
        public IFileSystemEvents Events { get; private set; }

        public override string ToString() => ConnectionString;

        public void Initialize(Action<WebFolderOptions> setupAction, IFileSystemEvents events, IDictionary<string, string> properties)
        {
            if (properties == null)
                throw new ArgumentNullException(nameof(properties));

            if (events == null)
                throw new ArgumentNullException(nameof(events));

            var cnx = properties.GetNullifiedValue(nameof(ConnectionString));
            if (string.IsNullOrWhiteSpace(cnx))
                throw new WebFolderException("0001: Configuration is missing parameter '" + nameof(ConnectionString) + "'.");

            Events = events;
            ConnectionString = cnx;
            UniqueId = Conversions.ComputeGuidHash(ConnectionString);
            setupAction?.Invoke(Options);

            Task.Run(async () =>
            {
                // create table
                await SqlExtensions.CreateTableAsync(ConnectionString, "Item", "Id uniqueidentifier NOT NULL, ParentId uniqueidentifier NOT NULL, Name nvarchar(260) NOT NULL, LastAccessTimeUtc datetime2 NOT NULL, CreationTimeUtc datetime2 NOT NULL, LastWriteTimeUtc datetime2 NOT NULL, Attributes int NOT NULL, Data varbinary(max) CONSTRAINT PK_Item PRIMARY KEY (Id)").ConfigureAwait(false);

                // add indices
                await SqlExtensions.CreateIndexAsync(ConnectionString, "IX_Parent", "CREATE NONCLUSTERED INDEX IX_Parent ON Item(ParentId)").ConfigureAwait(false);
                await SqlExtensions.CreateIndexAsync(ConnectionString, "IX_ParentIdName", "CREATE UNIQUE NONCLUSTERED INDEX IX_ParentIdName ON Item(ParentId, Name)").ConfigureAwait(false);

                // ensure root exists
                // we don't use ms
                var now = DateTime.UtcNow.RemoveMilliseconds();
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "IF (NOT EXISTS(SELECT * FROM Item WHERE Id = '00000000-0000-0000-0000-000000000000')) INSERT INTO Item (Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes) VALUES ('00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', '', @now, @now, @now, 16)", new { now }).ConfigureAwait(false);
            }).Wait();
        }

        private class Event
        {
            public Event()
            {
                Tick = Environment.TickCount64;
            }

            public long Tick;
            public Guid Id;
            public Guid ParentId;
            public WatcherChangeTypes Action;

            public override string ToString() => Action + ":" + Id + ":" + ParentId;
        }

        public void SendEvents()
        {
            var events = Interlocked.Exchange(ref _events, new ConcurrentDictionary<string, Event>());
            Task.Run(() =>
            {
                foreach (var evt in events.Values.OrderBy(v => v.Tick))
                {
                    Events.Change(evt.Id, evt.ParentId, evt.Action);
                }
            });
        }

        private void AddEvent(Guid id, Guid parentId, WatcherChangeTypes action)
        {
            var evt = new Event { Id = id, ParentId = parentId, Action = action };
            _events[evt.ToString()] = evt;
        }

        private SqlItem NewItem(SqlDataReader reader)
        {
            var item = new SqlItem();
            item.System = this;
            item.Id = (Guid)reader["Id"];
            item.ParentId = (Guid)reader["ParentId"];
            item.Name = (string)reader["Name"];

            item.LastAccessTimeUtc = DateTime.SpecifyKind((DateTime)reader["LastAccessTimeUtc"], DateTimeKind.Utc);
            item.CreationTimeUtc = DateTime.SpecifyKind((DateTime)reader["CreationTimeUtc"], DateTimeKind.Utc);
            item.LastWriteTimeUtc = DateTime.SpecifyKind((DateTime)reader["LastWriteTimeUtc"], DateTimeKind.Utc);
            item.Attributes = (FileAttributes)reader["Attributes"];

            var len = reader["Length"];
            if (!Convert.IsDBNull(len))
            {
                item.Length = (long)len;
            }
            return item;
        }

        public async Task<SqlItem> GetSqlItemAsync(Guid id)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WHERE Id = @id", new { id }).ConfigureAwait(false))
            {
                if (!reader.Read())
                    return null;

                return NewItem(reader);
            }
        }

        public async Task<IFileSystemInfo> GetItemAsync(Guid id) => await GetSqlItemAsync(id).ConfigureAwait(false);

        private async Task<SqlItem> CreateSqlItemAsync(SqlItem parentItem, string name, CreateOptions options = null)
        {
            if (parentItem == null)
                throw new ArgumentNullException(nameof(parentItem));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            options ??= new CreateOptions();

            // if a race condition occurred, it may already exists
            var item = await GetSqlItemAsync(parentItem.Id, name).ConfigureAwait(false);
            if (item == null)
            {
                var id = Guid.NewGuid();
                var parameters = new
                {
                    id,
                    parentId = parentItem.Id,
                    name,
                    attributes = options.Attributes,
                    now = DateTime.UtcNow.RemoveMilliseconds()
                };

                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "INSERT INTO Item (Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes) VALUES (@id, @parentId, @name, @now, @now, @now, @attributes)", parameters).ConfigureAwait(false);
                item = await GetSqlItemAsync(id).ConfigureAwait(false);
            }

            if (!item.IsFolder && options.InputStream != null)
            {
                await WriteAsync(item, options.InputStream).ConfigureAwait(false);

                // refresh
                item = await GetSqlItemAsync(item.Id).ConfigureAwait(false);
            }

            AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Created);
            AddEvent(item.ParentId, (item.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            return item;
        }

        private async Task<SqlItem> GetSqlItemAsync(Guid parentId, string name)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WHERE ParentId = @id AND Name=@name", new { id = parentId, name }).ConfigureAwait(false))
            {
                if (reader.Read())
                    return NewItem(reader);

                return null;
            }
        }

        private async Task<bool> ExistsNameAsync(Guid parentId, string name)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT 1 FROM Item WHERE ParentId = @id AND Name=@name", new { id = parentId, name }).ConfigureAwait(false))
            {
                return await reader.ReadAsync();
            }
        }

        private async Task<string> GetNewChildNameAsync(SqlItem parentItem, string tentativeName)
        {
            // come up with some unique name but that still looks like the original name
            return await Conversions.GetNewFileNameAsync(tentativeName, async (s) =>
            {
                return await ExistsNameAsync(parentItem.Id, s).ConfigureAwait(false);
            });
        }

        public async Task<IFileSystemInfo> CreateAsync(SqlItem parentItem, string name, CreateOptions options = null)
        {
            var item = await CreateSqlItemAsync(parentItem, name, options).ConfigureAwait(false);
            return item;
        }

        private async Task<SqlItem> CopyToAsync(SqlItem item, Guid newParentId)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var parentItem = await GetSqlItemAsync(newParentId).ConfigureAwait(false);
            if (parentItem == null)
                throw new InvalidOperationException();

            return await CopyToAsync(item, parentItem).ConfigureAwait(false);
        }

        private async Task<SqlItem> CopyToAsync(SqlItem item, SqlItem parentItem)
        {
            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            SqlItem newItem;
            using (var stream = await OpenReadAsync(item).ConfigureAwait(false))
            {
                newItem = await CreateSqlItemAsync(parentItem, item.Name, new CreateOptions { InputStream = stream, Attributes = item.Attributes }).ConfigureAwait(false);
            }

            if (item.IsFolder)
            {
                await foreach (var child in EnumerateAsync(item).ConfigureAwait(false))
                {
                    await CopyToAsync(child, newItem).ConfigureAwait(false);
                }
            }

            AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Created);
            AddEvent(parentItem.Id, parentItem.ParentId, WatcherChangeTypes.Changed);
            return newItem;
        }

        public async Task<IFileSystemInfo> MoveToAsync(SqlItem item, Guid newParentId, MoveOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            options ??= new MoveOptions();
            if (options.Copy)
                return await CopyToAsync(item, newParentId).ConfigureAwait(false);

            var oldParent = item.Parent;
            var sql = "UPDATE Item SET ParentId = @parentId WHERE Id = @id";
            await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { it = item.Id, parentId = newParentId }).ConfigureAwait(false);
            var newItem = await GetItemAsync(item.Id).ConfigureAwait(false);

            if (oldParent != null)
            {
                AddEvent(oldParent.Id, oldParent.ParentId, WatcherChangeTypes.Changed);
            }
            AddEvent(newItem.ParentId, (newItem.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            return newItem;
        }

        public async Task<IFileSystemInfo> UpdateAsync(SqlItem item, UpdateOptions options)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var sql = "UPDATE Item SET ";
            var parameters = new Dictionary<string, object>();
            parameters["id"] = item.Id;

            var sets = new List<string>();
            var renamed = false;
            var changed = false;
            if (options.Name != null && options.Name != item.Name)
            {
                var newName = options.Name;
                if (options.EnsureUniqueName && item.Parent is SqlItem parent)
                {
                    newName = await GetNewChildNameAsync(parent, newName).ConfigureAwait(false);
                }

                parameters["name"] = newName;
                sets.Add("Name = @name");
                renamed = true;
            }

            if (options.CreationTimeUtc.HasValue)
            {
                parameters["creationTimeUtc"] = options.CreationTimeUtc.Value.RemoveMilliseconds();
                sets.Add("CreationTimeUtc = @creationTimeUtc");
                changed = true;
            }

            if (options.LastAccessTimeUtc.HasValue)
            {
                parameters["lastAccessTimeUtc"] = options.LastAccessTimeUtc.Value.RemoveMilliseconds();
                sets.Add("LastAccessTimeUtc = @lastAccessTimeUtc");
                changed = true;
            }

            if (options.LastWriteTimeUtc.HasValue)
            {
                parameters["lastWriteTimeUtc"] = options.LastWriteTimeUtc.Value.RemoveMilliseconds();
                sets.Add("LastWriteTimeUtc = @lastWriteTimeUtc");
                changed = true;
            }

            if (sets.Count == 0) // nothing to do
                return item;

            sql += string.Join(", ", sets) + " WHERE Id = @id";
            await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, parameters).ConfigureAwait(false);
            var newItem = await GetItemAsync(item.Id).ConfigureAwait(false);

            if (renamed)
            {
                AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Renamed);
            }

            if (changed)
            {
                AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Changed);
            }
            AddEvent(newItem.ParentId, (newItem.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            return newItem;
        }

        public async Task<bool> DeleteAsync(SqlItem item, DeleteOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            string sql;
            if (!item.IsFolder)
            {
                sql = "DELETE FROM Item WHERE Id = @id";
            }
            else // deleting a folder is always recursive
            {
                sql = @"
WITH ItemHierarchy (Id)  
AS  
(  
	SELECT Id FROM Item WHERE Id = @id
	UNION ALL
	SELECT i.Id FROM Item i INNER JOIN ItemHierarchy h ON i.ParentId = h.Id AND i.ParentId <> '00000000-0000-0000-0000-000000000000'
)  
DELETE Item FROM ItemHierarchy JOIN Item ON Item.Id = ItemHierarchy.Id";
            }

            var parent = item.Parent; // get parent before delete
            var deleted = await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { id = item.Id }).ConfigureAwait(false) != 0;
            if (deleted)
            {
                AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Deleted);
                if (parent != null)
                {
                    AddEvent(parent.Id, parent.ParentId, WatcherChangeTypes.Changed);
                }
            }
            return deleted;
        }

        public async Task<Stream> OpenReadAsync(SqlItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Data FROM Item WHERE Id = @id", new { id = item.Id }).ConfigureAwait(false))
            {
                if (!reader.Read())
                    return null;

                if (await reader.IsDBNullAsync(0))
                    return null;

                // it's a pitty GetFieldValueAsync<Stream> doesn't work... https://github.com/dotnet/runtime/issues/28596#issuecomment-484614943
                return reader.GetSqlBytes(0).Stream;
            }
        }

        public async Task WriteAsync(SqlItem item, Stream stream)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            if (stream == null)
            {
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "UPDATE Item SET Data = NULL WHERE Id = @id", new { id = item.Id }).ConfigureAwait(false);
            }
            else
            {
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "UPDATE Item SET Data = @data WHERE Id = @id", new { id = item.Id, data = stream }).ConfigureAwait(false);
            }
            AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Changed);
        }

        public async IAsyncEnumerable<SqlItem> EnumerateAsync(SqlItem parentItem, EnumerateOptions options = null)
        {
            options ??= new EnumerateOptions();
            string and;
            if (options.IncludeFiles)
            {
                if (options.IncludeFolders)
                {
                    and = null;
                }
                else
                {
                    and = " AND (Attributes & " + (int)FileAttributes.Directory + ") = 0";
                }
            }
            else
            {
                if (options.IncludeFolders)
                {
                    and = " AND (Attributes & " + (int)FileAttributes.Directory + ") <> 0";
                }
                else
                    yield break;
            }

            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WHERE ParentId = @id and Id <> '00000000-0000-0000-0000-000000000000'" + and, new { id = parentItem.Id }).ConfigureAwait(false))
            {
                while (reader.Read())
                {
                    yield return NewItem(reader);
                }
            }
        }
    }
}
