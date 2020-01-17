using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Json;
using Google.Apis.Logging;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using GDriveData = Google.Apis.Drive.v3.Data;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // represent one Google Drive account
    public sealed class Account : IDisposable
    {
        // https://developers.google.com/drive/api/v3/mime-types
        public const string FolderMimeType = "application/vnd.google-apps.folder";

        // https://support.google.com/drive/answer/6374270?visit_id=637072450166330725-2466926012&rd=1&hl=en
        // these mime type don't take size on Google drive, so they report a size of zero
        public static string[] GoogleDocMimeTypes = new string[] {
            "application/vnd.google-apps.document",
            "application/vnd.google-apps.drawing",
            "application/vnd.google-apps.file",
            "application/vnd.google-apps.form",
            "application/vnd.google-apps.fusiontable",
            "application/vnd.google-apps.map",
            "application/vnd.google-apps.presentation",
            "application/vnd.google-apps.script",
            "application/vnd.google-apps.site",
            "application/vnd.google-apps.spreadsheet"
        };

        private const string _tokenJsonExt = ".token.json";
        public static Core.Utilities.ILogger Logger { get; set; }

        static Account()
        {
            ApplicationContext.RegisterLogger(new GoogleLogger());
        }

        private Account(string tokenFilePath, bool noReceiver)
        {
            if (!Settings.HasSecretsFile)
                throw new InvalidOperationException();

            if (tokenFilePath == null)
                throw new ArgumentNullException(nameof(tokenFilePath));

            TokenFilePath = tokenFilePath;
            // we want read & write access
            var scopes = new[] { DriveService.Scope.Drive };
            UserCredential credential;
            using (var stream = File.OpenRead(Settings.SecretsFilePath))
            {
                var receiver = noReceiver ? new NullReceiver() : AddAccountBox.GetNewCodeReceiver(Settings.Current.AddAccountClearCookies);
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, scopes, "user", CancellationToken.None, new FileStore(tokenFilePath), receiver).Result;
            }

            Service = GetService(credential);
            RefreshAbout();
            GetRootAndTempFolder();
            UserFileName = GetUserFileName(UserEmailAddress);
            DataDirectoryPath = GetDataDirectoryPath(UserEmailAddress);

            // each GDrive account has its own synchronizer (cloud provider) instance
            IOUtilities.DirectoryCreate(DataDirectoryPath);
            OnDemandLocalFileSystem.EnsureRegistered(DataDirectoryPath, GetRegistration());
            FileSystem = new FileSystem(this);

            // note the mpsync identifier must be *globally unique*, so you can choose a guid, or something like a .NET namespace
            Synchronizer = new MultiPointSynchronizer(typeof(Settings).Namespace, options: new MultiPointSynchronizerOptions { Logger = Settings.SynchronizerLogger, BackupState = true, StateProviderTraceLevel = Settings.Current.StateProviderLogLevel });
            Synchronizer.AddEndPoint("Local", new OnDemandLocalFileSystem(DataDirectoryPath));
            Synchronizer.AddEndPoint("GDrive", FileSystem);
        }

        // a receive that doesn't to anything on purpose
        private class NullReceiver : ICodeReceiver
        {
            public string RedirectUri => "whatever";
            public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken) => Task.FromResult(new AuthorizationCodeResponseUrl());
        }

        public DriveService Service { get; }
        public string TokenFilePath { get; }
        public GDriveData.About About { get; private set; }
        public string UserEmailAddress => About.User.EmailAddress;
        public string UserFileName { get; }
        public string DataDirectoryPath { get; }
        public string RootId { get; private set; }
        public string TempFolderId { get; private set; }
        public MultiPointSynchronizer Synchronizer { get; }
        public FileSystem FileSystem { get; }

        public override string ToString() => UserEmailAddress;

        public static void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => Logger?.Log(level, value, methodName);

        public void Dispose()
        {
#if DEBUG
            Log(TraceLevel.Info, "");
#endif
            Synchronizer?.Dispose();
            FileSystem?.Dispose();
            Service?.Dispose();
        }

        // customize registration to give a nice name to the cloud provider we represent
        private static OnDemandLocalFileSystemRegistration GetRegistration()
        {
            var reg = new OnDemandLocalFileSystemRegistration();
            reg.ProviderName = AssemblyUtilities.GetDescription();
            return reg;
        }

        public void UnregisterOnDemandSynchronizer()
        {
            if (IOUtilities.DirectoryExists(DataDirectoryPath))
            {
                OnDemandLocalFileSystem.Unregister(DataDirectoryPath, GetRegistration());
            }
        }

        // refresh About information (user info, etc.)
        public void RefreshAbout()
        {
            var aboutRequest = Service.About.Get();
            aboutRequest.Fields = "*";
            About = aboutRequest.Execute();
        }

        public IEnumerable<GDriveData.File> GetFolderFiles(string parentId, bool all)
        {
            if (parentId == null)
                throw new ArgumentNullException(nameof(parentId));

            if (parentId != RootId && all)
                throw new NotSupportedException();

            Log(TraceLevel.Info, "parentId:" + parentId + " all:" + all);
            string pageToken = null;
            do
            {
                var request = Service.Files.List();

                request.Fields = "nextPageToken, files(" + GetFileFields() + ")";
                request.PageToken = pageToken;

                // in this implementation, we don't want deleted files
                request.Q = "trashed=false";
                if (parentId != RootId)
                {
                    request.Q += " and '" + parentId + "' in parents";
                }
                request.Spaces = "drive";

                var result = request.Execute();
                foreach (var file in result.Files)
                {
                    // don't send the temp folder
                    if (parentId == RootId && file.Id == TempFolderId)
                        continue;

                    yield return file;
                }

                pageToken = result.NextPageToken;
            } while (pageToken != null);
        }

        public void DeleteFilesByName(string parentId, string name)
        {
            var files = GetFilesByName(parentId, name);
            foreach (var file in files)
            {
                DeleteFile(file.Id);
            }
        }

        public IList<GDriveData.File> GetFilesByName(string parentId, string name)
        {
            if (parentId == null)
                throw new ArgumentNullException(nameof(parentId));

            if (name == null)
                return new List<GDriveData.File>();

            Log(TraceLevel.Info, "parentId:" + parentId + " name:" + name);
            var request = Service.Files.List();
            request.Fields = "files(" + GetFileFields() + ")";

            // escape single quotes in queries
            // https://developers.google.com/drive/api/v3/ref-search-terms
            request.Q = "trashed=false and '" + parentId + "' in parents and name='" + name.Replace("'", @"\'") + "'";
            request.Spaces = "drive";

            // note there can be more than once, but we only need the first
            request.PageSize = 1;
            return request.Execute().Files;
        }

        public GDriveData.File GetFile(string id)
        {
            if (id == null)
                return null; // this is on purpose

            Log(TraceLevel.Info, "id:" + id);
            var request = Service.Files.Get(id);
            request.Fields = GetFileFields();
            return request.Execute();
        }

        public GDriveData.File UpdateFile(string id, DateTime modifiedTime, string mimeType = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            Log(TraceLevel.Info, "id:" + id + " modifiedTime:" + modifiedTime + " mimeType:" + mimeType);
            var fields = new List<string>();
            var file = new GDriveData.File();
            file.MimeType = mimeType;
            fields.Add("mimeType");

            if (modifiedTime != DateTime.MinValue)
            {
                file.ModifiedTime = modifiedTime;
                fields.Add("modifiedTime");
            }

            var request = Service.Files.Update(file, id);
            request.Fields = GetFileFields();
            return request.Execute();
        }

        public void DeleteFile(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            Log(TraceLevel.Info, "id:" + id);
            var request = Service.Files.Delete(id);
            request.Execute();
        }

        public Task DownloadFileAsync(string id, long offset, long count, Stream output, CancellationToken cancellationToken, Action<IDownloadProgress> downloadProgress = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            Log(TraceLevel.Info, "id:" + id + " offset:" + offset + " count:" + count);
            var request = Service.Files.Get(id);
            if (downloadProgress != null)
            {
                request.MediaDownloader.ProgressChanged += (progress) =>
                {
                    downloadProgress(progress);
                };
            }

            return request.DownloadRangeAsync(output, new RangeHeaderValue(offset, (offset + count) - 1), cancellationToken);
        }

        public GDriveData.File CreateFolder(string name, string parentId, DateTime? createdTime = null, DateTime? modifiedTime = null) => CreateFile(name, parentId, createdTime, modifiedTime, FolderMimeType);
        public GDriveData.File CreateFile(string name, string parentId, DateTime? createdTime = null, DateTime? modifiedTime = null, string mimeType = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (parentId == null)
                throw new ArgumentNullException(nameof(parentId));

            Log(TraceLevel.Info, "name:'" + name + "' parentId:" + parentId + " createdTime:" + createdTime + " modifiedTime:" + modifiedTime + " mimeType:" + mimeType);
            var file = new GDriveData.File();
            var request = Service.Files.Create(file);
            file.Name = name;
            file.Parents = new List<string>(new[] { parentId });
            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                file.MimeType = mimeType;
            }

            if (modifiedTime.HasValue)
            {
                file.ModifiedTime = modifiedTime.Value;
            }

            if (createdTime.HasValue)
            {
                file.CreatedTime = createdTime.Value;
            }

            request.Fields = GetFileFields();
            return request.Execute();
        }

        public GDriveData.File MoveFile(string id, string newName, string oldParentId, string newParentId)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (newName == null)
                throw new ArgumentNullException(nameof(newName));

            if (oldParentId == null)
                throw new ArgumentNullException(nameof(oldParentId));

            if (newParentId == null)
                throw new ArgumentNullException(nameof(newParentId));

            Log(TraceLevel.Info, "id:" + id + " newName:" + newName + " oldParentId:" + oldParentId + " newParentId:" + newParentId);

            // note google drive allows multiple files with the same name, so we must delete conflicting targets
            DeleteFilesByName(newParentId, newName);

            var file = new GDriveData.File();
            file.Name = newName;

            // Note: it looks like originalFileName can not be changed, at least not like this
            // For some reason, Google Drive keeps using the original extension (the one with originalFilename), even once the file has been renamed, when we download it using Google Drive UI.
            // So, the whole thing works with Google Drive because the file name we used for temporary uploads has the same extension than the the final name
            var request = Service.Files.Update(file, id);

            if (oldParentId != newParentId)
            {
                request.AddParents = newParentId;
                request.RemoveParents = oldParentId;
            }

            request.Fields = GetFileFields();
            return request.Execute();
        }

        public async Task<GDriveData.File> UploadFileAsync(string parentId, string id, string name, DateTime createdTime, DateTime modifiedTime, Stream input, CancellationToken cancellationToken)
        {
            if (parentId == null)
                throw new ArgumentNullException(nameof(parentId));

            if (id == null)
                throw new ArgumentNullException(nameof(id));

            Log(TraceLevel.Info, "parentId:" + parentId + " id:" + id + " name:" + name + " createdTime:" + createdTime + " modifiedTime:" + modifiedTime);
            var file = new GDriveData.File();
            if (modifiedTime != DateTime.MinValue)
            {
                file.ModifiedTime = modifiedTime;
            }

            if (id == null)
            {
                if (createdTime != DateTime.MinValue)
                {
                    file.CreatedTime = createdTime;
                }
            }

            GDriveData.File result;
            if (input == null)
            {
                if (id == null)
                {
                    var request = Service.Files.Create(file);
                    file.Name = name;
                    file.Parents = new List<string>(new[] { parentId });
                    request.Fields = GetFileFields();
                    result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var request = Service.Files.Update(file, id);
                    request.Fields = GetFileFields();
                    result = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                if (id == null)
                {
                    var request = Service.Files.Create(file, input, null);
                    file.Name = name;
                    file.Parents = new List<string>(new[] { parentId });
                    request.Fields = GetFileFields();
                    await request.UploadAsync(cancellationToken).ConfigureAwait(false);
                    result = request.ResponseBody;
                }
                else
                {
                    var request = Service.Files.Update(file, id, input, null);
                    request.Fields = GetFileFields();
                    await request.UploadAsync(cancellationToken).ConfigureAwait(false);
                    result = request.ResponseBody;
                }
            }

            if (result == null)
            {
                Log(TraceLevel.Info, "result is null.");
            }
            else
            {
                Log(TraceLevel.Info, "result id:" + result.Id);
            }
            return result;
        }

        // get and sync the root folder
        private void GetRootAndTempFolder()
        {
            var rootFile = Service.Files.Get("root").Execute();
            RootId = rootFile.Id;

            var tempFolderName = Settings.Current.GoogleTempFolderName.Nullify() ?? Settings._defaultGoogleTempFolderName;
            var tempFolder = GetFilesByName(RootId, tempFolderName).FirstOrDefault();
            if (tempFolder == null)
            {
                tempFolder = CreateFolder(tempFolderName, RootId);
            }
            TempFolderId = tempFolder.Id;
        }

        private string GetLastStartPageToken()
        {
            var path = GetStartPageTokenFilePath(UserEmailAddress);
            if (!IOUtilities.FileExists(path))
                return null;

            try
            {
                return File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private void SetLastStartPageToken(string token)
        {
            var path = GetStartPageTokenFilePath(UserEmailAddress);
            File.WriteAllText(path, token);
        }

        public void ResetLastStartPageToken()
        {
            var path = GetStartPageTokenFilePath(UserEmailAddress);
            IOUtilities.FileDelete(path);
        }

        public void SynchronizeChanges(EventHandler<SyncFileSystemEventArgs> eventHandler)
        {
            var lastToken = GetLastStartPageToken();
            var response = Service.Changes.GetStartPageToken().Execute();
            var changeToken = response.StartPageTokenValue;

            if (lastToken != null && changeToken != lastToken)
            {
                TrackChanges(lastToken, eventHandler);
                return;
            }

            SetLastStartPageToken(changeToken);
        }

        // track changes using Google Drive Change API
        // https://developers.google.com/drive/api/v3/reference/changes
        private void TrackChanges(string token, EventHandler<SyncFileSystemEventArgs> eventHandler)
        {
            string pageToken = token;
            while (pageToken != null)
            {
                var request = Service.Changes.List(pageToken);
                request.Spaces = "drive";
                request.Fields = "*"; // should be optimized
                var changes = request.Execute();
                foreach (var change in changes.Changes)
                {
                    try
                    {
                        ProcessChange(change, eventHandler);
                    }
                    catch (Exception e)
                    {
                        Log(TraceLevel.Warning, "An error has occurred trying to process change: " + e);
                        return;
                    }
                }

                if (changes.NewStartPageToken != null)
                {
                    SetLastStartPageToken(changes.NewStartPageToken);
                }
                pageToken = changes.NextPageToken;
            }
        }

        // return true to resynchronize root
        private void ProcessChange(GDriveData.Change change, EventHandler<SyncFileSystemEventArgs> eventHandler)
        {
            if (eventHandler == null)
                return;

            if (change.Type == "file")
            {
                Log(TraceLevel.Info, "Change " + Trace(change));
                if ((change.File == null && change.Removed == true) || change.File?.Trashed == true)
                {
                    eventHandler.Invoke(FileSystem, new SyncFileSystemEventArgs(SyncFileSystemEventType.Deleted, change.Time.Value, new StateSyncEntry { Id = change.FileId }));
                    Log(TraceLevel.Info, "File with id '" + change.FileId + "' was deleted.");
                    return;
                }

                if (change.File == null)
                {
                    Log(TraceLevel.Warning, "Don't know what to do with a change without an associated file.");
                    return;
                }

                if (change.File.Name != null && change.File.Name.EndsWith(FileSystem.UrlExt, StringComparison.OrdinalIgnoreCase))
                {
                    Log(TraceLevel.Warning, "File '" + change.File.Name + "' is a .url, skipped.");
                    return;
                }

                var parentId = change.File.Parents?.FirstOrDefault();

                eventHandler.Invoke(FileSystem, new SyncFileSystemEventArgs(SyncFileSystemEventType.Changed, change.Time.Value, new StateSyncEntry { Id = change.FileId, ParentId = parentId }));
            }
        }

        private static string Trace(GDriveData.Change change)
        {
            var list = new List<Tuple<string, object>>();
            list.Add(new Tuple<string, object>("FileId", change.FileId));
            list.Add(new Tuple<string, object>("ChangeType", change.ChangeType));
            if (change.Removed.HasValue)
            {
                list.Add(new Tuple<string, object>("Removed", change.Removed.Value));
            }

            if (change.Time.HasValue)
            {
                list.Add(new Tuple<string, object>("Time", change.Time.Value));
            }

            if (change.File != null)
            {
                list.Add(new Tuple<string, object>("File.Name", change.File.Name));
                list.Add(new Tuple<string, object>("File.MimeType", change.File.MimeType));
                if (change.File.OriginalFilename != null)
                {
                    list.Add(new Tuple<string, object>("File.OriginalFilename", change.File.OriginalFilename));
                }

                if (change.File.Size.HasValue)
                {
                    list.Add(new Tuple<string, object>("File.Size", change.File.Size.Value));
                }

                if (change.File.Trashed.HasValue)
                {
                    list.Add(new Tuple<string, object>("File.Trashed", change.File.Trashed.Value));
                }

                if (change.File.Version.HasValue)
                {
                    list.Add(new Tuple<string, object>("File.Version", change.File.Version.Value));
                }

                if (change.File.Parents != null)
                {
                    list.Add(new Tuple<string, object>("File.Parents", string.Join("|", change.File.Parents)));
                }

                if (change.File.CreatedTime.HasValue)
                {
                    list.Add(new Tuple<string, object>("File.CreatedTime", change.File.CreatedTime.Value));
                }

                if (change.File.ModifiedTime.HasValue)
                {
                    list.Add(new Tuple<string, object>("File.ModifiedTime", change.File.ModifiedTime.Value));
                }
            }

            return string.Join(", ", list.Select(c => c.Item1 + "=" + c.Item2));
        }

        // these are the fields we really use
        // fields are described here
        // https://developers.google.com/drive/api/v3/reference/files
        private static string GetFileFields() => "id, parents, name, mimeType, size, version, modifiedTime, createdTime, webViewLink";

        private static DriveService GetService(UserCredential credential) => new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "ShellBoost Drive Sample",
        });

        private static Account FromFile(string filePath, bool noReceiver)
        {
            try
            {
                return new Account(filePath, noReceiver);
            }
            catch (Exception e)
            {
                Log(TraceLevel.Error, "An error occured trying to load the account for token file '" + filePath + "':" + e);
                return null;
            }
        }

        public static string GetStartPageTokenFilePath(string emailAddress) => Path.Combine(Settings.ConfigurationDirectoryPath, GetUserFileName(emailAddress) + "_startPageToken.txt");
        public static string GetTokenFilePath(string emailAddress) => Path.Combine(Settings.ConfigurationDirectoryPath, GetUserFileName(emailAddress) + _tokenJsonExt);
        public static string GetDataDirectoryPath(string emailAddress) => Path.Combine(Settings.DataDirectoryPath, GetUserFileName(emailAddress));
        public static string GetUserFileName(string emailAddress)
        {
            if (emailAddress == null)
                throw new ArgumentNullException(nameof(emailAddress));

            return IOUtilities.PathToValidFileName(emailAddress);
        }

        public static bool IsGoogleDoc(GDriveData.File file) => file != null && file.MimeType != null && GoogleDocMimeTypes.Contains(file.MimeType);

        // get all accounts stored in the app local data directory
        public static IEnumerable<Account> GetAllAccounts(bool noReceiver)
        {
            foreach (var file in Directory.EnumerateFiles(Settings.ConfigurationDirectoryPath, "*" + _tokenJsonExt))
            {
                var account = FromFile(file, noReceiver);
                if (account != null)
                    yield return account;
            }
        }

        // remove an account (token, files, etc.) from app local data directory
        public void Remove()
        {
            var tokenFilePath = GetTokenFilePath(UserEmailAddress);
            IOUtilities.FileDelete(tokenFilePath, true);
            ResetLastStartPageToken();
        }

        // adds an account to app local data directory
        // uses Google Sign-In (pops up a form)
        public static GDriveData.About AddAccount()
        {
            if (!Settings.HasSecretsFile)
                throw new InvalidOperationException();

            // we want read & write access
            var scopes = new[] { DriveService.Scope.Drive };
            UserCredential credential;
            var tempPath = Path.Combine(Settings.ConfigurationDirectoryPath, "temp" + _tokenJsonExt);
            using (var stream = File.OpenRead(Settings.SecretsFilePath))
            {
                IOUtilities.FileCreateDirectory(tempPath);
                var store = new FileStore(tempPath);
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, scopes, "user", CancellationToken.None, store, AddAccountBox.GetNewCodeReceiver(Settings.Current.AddAccountClearCookies)).Result;
            }

            using (var service = GetService(credential))
            {
                var aboutRequest = service.About.Get();
                aboutRequest.Fields = "user";
                var about = aboutRequest.Execute();
                var email = about.User.EmailAddress;
                var path = GetTokenFilePath(email);

                // note this could overwrite an existing token file. this is ok since this one is more recent
                IOUtilities.FileMove(tempPath, path, true);
                Settings.Current.ResetAccounts();
                return about;
            }
        }

        // we use our own implementation of IDataStore that store the token in a single file
        // this implementation doesn't use key since the key is the file itself
        private class FileStore : IDataStore
        {
            public FileStore(string tokenFilePath)
            {
                TokenFilePath = tokenFilePath;
            }

            public string TokenFilePath { get; }

            Task IDataStore.StoreAsync<T>(string key, T value)
            {
                var serialized = NewtonsoftJsonSerializer.Instance.Serialize(value);
                IOUtilities.FileCreateDirectory(TokenFilePath);
                File.WriteAllText(TokenFilePath, serialized);
                return Task.CompletedTask;
            }

            Task IDataStore.DeleteAsync<T>(string key)
            {
                IOUtilities.FileDelete(TokenFilePath, true, false);
                return Task.CompletedTask;
            }

            Task<T> IDataStore.GetAsync<T>(string key)
            {
                if (IOUtilities.FileExists(TokenFilePath))
                {
                    try
                    {
                        var serialized = File.ReadAllText(TokenFilePath);
                        if (!string.IsNullOrEmpty(serialized))
                        {
                            var result = NewtonsoftJsonSerializer.Instance.Deserialize<T>(serialized);
                            return Task.FromResult(result);
                        }
                    }
                    catch
                    {
                        // continue
                    }
                }
                return Task.FromResult(default(T));
            }

            Task IDataStore.ClearAsync()
            {
                IOUtilities.FileDelete(TokenFilePath, true, false);
                return Task.CompletedTask;
            }
        }

        // log Google Apis work
        private class GoogleLogger : BaseLogger
        {
            public GoogleLogger()
                : base(LogLevel.All, null, null)
            {
            }

            protected override Google.Apis.Logging.ILogger BuildNewLogger(Type type) => new GoogleLogger();
            protected override void Log(LogLevel logLevel, string formattedMessage)
            {
                // convert to our own logging system
                TraceLevel level;
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        level = TraceLevel.Verbose;
                        break;

                    case LogLevel.Warning:
                        // hack remove this bogus warning
                        if (formattedMessage != null && formattedMessage.IndexOf("Add parameter should not get null values. type=Query, name=key", StringComparison.OrdinalIgnoreCase) >= 0)
                            return;

                        level = TraceLevel.Warning;
                        break;

                    case LogLevel.Error:
                        level = TraceLevel.Error;
                        break;

                    default:
                        level = TraceLevel.Info;
                        break;
                }

                Logger?.Log(level, formattedMessage, string.Empty);
            }
        }
    }
}
