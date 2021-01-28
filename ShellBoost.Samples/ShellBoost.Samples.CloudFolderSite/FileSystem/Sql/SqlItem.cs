using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Transactions;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Sql
{
    // implementation of files and folders for an SQL Server database
    public class SqlItem : IFileInfo, IFolderInfo
    {
        private readonly Lazy<SqlItem> _parent;

        public SqlItem()
        {
            _parent = new Lazy<SqlItem>(() => System.GetSqlItemAsync(ParentId).Result);
        }

        [JsonIgnore]
        public SqlFileSystem System { get; set; }
        public Guid ParentId { get; set; }

        [JsonIgnore]
        public bool IsFolder => Attributes.HasFlag(FileAttributes.Directory);

        [JsonIgnore]
        public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);

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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');

            sb.Append("Id=");
            sb.Append(Id);

            sb.Append(" PId=");
            sb.Append(ParentId);

            sb.Append(" Name='");
            sb.Append(Name);
            sb.Append('\'');

            sb.Append(" Atts=");
            sb.Append(Attributes);

            sb.Append(" CreationTime=");
            sb.Append(CreationTimeUtc.ToLocalTime());

            sb.Append(" LastWriteTime=");
            sb.Append(LastWriteTimeUtc.ToLocalTime());

            sb.Append(']');
            return sb.ToString();
        }

        public async Task<IFileSystemInfo> GetByNameAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return await System.GetByNameAsync(this, name).ConfigureAwait(false);
        }

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
