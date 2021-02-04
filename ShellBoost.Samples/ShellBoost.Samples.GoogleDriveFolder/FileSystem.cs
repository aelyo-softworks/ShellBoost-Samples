using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using GDriveData = Google.Apis.Drive.v3.Data;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public sealed class FileSystem : ISyncFileSystemRead, ISyncFileSystemWrite, ISyncFileSystemEvents, IDisposable
    {
        // google docs are special: they have a zero size since they don't participate in user's quota
        // so we just create a .url file to redirect to the web
        public const string UrlExt = ".url";

        private readonly Timer _timer;

        public FileSystem(Account account)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            Account = account;
            _timer = new Timer((state) => Synchronize());
        }

        public Account Account { get; }

        public void Dispose()
        {
#if DEBUG
            Account.Log(TraceLevel.Info, "");
#endif
            _timer?.Dispose();
        }

        public void Synchronize()
        {
            try
            {
                Account.SynchronizeChanges(Event);
            }
            catch (Exception e)
            {
                // race condition can happen on dispose
                Account.Log(TraceLevel.Warning, "Error:" + e);
            }
        }

        #region ISyncFileSystem
        public string RootId => Account.RootId;

        public EndPointSynchronizer EndPointSynchronizer { get; set; }
        #endregion ISyncFileSystem

        #region ISyncFileSystemEvents
        public event EventHandler<SyncFileSystemEventArgs> Event;

        public void StartEventMonitoring() => _timer.Change(0, Settings.Current.SyncPeriod * 1000);
        public void StopEventMonitoring() => _timer.Change(0, Timeout.Infinite);

        #endregion ISyncFileSystemEvents

        #region ISyncFileSystemRead
        public IEnumerable<StateSyncEntry> EnumerateEntries(SyncContext context, StateSyncEntry parentEntry, SyncEnumerateEntriesOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (parentEntry == null)
                throw new ArgumentNullException(nameof(parentEntry));

            options = options ?? new SyncEnumerateEntriesOptions();
            foreach (var file in Account.GetFolderFiles(parentEntry.Id, options.Level == EnumerationLevel.AllChildren))
            {
                var entry = ToEntry(file);
                yield return entry;
            }
        }

        public Task GetEntryContentAsync(SyncContext context, StateSyncEntry entry, Stream output, SyncGetEntryContentOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            options = options ?? new SyncGetEntryContentOptions();
            var webViewLink = entry.GetNullifiedExtendedDataValue("WebViewLink");
            Account.Log(TraceLevel.Verbose, "offset:" + options.Offset + " count:" + options.Count + " webViewLink:" + webViewLink);
            if (webViewLink != null)
            {
                var content = GetUrlFileContent(webViewLink);
                return output.WriteAsync(content, (int)options.Offset, (int)Math.Min(options.Count, content.Length));
            }

            var sink = context.ProgressSink;
            if (sink == null)
                return Account.DownloadFileAsync(entry.Id, options.Offset, options.Count, output, options.CancellationToken);

            return Account.DownloadFileAsync(entry.Id, options.Offset, options.Count, output, options.CancellationToken, (p) =>
            {
                sink.Progress(context, entry.Size, p.BytesDownloaded);
            });
        }

        public bool HasCapability(SyncFileSystemCapability capability)
        {
            switch (capability)
            {
                case SyncFileSystemCapability.AllChildrenEnumeration:
                case SyncFileSystemCapability.GetPartialContent:
                case SyncFileSystemCapability.SetPartialContent:
                    return true;

                default:
                    return false;
            }
        }

        #endregion ISyncFileSystemRead

        #region ISyncFileSystemWrite
        public void GetOrCreateEntry(SyncContext context, StateSyncEntry entry, SyncGetEntryOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            options = options ?? new SyncGetEntryOptions();
            var file = GetOrCreateFile(entry, options);
            if (file == null && !options.CanCreate)
                return;

            CopyToEntry(file, entry);
        }

        private GDriveData.File GetOrCreateFile(StateSyncEntry entry, SyncGetEntryOptions options)
        {
            GDriveData.File file;
            if (entry.Id == null)
            {
                if (!options.IsTemporary)
                    return null;

                // entry is a temporary entry
                file = Account.GetFilesByName(Account.TempFolderId, entry.Name).FirstOrDefault();
                if (file != null)
                    return file;

                Account.Log(TraceLevel.Warning, "Cannot find temp entry '" + entry.Name + "' with parent temp folder id '" + Account.TempFolderId + "'.");
                if (!options.CanCreate)
                    return null;

                return Account.CreateFile(entry.Name, Account.TempFolderId);
            }

            file = Account.GetFile(entry.Id);
            if (file != null)
                return file;

            Account.Log(TraceLevel.Warning, "Cannot find entry with id '" + entry.Id + "' name '" + entry.Name + "'.");
            if (!options.CanCreate)
                return null;

            // we can't create google docs
            if (IsGoogleDoc(entry))
                return null;

            return CreateEntry(entry);
        }

        public void UpdateEntry(SyncContext context, StateSyncEntry entry, SyncUpdateEntryOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // we can't update google docs
            if (IsGoogleDoc(entry))
                return;

            options = options ?? new SyncUpdateEntryOptions();
            var file = Account.GetFile(entry.Id);
            if (file == null)
            {
                if (entry.Id != null)
                {
                    Account.Log(TraceLevel.Warning, "Cannot find entry '" + entry.Id + "' name '" + entry.Name + "'.");
                }
                if (!options.CanCreate)
                    return;

                file = CreateEntry(entry);
            }
            else
            {
                // is this the rename/move case?
                if (!entry.Name.EqualsIgnoreCase(file.Name) || entry.ParentId != file.Parents.FirstOrDefault())
                {
                    file = Account.MoveFile(file.Id, entry.Name, file.Parents.FirstOrDefault(), entry.ParentId);
                }
            }

            // note Google Drive cannot updated the created time of a file
            if (context.MultiPointSynchronizer.NormalizeDateTime(entry.LastWriteTime) != context.MultiPointSynchronizer.NormalizeDateTime(file.ModifiedTime.Value) ||
                entry.Attributes != ToAttributes(file))
            {
                string mimeType = null;
                if (entry.IsDirectory)
                {
                    mimeType = Account.FolderMimeType;
                }

                file = Account.UpdateFile(file.Id, entry.LastWriteTime.LocalDateTime, mimeType);
            }

            // note: unlike Windows on-demand files, we can't update the size w/o uploading a whole file

            CopyToEntry(file, entry);
        }

        public void DeleteEntry(SyncContext context, StateSyncEntry entry, SyncDeleteEntryOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            Account.DeleteFile(entry.Id);
        }

        public async Task SetEntryContentAsync(SyncContext context, StateSyncEntry entry, Stream input, SyncSetEntryContentOptions options = null)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // we can't update google docs
            if (IsGoogleDoc(entry))
                return;

            options = options ?? new SyncSetEntryContentOptions();
            var file = await Account.UploadFileAsync(entry.ParentId, entry.Id, entry.Name, entry.CreationTime.LocalDateTime, entry.LastWriteTime.LocalDateTime, input, options.CancellationToken).ConfigureAwait(false);
            if (file != null)
            {
                entry.Id = file.Id;
            }
        }
        #endregion ISyncFileSystemWrite

        private SyncEntryAttributes ToAttributes(GDriveData.File file)
        {
            if (file.MimeType == Account.FolderMimeType)
                return SyncEntryAttributes.Directory;

            return SyncEntryAttributes.None;
        }

        private GDriveData.File CreateEntry(StateSyncEntry entry)
        {
            if (entry.IsDirectory)
                return Account.CreateFolder(entry.Name, entry.ParentId, entry.CreationTime.LocalDateTime, entry.LastWriteTime.LocalDateTime);

            return Account.CreateFile(entry.Name, entry.ParentId, entry.CreationTime.LocalDateTime, entry.LastWriteTime.LocalDateTime);
        }

        private bool IsGoogleDoc(StateSyncEntry entry) => entry != null && entry.Name.EndsWith(UrlExt, StringComparison.OrdinalIgnoreCase);

        private void CopyToEntry(GDriveData.File file, StateSyncEntry entry)
        {
            if (file.MimeType == Account.FolderMimeType)
            {
                entry.Attributes |= SyncEntryAttributes.Directory;
            }

            // google docs are special: they have a zero size since they don't participate in user's quota
            // so we just create a .url file to redirect to the web
            if (Account.IsGoogleDoc(file))
            {
                entry.Name = file.Name + UrlExt;

                // .url format is
                // [InternetShortcut]
                // URL=https://developers.google.com/drive/api/v3/mime-types
                entry.Size = GetUrlFileContent(file.WebViewLink).Length;
                entry.SetExtendedDataValue("WebViewLink", file.WebViewLink);
            }
            else
            {
                entry.Name = file.Name;
                entry.Size = file.Size.GetValueOrDefault();
            }

            entry.ContentVersion = file.Version.GetValueOrDefault().ToString();
            if (file.CreatedTime.HasValue)
            {
                entry.CreationTime = file.CreatedTime.Value;
            }

            if (file.ModifiedTime.HasValue)
            {
                entry.LastWriteTime = file.ModifiedTime.Value;
            }

            entry.Id = file.Id;
            entry.ParentId = file.Parents?.FirstOrDefault();
        }

        private StateSyncEntry ToEntry(GDriveData.File file)
        {
            var entry = new StateSyncEntry();
            CopyToEntry(file, entry);
            return entry;
        }

        // .url format is
        // [InternetShortcut]
        // URL=<escaped url>
        private static byte[] GetUrlFileContent(string rawUrl)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[InternetShortcut]");
            sb.Append("URL=");
            sb.AppendLine(rawUrl);
            return Encoding.Default.GetBytes(sb.ToString());
        }
    }
}
