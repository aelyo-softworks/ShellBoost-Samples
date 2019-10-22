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
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Json;
using Google.Apis.Logging;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    // represent one Google Drive account
    public sealed class Account : IDisposable
    {
        private const string _tokenJsonExt = ".token.json";
        private const string _changeTokenKey = "ChangeToken";
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
            using (var stream = System.IO.File.OpenRead(Settings.SecretsFilePath))
            {
                var receiver = noReceiver ? new NullReceiver() : AddAccountBox.GetNewCodeReceiver(Settings.Current.AddAccountClearCookies);
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.Load(stream).Secrets, scopes, "user", CancellationToken.None, new FileStore(tokenFilePath), receiver).Result;
            }

            Service = GetService(credential);
            RefreshAbout();
            UserFileName = GetUserFileName(UserEmailAddress);
            DataDirectoryPath = GetDataDirectoryPath(UserEmailAddress);
            DatabaseFilePath = GetDatabaseFilePath(UserEmailAddress);
            Database = new LocalDatabase(DatabaseFilePath);
        }

        // a receive that doesn't to anything on purpose
        private class NullReceiver : ICodeReceiver
        {
            public string RedirectUri => "whatever";
            public Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync(AuthorizationCodeRequestUrl url, CancellationToken taskCancellationToken) => Task.FromResult(new AuthorizationCodeResponseUrl());
        }

        public DriveService Service { get; }
        public string TokenFilePath { get; }
        public About About { get; private set; }
        public string UserEmailAddress => About.User.EmailAddress;
        public string UserFileName { get; }
        public string DatabaseFilePath { get; }
        public string DataDirectoryPath { get; }
        public LocalDatabase Database { get; }
        public string RootId { get; private set; }

        public override string ToString() => UserEmailAddress;

        private static void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => Logger?.Log(level, value, methodName);

        public void Dispose()
        {
            Service?.Dispose();
            Database?.Dispose();
        }

        // start the database linked to this account
        public void InitializeDatabase(bool synchronizeRootFolder = false)
        {
            GetRootFolder();

            bool needRootRefresh = SynchronizeChanges();
            if (synchronizeRootFolder || needRootRefresh)
            {
                SynchronizeRootFolder();
            }
        }

        // refresh About information (user info, etc.)
        public void RefreshAbout()
        {
            var aboutRequest = Service.About.Get();
            aboutRequest.Fields = "*";
            About = aboutRequest.Execute();
        }

        public IEnumerable<DriveFile> GetFolderFiles(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            var id = GetId(relativePath);
            if (id == null)
            {
                Log(TraceLevel.Warning, "Id '" + id + "' wasn't found in the database.");
                return Enumerable.Empty<DriveFile>();
            }

            return Database.GetFolderFiles(id);
        }

        public void DeleteFile(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            string id = GetId(relativePath);
            if (id == null)
            {
                Log(TraceLevel.Warning, "Id '" + id + "' wasn't found in the database.");
                return;
            }

            var request = Service.Files.Delete(id);
            request.Execute();
            Database.DeleteFile(id);
        }

        public DriveFile GetFile(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            string id = GetId(relativePath);
            if (id == null)
            {
                Log(TraceLevel.Warning, "Id '" + id + "' wasn't found in the database.");
                return null;
            }

            return Database.GetFile(id);
        }

        public void DownloadFile(string relativePath, long offset, long count, Stream output, RemoteOperationContext context)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            string id = GetId(relativePath);
            if (id == null)
            {
                Log(TraceLevel.Warning, "Id '" + id + "' wasn't found in the database.");
                return;
            }

            var file = Database.GetFile(id);
            if (file == null)
            {
                Log(TraceLevel.Warning, "File id '" + id + "' wasn't found in the database.");
                return;
            }

            var request = Service.Files.Get(id);
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                context.Synchronizer.ReportProgress(context.CallbackContext, file.Size, progress.BytesDownloaded);
            };

            var prog = request.DownloadRange(output, new RangeHeaderValue(offset, (offset + count) - 1));
            if (prog.BytesDownloaded != count)
            {
                // downloaded bytes size is not the same as expected, update our local database
                request = Service.Files.Get(id);
                request.Fields = GetFileFields();
                var result = request.Execute();
                var driveFile = DriveFile.From(result);

                if (driveFile == null || string.IsNullOrEmpty(driveFile.Id) || driveFile.Id != id)
                {
                    // check google api log
                    Log(TraceLevel.Error, "Drive result is empty or its id is invalid.");
                }
                else
                {
                    Database.SynchronizeOne(relativePath, driveFile);
                }
            }
        }

        public void UploadFile(string relativePath, string fullPath, Stream input)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (fullPath == null)
                throw new ArgumentNullException(nameof(fullPath));

            // note input can be null (if the file is currently locked but exists locally)

            var parentPath = Path.GetDirectoryName(relativePath);
            string parentId = GetId(parentPath);
            if (parentId == null)
            {
                Log(TraceLevel.Warning, "Parent id '" + parentId + "' wasn't found in the database.");
                return;
            }

            var id = GetId(relativePath); // can be null

            var file = new Google.Apis.Drive.v3.Data.File();
            var fi = new FileInfo(fullPath);
            if (fi.Exists)
            {
                file.ModifiedTime = fi.LastWriteTime;
                if (id == null)
                {
                    file.CreatedTime = fi.CreationTime;
                }
            }

            Google.Apis.Drive.v3.Data.File result;
            if (input == null)
            {
                if (id == null)
                {
                    var request = Service.Files.Create(file);
                    file.Name = Path.GetFileName(relativePath);
                    file.Parents = new List<string>(new[] { parentId });
                    request.Fields = GetFileFields();
                    result = request.Execute();
                }
                else
                {
                    var request = Service.Files.Update(file, id);
                    request.Fields = GetFileFields();
                    result = request.Execute();
                }
            }
            else
            {
                if (id == null)
                {
                    var request = Service.Files.Create(file, input, null);
                    file.Name = Path.GetFileName(relativePath);
                    file.Parents = new List<string>(new[] { parentId });
                    request.Fields = GetFileFields();
                    request.Upload();
                    result = request.ResponseBody;
                }
                else
                {
                    var request = Service.Files.Update(file, id, input, null);
                    request.Fields = GetFileFields();
                    request.Upload();
                    result = request.ResponseBody;
                }
            }

            var driveFile = DriveFile.From(result);
            if (driveFile == null || string.IsNullOrEmpty(driveFile.Id))
            {
                // check google api log
                Log(TraceLevel.Error, "Drive result is empty or its id is invalid.");
            }
            else
            {
                Database.SynchronizeOne(relativePath, driveFile);
            }
        }

        public void CreateDirectory(string relativePath, string fullPath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (fullPath == null)
                throw new ArgumentNullException(nameof(fullPath));

            var parentPath = Path.GetDirectoryName(relativePath);
            string parentId = GetId(parentPath);
            if (parentId == null)
            {
                Log(TraceLevel.Warning, "Parent id '" + parentId + "' wasn't found in the database.");
                return;
            }

            var file = new Google.Apis.Drive.v3.Data.File();
            file.Name = Path.GetFileName(relativePath);
            file.MimeType = DriveFile.FolderMimeType;
            file.Parents = new List<string>(new[] { parentId });

            var di = new DirectoryInfo(fullPath);
            if (di.Exists)
            {
                file.ModifiedTime = di.LastWriteTime;
                file.CreatedTime = di.CreationTime;
            }

            var request = Service.Files.Create(file);
            request.Fields = GetFileFields();
            var result = request.Execute();
            var driveFile = DriveFile.From(result);
            if (driveFile == null || string.IsNullOrEmpty(driveFile.Id))
            {
                // check google api log
                Log(TraceLevel.Error, "Drive result is empty or its id is invalid.");
            }
            else
            {
                Database.SynchronizeOne(relativePath, driveFile);
            }
        }

        // get full path from a file id
        private string GetFullPath(string id)
        {
            var path = GetRelativePath(id);
            if (path == null)
                return null;

            return Path.Combine(DataDirectoryPath, path);
        }

        private string GetRelativePath(string id)
        {
            if (id == RootId)
                return string.Empty;

            return Database.GetRelativePath(id);
        }

        // get id from relativePath
        private string GetId(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return RootId;

            return Database.GetId(relativePath);
        }

        // get and sync the root folder
        private void GetRootFolder()
        {
            var rootFile = Service.Files.Get("root").Execute();
            RootId = rootFile.Id;
            Database.SetValue(nameof(RootId), RootId);
        }

        private void SynchronizeRootFolder()
        {
            // note we read all (not trashed) files here. this could be optimized
            var folder = LoadAllFiles(RootId);
            Database.SynchronizeAll(folder);
        }

        public bool SynchronizeChanges()
        {
            var dbChangeToken = Database.GetNullifiedValue(_changeTokenKey);
            var response = Service.Changes.GetStartPageToken().Execute();
            var changeToken = response.StartPageTokenValue;

            if (dbChangeToken != null && changeToken != dbChangeToken)
                return TrackChanges(dbChangeToken);

            Database.SetValue(_changeTokenKey, changeToken);
            return false;
        }

        // track changes using Google Drive Change API
        // https://developers.google.com/drive/api/v3/reference/changes
        private bool TrackChanges(string token)
        {
            string pageToken = token;
            while (pageToken != null)
            {
                var request = Service.Changes.List(pageToken);
                request.Spaces = "drive";
                request.Fields = "*";
                var changes = request.Execute();
                foreach (var change in changes.Changes)
                {
                    try
                    {
                        if (ProcessChange(change))
                            return true;
                    }
                    catch (Exception e)
                    {
                        Log(TraceLevel.Error, "An error has occurred trying to process change: " + e);
                        return false;
                    }
                }

                if (changes.NewStartPageToken != null)
                {
                    Database.SetValue(_changeTokenKey, changes.NewStartPageToken);
                }
                pageToken = changes.NextPageToken;
            }
            return false;
        }

        // return true to resynchronize root
        private bool ProcessChange(Change change)
        {
            if (change.Type == "file")
            {
                if ((change.File == null && change.Removed == true) || change.File.Trashed == true)
                {
                    var path = GetFullPath(change.FileId);
                    if (path == null)
                    {
                        if (change.File != null)
                        {
                            Log(TraceLevel.Warning, "Trashed file '" + change.File.Name + "' was not found and cannot be deleted.");
                        }
                        else
                        {
                            Log(TraceLevel.Warning, "Trashed file with id '" + change.FileId + "' was not found and cannot be deleted.");
                        }
                        return false;
                    }

                    Database.DeleteFile(change.FileId);
                    IOUtilities.FileDelete(path, true);
                    if (change.File != null)
                    {
                        Log(TraceLevel.Info, "File '" + change.File.Name + "' was deleted.");
                    }
                    else
                    {
                        Log(TraceLevel.Info, "File with id '" + change.FileId + "' was deleted.");
                    }
                    return false;
                }

                if (change.File == null)
                {
                    Log(TraceLevel.Warning, "Don't know what to do with a change without an associated file.");
                    return false;
                }

                if (change.File.Name != null && change.File.Name.EndsWith(FileSystem.UrlExt))
                {
                    Log(TraceLevel.Warning, "File '" + change.File.Name + "' is a .url, skipped.");
                    return false;
                }

                var driveFile = DriveFile.From(change.File);
                if (driveFile == null || string.IsNullOrEmpty(driveFile.Id))
                {
                    Log(TraceLevel.Warning, "File '" + change.File.Name + "' is empty or its id is invalid.");
                    return false;
                }

                if (driveFile.ParentId == null)
                {
                    Log(TraceLevel.Warning, "File '" + change.File.Name + "' has no parent id.");
                    return false;
                }

                var parentPath = GetRelativePath(driveFile.ParentId);
                if (parentPath == null)
                {
                    Log(TraceLevel.Warning, "File '" + change.File.Name + "' with parent id '" + driveFile.ParentId + "' has no parent path.");
                    return false;
                }

                var relativePath = Path.Combine(parentPath, driveFile.FileName);
                Database.SynchronizeOne(relativePath, driveFile);
                Log(TraceLevel.Info, "File '" + change.File.Name + "' was synchronized.");
                return false;
            }
            return false;
        }

        private static string GetFileFields() => "id, parents, name, mimeType, size, version, modifiedTime, createdTime, folderColorRgb, owners, lastModifyingUser, webViewLink, webContentLink";

        // build a dictionary of <relative path> => file
        private IReadOnlyDictionary<string, DriveFile> LoadAllFiles(string rootId)
        {
            var files = new Dictionary<string, DriveFile>();
            string pageToken = null;
            do
            {
                var request = Service.Files.List();

                // fields are described here
                // https://developers.google.com/drive/api/v3/reference/files
                request.Fields = "nextPageToken, files(" + GetFileFields() + ")";
                request.PageToken = pageToken;

                // in this implementation, we don't want deleted files
                request.Q = "trashed=false";
                request.Spaces = "drive";

                var result = request.Execute();
                foreach (var driveFile in result.Files)
                {
                    var file = DriveFile.From(driveFile);
                    if (file != null)
                    {
                        //var filePath = Path.Combine(path, file.FileName);
                        //files[filePath] = file;
                        files[driveFile.Id] = file;
                    }
                }

                pageToken = result.NextPageToken;
            } while (pageToken != null);

            var dic = new Dictionary<string, DriveFile>(files.Count);
            foreach (var kv in files)
            {
                var path = buildPath(kv.Value);
                if (path == null)
                    continue; // huh?

                dic[path] = kv.Value;
            }

            string buildPath(DriveFile file)
            {
                if (file.ParentId == rootId)
                    return file.FileName;

                files.TryGetValue(file.ParentId, out var parent);
                if (parent == null)
                    return null;

                var parentPath = buildPath(parent);
                if (parentPath == null)
                    return null;

                return Path.Combine(parentPath, file.FileName);
            }
            return dic;
        }

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

        public static string GetTokenFilePath(string emailAddress) => Path.Combine(Settings.ConfigurationDirectoryPath, GetUserFileName(emailAddress) + _tokenJsonExt);
        public static string GetDatabaseFilePath(string emailAddress) => Path.Combine(Settings.ConfigurationDirectoryPath, GetUserFileName(emailAddress) + ".db");
        public static string GetDataDirectoryPath(string emailAddress) => Path.Combine(Settings.DataDirectoryPath, GetUserFileName(emailAddress));
        public static string GetUserFileName(string emailAddress)
        {
            if (emailAddress == null)
                throw new ArgumentNullException(nameof(emailAddress));

            return IOUtilities.PathToValidFileName(emailAddress);
        }

        // does the directory name matches a valid account?
        public static bool IsDirectoryAnAccount(string directoryName) => GetAllAccounts(true).Any(a => a.UserFileName.EqualsIgnoreCase(directoryName));

        public static Account GetAccount(string emailAddress) => GetAllAccounts(true).FirstOrDefault(a => a.UserEmailAddress.EqualsIgnoreCase(emailAddress));

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
        public void Remove(bool deleteData = true)
        {
            var tokenFilePath = GetTokenFilePath(UserEmailAddress);
            IOUtilities.FileDelete(tokenFilePath, true);

            if (deleteData)
            {
                Database?.Dispose();

                var databaseFilePath = GetDatabaseFilePath(UserEmailAddress);
                IOUtilities.FileDelete(databaseFilePath, true);

                var dataDirectoryPath = GetDataDirectoryPath(UserEmailAddress);
                IOUtilities.DirectoryDelete(dataDirectoryPath, true);
            }
        }

        // adds an account to app local data directory
        // uses Google Sign-In (pops up a form)
        public static About AddAccount()
        {
            if (!Settings.HasSecretsFile)
                throw new InvalidOperationException();

            // we want read & write access
            var scopes = new[] { DriveService.Scope.Drive };
            UserCredential credential;
            var tempPath = Path.Combine(Settings.ConfigurationDirectoryPath, "temp" + _tokenJsonExt);
            using (var stream = System.IO.File.OpenRead(Settings.SecretsFilePath))
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
                System.IO.File.WriteAllText(TokenFilePath, serialized);
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
                        var serialized = System.IO.File.ReadAllText(TokenFilePath);
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
