using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // this represents a File On-Demand remote file system backed by Google Drive
    // all communications withe the OnDemandSynchronizer is done here
    public sealed class FileSystem : IRemoteFileSystem
    {
        // google docs are special: they have a zero size since they don't participate in user's quota
        // so we just create a .url file to redirect to the web
        public const string UrlExt = ".url";

        private readonly List<Account> _accounts;

        public FileSystem()
        {
            _accounts = new List<Account>();
        }

        public IReadOnlyList<Account> Accounts => _accounts;

        // add an account to handle
        public Account AddAccount(Account account, bool synchronizeRootFolder = false)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            var existing = _accounts.FirstOrDefault(a => a.UserEmailAddress.EqualsIgnoreCase(account.UserEmailAddress));
            if (existing != null)
                return existing;

            _accounts.Add(account);
            account.InitializeDatabase(synchronizeRootFolder);
            return account;
        }

        // for a given path, get the account (and the left path part)
        private Account GetAccount(string path, out string relativePath)
        {
            Account account = null;
            relativePath = null;
            int pos = path.IndexOf('\\');
            if (pos < 0)
            {
                account = Accounts.FirstOrDefault(a => a.UserFileName.Equals(path));
            }
            else
            {
                account = Accounts.FirstOrDefault(a => a.UserFileName.Equals(path.Substring(0, pos)));
                if (account != null)
                {
                    relativePath = path.Substring(pos + 1);
                }
            }

            if (account == null)
            {
                relativePath = null;
            }
            else if (relativePath == null)
            {
                relativePath = string.Empty;
            }
            return account;
        }

        // IRemoteFileSystem implementation
        public IEnumerable<IRemoteResource> EnumResources(RemoteOperationContext context, string parentPath)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // first level are accounts folders
            if (string.IsNullOrEmpty(parentPath))
            {
                foreach (var acc in _accounts)
                {
                    yield return new AccountResource(acc);
                }
                yield break;
            }

            var account = GetAccount(parentPath, out var relativePath);
            if (account == null)
                yield break;

            foreach (var file in account.GetFolderFiles(relativePath))
            {
                Account.Log(TraceLevel.Info, "File: " + file);
                yield return new FileResource(file);
            }
        }

        public void CreateDirectory(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // first level are accounts folders
            // we can't create a first level directory
            var account = GetAccount(path, out var relativePath);
            if (account == null || string.IsNullOrEmpty(relativePath))
                return;

            Account.Log(TraceLevel.Info, "Before RelativePath: " + relativePath);
            account.CreateDirectory(relativePath, Path.Combine(account.DataDirectoryPath, relativePath));
            Account.Log(TraceLevel.Info, "After RelativePath: " + relativePath);
        }

        public void DeleteResource(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            // first level are accounts folders
            // we can't delete a first level directory
            var account = GetAccount(path, out var relativePath);
            if (account == null || string.IsNullOrEmpty(relativePath))
                return;

            relativePath = NormPath(relativePath);
            Account.Log(TraceLevel.Info, "Before RelativePath: " + relativePath);
            account.DeleteFile(relativePath);
            Account.Log(TraceLevel.Info, "After RelativePath: " + relativePath);
        }

        public void DownloadResource(RemoteOperationContext context, string path, long offset, long count, Stream output)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            // first level are accounts folders
            // we can't download a file from there
            var account = GetAccount(path, out var relativePath);
            if (account == null || string.IsNullOrEmpty(relativePath))
                return;

            // for google doc, we just write the .url file content
            if (path.EndsWith(UrlExt))
            {
                relativePath = NormPath(relativePath);
                var file = account.GetFile(relativePath);
                if (file == null)
                    return;

                var content = GetUrlFileContent(file.WebViewLink);
                output.Write(content, 0, Math.Min((int)count, content.Length));
                return;
            }

            Account.Log(TraceLevel.Info, "Before RelativePath: " + relativePath + " offset: " + offset + " count: " + count);
            account.DownloadFile(relativePath, offset, count, output, context);
            Account.Log(TraceLevel.Info, "After RelativePath: " + relativePath);
        }

        public void UploadResource(RemoteOperationContext context, string path, Stream input)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // can't change google doc
            if (path.EndsWith(UrlExt))
                return;

            // first level are accounts folders
            // we can't upload a file from there
            var account = GetAccount(path, out var relativePath);
            if (account == null || string.IsNullOrEmpty(relativePath))
                return;

            Account.Log(TraceLevel.Info, "Before RelativePath: " + relativePath);
            account.UploadFile(relativePath, Path.Combine(account.DataDirectoryPath, relativePath), input);
            Account.Log(TraceLevel.Info, "After RelativePath: " + relativePath);
        }

        public IRemoteResource GetResource(RemoteOperationContext context, string path)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var account = GetAccount(path, out var relativePath);
            if (account == null)
                return null;

            if (string.IsNullOrEmpty(relativePath))
                return new AccountResource(account);

            relativePath = NormPath(relativePath);
            var file = account.GetFile(relativePath);
            if (file == null)
                return null;

            return new FileResource(file);
        }

        public void UpdateResource(RemoteOperationContext context, string path, IRemoteResource resource, IReadOnlyDictionary<string, object> properties)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            // first level are accounts folders
            // we can't update anything
            var account = GetAccount(path, out var relativePath);
            if (account == null || string.IsNullOrEmpty(relativePath))
                return;

            // rename is when the resource name doesn't match the path
            bool rename = resource.DisplayName != null && !resource.DisplayName.EqualsIgnoreCase(Path.GetFileName(path));
            if (rename)
            {
                relativePath = NormPath(relativePath);
                Account.Log(TraceLevel.Info, "Before RelativePath: " + relativePath + " new name: " + resource.DisplayName);
                account.RenameFile(relativePath, resource.DisplayName);
                Account.Log(TraceLevel.Info, "After RelativePath: " + relativePath);
                return;
            }

            // else we currently don't support this
        }

        public bool TryGetPropertyValue(string name, out object value) { value = null; return false; }

        private static string NormPath(string path)
        {
            if (path == null)
                return null;

            // remove the .url extension, if any
            if (path.EndsWith(UrlExt))
            {
                path = path.Substring(0, path.Length - UrlExt.Length);
            }
            return path;
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

        // classes that implement File On-Demand IRemoteResource
        private class AccountResource : BaseRemoteResource
        {
            public AccountResource(Account account)
            {
                Account = account;
                DisplayName = account.UserFileName;
                Attributes |= FileAttributes.Directory;

                // we use token file for dates... why not
                LastWriteTimeUtc = File.GetLastWriteTimeUtc(account.TokenFilePath);
                CreationTimeUtc = File.GetCreationTimeUtc(account.TokenFilePath);

                // content length is 0
                // content type is undefined
                // etag is undefined
            }

            public Account Account { get; }
        }

        private class FileResource : BaseRemoteResource
        {
            public FileResource(DriveFile file)
            {
                // google docs are special: they have a zero size since they don't participate in user's quota
                // so we just create a .url file to redirect to the web
                if (file.IsGoogleDoc)
                {
                    DisplayName = file.FileName + UrlExt;

                    // .url format is
                    // [InternetShortcut]
                    // URL=https://developers.google.com/drive/api/v3/mime-types
                    ContentLength = GetUrlFileContent(file.WebViewLink).Length;
                    ContentType = "text/x-url"; // Google seems to be using this
                }
                else
                {
                    DisplayName = file.FileName;
                    ContentLength = file.Size;
                    ContentType = file.MimeType;
                }

                Attributes = FileAttributes.Normal;
                if (file.IsDirectory)
                {
                    Attributes = FileAttributes.Directory;
                }

                CreationTimeUtc = file.CreatedTime.ToUniversalTime();
                LastWriteTimeUtc = file.ModifiedTime.ToUniversalTime();

                // etag is not supported by Google Drive v3
            }
        }
    }
}
