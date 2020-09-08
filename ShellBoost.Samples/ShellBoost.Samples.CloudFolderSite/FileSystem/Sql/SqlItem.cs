using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Sql
{
    public class SqlItem : IFileInfo, IFolderInfo
    {
        private Lazy<SqlItem> _parent;

        public SqlItem()
        {
            _parent = new Lazy<SqlItem>(() => System.GetSqlItemAsync(ParentId).Result);
        }

        [JsonIgnore]
        public SqlFileSystem System { get; set; }
        public Guid ParentId { get; set; }

        [JsonIgnore]
        public bool IsFolder => Attributes.HasFlag(FileAttributes.Directory);

        // IFileSystemInfo
        [JsonIgnore]
        public IFolderInfo Parent => _parent.Value;

        public Guid Id { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        public string Name { get; set; }
        public FileAttributes Attributes { get; set; }
        IFileSystem IFileSystemInfo.System => System;

        public async Task<bool> DeleteAsync(DeleteOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var res = await System.DeleteAsync(this, options).ConfigureAwait(false);
                scope.Complete();
                System.SendEvents();
                return res;
            }
        }

        public async Task<IFileSystemInfo> MoveToAsync(Guid newParentId, MoveOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var res = await System.MoveToAsync(this, newParentId, options).ConfigureAwait(false);
                scope.Complete();
                System.SendEvents();
                return res;
            }
        }

        public async Task<IFileSystemInfo> UpdateAsync(UpdateOptions options)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var res = await System.UpdateAsync(this, options).ConfigureAwait(false);
                scope.Complete();
                System.SendEvents();
                return res;
            }
        }

        // IFileInfo
        public long Length { get; set; }

        public Task<Stream> OpenReadAsync()
        {
            if (IsFolder)
                throw new InvalidOperationException();

            return System.OpenReadAsync(this);
        }

        public async Task WriteAsync(Stream stream)
        {
            if (IsFolder)
                throw new InvalidOperationException();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await System.WriteAsync(this, stream).ConfigureAwait(false);
                scope.Complete();
                System.SendEvents();
            }
        }

        // IFolderInfo
        public bool IsRoot => Id == Guid.Empty;

        public IAsyncEnumerable<IFileSystemInfo> EnumerateAsync(EnumerateOptions options = null)
        {
            if (!IsFolder)
                throw new InvalidOperationException();

            return System.EnumerateAsync(this, options);
        }

        public async Task<IFileSystemInfo> CreateAsync(string name, CreateOptions options = null)
        {
            if (!IsFolder)
                throw new InvalidOperationException();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var res = await System.CreateAsync(this, name, options).ConfigureAwait(false);
                scope.Complete();
                System.SendEvents();
                return res;
            }
        }
    }
}
