﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem.Events;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Sql
{
    // the implementation of a file system for an SQL Server database
    public sealed class SqlFileSystem : IFileSystem
    {
        private ConcurrentDictionary<string, EventImpl> _events = new ConcurrentDictionary<string, EventImpl>();

        public SqlFileSystem(IFileSystemEvents events, WebFolderConfigurationFileSystem configuration)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (configuration.Type != GetType())
                throw new ArgumentException(null, nameof(configuration));

            Options = new WebFolderOptions();
            var cnx = configuration.Properties.GetNullifiedValue(nameof(ConnectionString));
            if (string.IsNullOrWhiteSpace(cnx))
                throw new WebFolderException("0004: Configuration is missing parameter '" + nameof(ConnectionString) + "'.");

            RootId = Guid.Empty;
            Events = events;
            ConnectionString = cnx;
            UniqueId = Conversions.ComputeGuidHash(ConnectionString);

            Task.Run(async () =>
            {
                // create tables
                await SqlExtensions.CreateTableAsync(ConnectionString, "Item", "Id uniqueidentifier NOT NULL, ParentId uniqueidentifier NOT NULL, Name nvarchar(260) NOT NULL, LastAccessTimeUtc datetime2 NOT NULL, CreationTimeUtc datetime2 NOT NULL, LastWriteTimeUtc datetime2 NOT NULL, Attributes int NOT NULL, Data varbinary(max) CONSTRAINT PK_Item PRIMARY KEY NONCLUSTERED (Id)").ConfigureAwait(false);
                await SqlExtensions.CreateTableAsync(ConnectionString, "Change", "Id uniqueidentifier NOT NULL, ItemId uniqueidentifier NOT NULL, ParentId uniqueidentifier NOT NULL, Type int NOT NULL, CreationTimeUtc datetime2 NOT NULL, OldName nvarchar(260), OldParentId uniqueidentifier CONSTRAINT PK_Change PRIMARY KEY NONCLUSTERED (Id)").ConfigureAwait(false);

                // add indices
                await SqlExtensions.CreateIndexAsync(ConnectionString, "IX_Parent", "CREATE NONCLUSTERED INDEX IX_Parent ON Item(ParentId)").ConfigureAwait(false);
                await SqlExtensions.CreateIndexAsync(ConnectionString, "IX_ParentIdName", "CREATE UNIQUE NONCLUSTERED INDEX IX_ParentIdName ON Item(ParentId, Name)").ConfigureAwait(false);
                await SqlExtensions.CreateIndexAsync(ConnectionString, "IX_ChangeCreationTimeUtc", "CREATE CLUSTERED INDEX IX_ChangeCreationTimeUtc ON Change(CreationTimeUtc)").ConfigureAwait(false);

                // ensure root exists
                // we don't use ms
                var now = DateTime.UtcNow.RemoveMilliseconds();
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "IF (NOT EXISTS(SELECT * FROM Item WHERE Id = '00000000-0000-0000-0000-000000000000')) INSERT INTO Item (Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes) VALUES ('00000000-0000-0000-0000-000000000000', '00000000-0000-0000-0000-000000000000', '', @now, @now, @now, 16)", new { now }).ConfigureAwait(false);

                // clear old changes
                var max = Options.MaxChangesDays;
                if (max >= 0)
                {
                    await ClearOldChangesAsync(max > 0 ? DateTime.Now.AddDays(-max) : DateTime.MaxValue).ConfigureAwait(false);
                }

                max = Options.MaxTempFilesDays;
                if (max >= 0)
                {
                    await ClearOldTempFilesAsync(max > 0 ? DateTime.Now.AddDays(-max) : DateTime.MaxValue).ConfigureAwait(false);
                }
            }).Wait();
        }

        public Guid RootId { get; }
        public string ConnectionString { get; }
        public WebFolderOptions Options { get; }
        public Guid UniqueId { get; }
        public IFileSystemEvents Events { get; }
        public Microsoft.Extensions.Logging.ILogger Logger { get; set; }

        public override string ToString() => ConnectionString;

        private void Log(string text, [CallerFilePath] string filePath = null, [CallerMemberName] string methodName = null) => Logger?.LogInformation(Thread.CurrentThread.ManagedThreadId + ":" + Path.GetFileNameWithoutExtension(filePath) + ":" + methodName + ": " + text);

        private class EventImpl : IFileSystemEvent
        {
            public EventImpl()
            {
                CreationTimeUtc = DateTime.UtcNow;
                Id = Guid.NewGuid();
            }

            public EventImpl(SqlDataReader reader)
            {
                Id = (Guid)reader["Id"];
                ItemId = (Guid)reader["ItemId"];
                ParentId = (Guid)reader["ParentId"];
                Type = (WatcherChangeTypes)reader["Type"];

                var name = reader["OldName"];
                if (!Convert.IsDBNull(name))
                {
                    OldName = (string)name;
                }

                var oldPid = reader["OldParentId"];
                if (!Convert.IsDBNull(oldPid))
                {
                    OldParentId = (Guid)oldPid;
                }

                CreationTimeUtc = DateTime.SpecifyKind((DateTime)reader["CreationTimeUtc"], DateTimeKind.Utc);
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

        public async Task<int> ClearOldTempFilesAsync(DateTime startTime)
        {
            // these are files that were uploaded but never finished for some reason
            var sql = "DELETE FROM Item WHERE Name LIKE @temp AND CreationTimeUtc < @startTime";
            var count = await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { temp = Core.Synchronization.ContentMover.DefaultTemporaryEntryMarker + "%", startTime = startTime.ToUniversalTime() }, Logger).ConfigureAwait(false);
            Log("Deleted:" + count);
            return count;
        }

        public async Task<int> ClearOldChangesAsync(DateTime startTime)
        {
            // remove old changes
            var sql = "DELETE FROM Change WHERE CreationTimeUtc < @startTime";
            var count = await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { startTime = startTime.ToUniversalTime() }, Logger).ConfigureAwait(false);
            Log("Deleted:" + count);
            return count;
        }

        private static bool IsTempFile(string name) => name?.IndexOf(Core.Synchronization.ContentMover.DefaultTemporaryEntryMarker, StringComparison.OrdinalIgnoreCase) >= 0;

        public async IAsyncEnumerable<IFileSystemEvent> EnumerateChangesAsync(DateTime startTime)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ItemId, ParentId, Type, CreationTimeUtc, OldName, OldParentId FROM Change WHERE CreationTimeUtc > @startTime", new { startTime = startTime.ToUniversalTime() }, Logger).ConfigureAwait(false))
            {
                while (reader.Read())
                {
                    yield return new EventImpl(reader);
                }
            }
        }

        public void SendEvents(bool inline = false)
        {
            var events = Interlocked.Exchange(ref _events, new ConcurrentDictionary<string, EventImpl>());
            if (events.IsEmpty)
                return;

            if (inline)
            {
                sendAsync().Wait();
            }
            else
            {
                Task.Run(async () => await sendAsync());
            }

            async Task sendAsync()
            {
                foreach (var evt in events.Values.OrderBy(v => v.CreationTimeUtc))
                {
                    await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "INSERT INTO Change (Id, ItemId, ParentId, Type, CreationTimeUtc, OldName, OldParentId) VALUES (@id, @itemId, @parentId, @type, @creationTimeUtc, @oldName, @oldParentId)", new { id = evt.Id, itemId = evt.ItemId, parentId = evt.ParentId, type = evt.Type, creationTimeUtc = evt.CreationTimeUtc, oldName = evt.OldName, oldParentId = evt.OldParentId }).ConfigureAwait(false);
                    Events?.Change(evt);
                }
            }
        }

        public void AddEvent(Guid itemId, Guid parentId, WatcherChangeTypes action, string oldName = null, Guid? oldParentId = null)
        {
            var evt = new EventImpl { ItemId = itemId, ParentId = parentId, Type = action, OldName = oldName, OldParentId = oldParentId };
            _events[evt.ToString()] = evt;
        }

        private SqlItem NewItem(SqlDataReader reader)
        {
            var item = new SqlItem(this);
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
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WITH (NOLOCK) WHERE Id = @id", new { id }, Logger).ConfigureAwait(false))
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

            if (!parentItem.IsFolder)
                throw new ArgumentNullException(nameof(parentItem));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Log("Parent: " + parentItem.Id + " '" + parentItem.Name + "' name: '" + name + "'");
            options ??= new CreateOptions();

            var item = await GetSqlItemAsync(parentItem.Id, name).ConfigureAwait(false);
            if (item != null && options.EnsureUniqueName)
            {
                name = await GetNewChildNameAsync(parentItem, name).ConfigureAwait(false);
                item = null;
            }

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

                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "INSERT INTO Item (Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes) VALUES (@id, @parentId, @name, @now, @now, @now, @attributes)", parameters, Logger).ConfigureAwait(false);
                item = await GetSqlItemAsync(id).ConfigureAwait(false);
            }
            else if (!options.Overwrite)
                return null;

            if (!item.IsFolder && options.InputStream != null)
            {
                await WriteAsync(item, options.InputStream).ConfigureAwait(false);

                // refresh
                item = await GetSqlItemAsync(item.Id).ConfigureAwait(false);
            }

            if (!IsTempFile(name))
            {
                AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Created);
                AddEvent(item.ParentId, (item.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            }
            return item;
        }

        private async Task<SqlItem> GetSqlItemAsync(Guid parentId, string name)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WITH (NOLOCK) WHERE ParentId = @pid AND Name=@name", new { pid = parentId, name }, Logger).ConfigureAwait(false))
            {
                if (reader.Read())
                    return NewItem(reader);

                return null;
            }
        }

        private async Task<bool> ExistsNameAsync(Guid parentId, string name)
        {
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT 1 FROM Item WITH (NOLOCK) WHERE ParentId = @pid AND Name=@name", new { pid = parentId, name }, Logger).ConfigureAwait(false))
            {
                return await reader.ReadAsync().ConfigureAwait(false);
            }
        }

        // come up with some unique name but that still looks like the original name
        private async Task<string> GetNewChildNameAsync(SqlItem parentItem, string tentativeName) =>
            await Conversions.GetNewFileNameAsync(tentativeName, async (s) =>
            {
                return await ExistsNameAsync(parentItem.Id, s).ConfigureAwait(false);
            }).ConfigureAwait(false);

        public async Task<IFileSystemInfo> GetByNameAsync(SqlItem parentItem, string name)
        {
            if (parentItem == null)
                throw new ArgumentNullException(nameof(parentItem));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return await GetSqlItemAsync(parentItem.Id, name).ConfigureAwait(false);
        }

        public async Task<IFileSystemInfo> CreateAsync(SqlItem parentItem, string name, CreateOptions options = null) => await CreateSqlItemAsync(parentItem, name, options).ConfigureAwait(false);
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

            Log("Parent: " + parentItem.Id + " '" + parentItem.Name + "' item: " + item.Id + " '" + item.Name + "'");
            SqlItem newItem;

            // note: this could be improved if coded on SQLServer side
            using (var stream = await OpenReadAsync(item, null, null).ConfigureAwait(false))
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

            if (!IsTempFile(item.Name))
            {
                AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Created);
                AddEvent(parentItem.Id, parentItem.ParentId, WatcherChangeTypes.Changed);
            }
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

            if (item.ParentId == newParentId)
                return item;

            Log("New Parent: " + newParentId + " item: " + item.Id + " '" + item.Name + "'");

            var oldParent = item.Parent;
            var sql = "UPDATE Item SET ParentId = @pid WHERE Id = @id";
            await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { id = item.Id, pid = newParentId }, Logger).ConfigureAwait(false);
            var newItem = await GetItemAsync(item.Id).ConfigureAwait(false);

            if (!IsTempFile(item.Name))
            {
                AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Changed, null, oldParent?.Id);
                if (oldParent != null)
                {
                    AddEvent(oldParent.Id, oldParent.ParentId, WatcherChangeTypes.Changed);
                }
                AddEvent(newItem.ParentId, (newItem.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            }
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

            Log("Item: " + item.Id + " '" + item.Name + "' attributes: " + item.Attributes + " options: " + options);

            var sql = "UPDATE Item SET ";
            var parameters = new Dictionary<string, object>();
            parameters["id"] = item.Id;
            var finalId = item.Id;

            var sets = new List<string>();
            var oldName = item.Name;
            var changed = false;
            var renamed = false;
            Guid? oldParentId = null;

            if ((options.Name != null && options.Name != item.Name) || options.ParentId.HasValue)
            {
                renamed = true;
                var newName = options.Name;
                parameters["name"] = newName;
                sets.Add("Name = @name");

                // new parent (move)?
                var pid = item.ParentId;
                var parent = item.Parent as SqlItem;
                if (options.ParentId.HasValue)
                {
                    pid = options.ParentId.Value;
                    parent = await GetSqlItemAsync(pid).ConfigureAwait(false);
                    parameters["pid"] = pid;
                    sets.Add("ParentId = @pid");
                    oldParentId = item.Id;
                }

                if (options.Overwrite)
                {
                    var existingItem = await GetSqlItemAsync(pid, newName).ConfigureAwait(false);
                    if (existingItem != null)
                    {
                        // delete existing
                        await DeleteAsync(existingItem, new DeleteOptions { Recursive = true }).ConfigureAwait(false);

                        // but copy data from it
                        if (!options.CreationTimeUtc.HasValue)
                        {
                            options.CreationTimeUtc = existingItem.CreationTimeUtc;
                        }

                        if (!options.LastAccessTimeUtc.HasValue)
                        {
                            options.LastAccessTimeUtc = existingItem.LastAccessTimeUtc;
                        }

                        if (!options.LastWriteTimeUtc.HasValue)
                        {
                            options.LastWriteTimeUtc = existingItem.LastWriteTimeUtc;
                        }

                        if (!options.Attributes.HasValue)
                        {
                            options.Attributes = existingItem.Attributes;
                        }

                        renamed = false;
                        changed = true;
                        parameters["eid"] = existingItem.Id;
                        finalId = existingItem.Id;
                        sets.Add("Id = @eid");

                        parameters["pid"] = pid;
                        sets.Add("ParentId = @pid");
                    }
                }
                else if (options.EnsureUniqueName && parent != null)
                {
                    // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                    newName = await GetNewChildNameAsync(parent, newName).ConfigureAwait(false);
                    parameters["name"] = newName;
                }
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

            if (options.Attributes.HasValue)
            {
                parameters["attributes"] = (int)options.Attributes.Value;
                sets.Add("Attributes = @attributes");
                changed = true;
            }

            if (sets.Count == 0) // nothing to do
                return item;

            sql += string.Join(", ", sets) + " WHERE Id = @id";
            await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, parameters, Logger).ConfigureAwait(false);

            // reload what we changed
            var newItem = await GetItemAsync(finalId).ConfigureAwait(false);

            if (!IsTempFile(newItem.Name))
            {
                if (renamed)
                {
                    AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Renamed, oldName, oldParentId);
                }

                if (changed)
                {
                    AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Changed, null, oldParentId);
                }

                AddEvent(newItem.ParentId, (newItem.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            }
            return newItem;
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<bool> DeleteAsync(SqlItem item, DeleteOptions options = null)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            Log("Item: " + item.Id + " '" + item.Name);

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
            var deleted = await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, sql, new { id = item.Id }, Logger).ConfigureAwait(false) != 0;
            if (deleted)
            {
                if (!IsTempFile(item.Name))
                {
                    AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Deleted);
                    if (parent != null)
                    {
                        AddEvent(parent.Id, parent.ParentId, WatcherChangeTypes.Changed);
                    }
                }
            }
            return deleted;
        }

        public async Task<Stream> OpenReadAsync(SqlItem item, long? offset, long? count)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            SqlDataReader reader;
            if (offset.HasValue || count.HasValue)
            {
                if (!count.HasValue)
                {
                    count = long.MaxValue;
                }

                // offset is 1-based
                if (!offset.HasValue)
                {
                    offset = 1;
                }
                else
                {
                    offset++;
                }

                reader = await SqlExtensions.ExecuteReaderSequentialAccessAsync(ConnectionString, "SELECT SUBSTRING(Data, @offset, @count) FROM Item WHERE Id = @id", new { id = item.Id, offset = offset.Value, count = count.Value }, Logger).ConfigureAwait(false);
            }
            else
            {
                reader = await SqlExtensions.ExecuteReaderSequentialAccessAsync(ConnectionString, "SELECT Data FROM Item WHERE Id = @id", new { id = item.Id }, Logger).ConfigureAwait(false);
            }
            if (!reader.Read())
                return null;

            if (await reader.IsDBNullAsync(0).ConfigureAwait(false))
                return null;

            // it's a pitty GetFieldValueAsync<Stream> doesn't work... https://github.com/dotnet/runtime/issues/28596#issuecomment-484614943
            // also reader.GetSqlBytes(0).Stream in fact reads the whole data (big LOH alloc) and builds a stream on it, which is fairly dumb
            return new SqlBytesStream(reader, 0);
        }

        private class SqlBytesStream : Stream
        {
            private readonly SqlDataReader _reader;
            private readonly int _index;
            private long _position;

            public SqlBytesStream(SqlDataReader reader, int index)
            {
                _reader = reader;
                _index = index;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => _position; set => throw new NotSupportedException(); }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = _reader.GetBytes(_index, Position, buffer, offset, count);
                _position += read;
                return (int)read;
            }

            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _reader.CloseAsync();
            }
        }

        public async Task WriteAsync(SqlItem item, Stream stream)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Id == Guid.Empty)
                throw new UnauthorizedAccessException();

            Log("Item: " + item.Id + " '" + item.Name + " stream: " + stream);

            if (stream == null)
            {
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "UPDATE Item SET Data = NULL WHERE Id = @id", new { id = item.Id }, Logger).ConfigureAwait(false);
            }
            else
            {
                await SqlExtensions.ExecuteNonQueryAsync(ConnectionString, "UPDATE Item SET Data = @data WHERE Id = @id", new { id = item.Id, data = stream }, Logger).ConfigureAwait(false);
            }

            if (!IsTempFile(item.Name))
            {
                AddEvent(item.Id, item.ParentId, WatcherChangeTypes.Changed);
            }
        }

        public async Task<Stream> OpenThumbnailReadAsync(SqlItem item, int width, long? offset)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (item.IsFolder)
                throw new InvalidOperationException();

            var ext = Path.GetExtension(item.Name);
            if (!IsSupportedThumbnailFile(ext))
                throw new InvalidOperationException("Thumbnail are unsupported for extension '" + ext + "'.");

            // build some unique key & cache path
            var key = Conversions.ComputeGuidHash(item.Id + "\0" + item.Length + "\0" + item.LastWriteTimeUtc.Ticks + "\0" + item.Name);
            var file = Path.Combine(Path.GetTempPath(), "CloudFolderImages", key.ToString("N") + "." + width + ext);
            if (!IOUtilities.FileExists(file))
            {
                // thumbnail doesn't exists yet
                // ensure directory exists
                IOUtilities.FileCreateDirectory(file);
                using (var stream = await OpenReadAsync(item, null, null).ConfigureAwait(false))
                {
                    if (stream == null)
                        return null;

                    using (var input = Image.FromStream(stream))
                    {
                        using (var bmp = ImageUtilities.ResizeImageByWidth(input, width))
                        {
                            IOUtilities.WrapSharingViolations(() => bmp.Save(file));
                        }
                    }
                }
            }

            var output = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (offset.HasValue)
            {
                output.Seek(offset.Value, SeekOrigin.Begin);
                // note we don't support count
            }
            return output;
        }

        private static bool IsSupportedThumbnailFile(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return false;

            ext = ext.ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".png" || ext == ".tif" || ext == ".tiff";
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

            if (!options.IncludeHidden)
            {
                and += " AND (Attributes & " + (int)FileAttributes.Hidden + ") = 0";
            }

            var orderBy = new List<string>();
            if (options.FoldersFirst)
            {
                orderBy.Add("(Attributes & " + (int)FileAttributes.Directory + ") DESC");
            }

            if (options.SortByName)
            {
                orderBy.Add("Name");
            }

            if (orderBy.Count > 0)
            {
                and += " ORDER BY " + string.Join(", ", orderBy);
            }

            var includeTemps = options.IncludeTemps;
            using (var reader = await SqlExtensions.ExecuteReaderAsync(ConnectionString, "SELECT Id, ParentId, Name, LastAccessTimeUtc, CreationTimeUtc, LastWriteTimeUtc, Attributes, DATALENGTH(Data) AS Length FROM Item WHERE ParentId = @pid AND Id <> '00000000-0000-0000-0000-000000000000'" + and, new { pid = parentItem.Id }, Logger).ConfigureAwait(false))
            {
                while (reader.Read())
                {
                    var item = NewItem(reader);
                    if (includeTemps || !IsTempFile(item.Name))
                        yield return NewItem(reader);
                }
            }
        }
    }
}
