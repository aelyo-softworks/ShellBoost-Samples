using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public static class WebApi
    {
        // TODO: update this if you change ShellBoost.Samples.CloudFolderSite's project url.
        public const string RootUrl = "https://localhost:5001/";

        public static DirectoryInfo LocalDirectory => GetLocalDirectory();
        public static DirectoryInfo RecycleBinDirectory => GetRecycleBinDirectory();
        public static ServerInfo ServerInfo => _serverInfo.Value;

        private static readonly HttpClient _client = new HttpClient();
        private static readonly Lazy<ServerInfo> _serverInfo = new Lazy<ServerInfo>(() => GetServerInfo().Result, true);
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private const string _baseUrl = RootUrl + "api/";

        // direct cache id => item
        private static readonly ConcurrentDictionary<Guid, WebItem> _itemCache = new ConcurrentDictionary<Guid, WebItem>();

        // cache id+request => items
        private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebItem>>> _itemsCache = new ConcurrentDictionary<Guid, ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebItem>>>();

        // cache item => referencing items
        private static readonly ConcurrentDictionary<Guid, Guid> _referencingCache = new ConcurrentDictionary<Guid, Guid>();

        public static void ClearCache()
        {
            _itemCache.Clear();
            _itemsCache.Clear();
            _referencingCache.Clear();
        }

        private static void RemoveFromCache(Guid id)
        {
            _itemCache.TryRemove(id, out _);
            _itemsCache.TryRemove(id, out _);
            if (_referencingCache.TryRemove(id, out var referencing))
            {
                if (_itemsCache.TryGetValue(referencing, out var items))
                {
                    foreach (var kv in items)
                    {
                        if (kv.Value.ContainsKey(id))
                        {
                            _itemsCache.TryRemove(referencing, out _);
                        }
                    }
                }
            }
        }

        // invalidate cache using events from the server
        public static void UpdateCache(Guid id, Guid parentId, WatcherChangeTypes types)
        {
            switch (types)
            {
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                    RemoveFromCache(id);
                    break;

                case WatcherChangeTypes.Created:
                    RemoveFromCache(parentId);
                    break;
            }
        }

        private static void AddToCache(Guid id, WebItem value) => _itemCache[id] = value;
        private static void AddToCache(Guid id, string key, ConcurrentDictionary<Guid, WebItem> items)
        {
            if (!_itemsCache.TryGetValue(id, out var dic))
            {
                dic = new ConcurrentDictionary<string, ConcurrentDictionary<Guid, WebItem>>();
                dic = _itemsCache.AddOrUpdate(id, dic, (k, o) => o);
            }
            dic[key] = items;

            // add referencing items
            foreach (var item in items)
            {
                _referencingCache[item.Key] = id;
            }
        }

        private static bool TryGetFromCache(Guid id, out WebItem value) => _itemCache.TryGetValue(id, out value);
        private static bool TryGetFromCache(Guid id, string key, out ConcurrentDictionary<Guid, WebItem> items)
        {
            if (!_itemsCache.TryGetValue(id, out var dic))
            {
                items = null;
                return false;
            }

            return dic.TryGetValue(key, out items);
        }

        // standard HTTP calls
        // note we don't support any auth here
        private static async Task<T> ApiPostAsync<T>(string url, string path, string json = null, bool throwOnError = true)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (path == null)
                return await ApiPostAsync<T>(url, (Stream)null, json, throwOnError).ConfigureAwait(false);

            // if the file is locked, still create it empty, it will be updated later on
            return await IOUtilities.WrapSharingViolationsAsync(async () =>
            {
                if (IOUtilities.PathIsDirectory(path))
                    return await ApiPostAsync<T>(url, (Stream)null, json, throwOnError).ConfigureAwait(false);

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return await ApiPostAsync<T>(url, stream, json, throwOnError).ConfigureAwait(false);
                }
            }, (IOException e, int r, int m, int w, out Task<T> i) =>
            {
                i = ApiPostAsync<T>(url, (Stream)null, json, throwOnError);
                return Task.FromResult(true);
            }).ConfigureAwait(false);
        }

        private static Task<T> ApiPostAsync<T>(string url, Stream stream, string json = null, bool throwOnError = true)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (throwOnError)
                return post();

            try
            {
                return post();
            }
            catch (Exception e)
            {
                WebShellFolderServer.Current.Log(TraceLevel.Error, "An error has occurred trying to call POST on web api: " + e.Message);
                return default;
            }

            async Task<T> post()
            {
                var content = new MultipartFormDataContent();
                // put json first
                if (!string.IsNullOrWhiteSpace(json))
                {
                    content.Add(new StringContent(json, Encoding.UTF8, "application/json"));
                }

                // stream can be null for directory creation
                if (stream != null)
                {
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.Add("Content-Type", "application/octet-stream");
                    content.Add(streamContent);
                }

                WebShellFolderServer.Current.Log(TraceLevel.Verbose, "PostAsync " + _baseUrl + url + Environment.NewLine + json);
                var msg = await _client.PostAsync(_baseUrl + url, content).ConfigureAwait(false);
                var responseJson = await msg.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<T>(responseJson, _serializerOptions).ConfigureAwait(false);
            }
        }

        private static async Task<ServerInfo> GetServerInfo()
        {
            try
            {
                var json = await _client.GetStreamAsync(_baseUrl + "info").ConfigureAwait(false);
                var info = await JsonSerializer.DeserializeAsync<ServerInfo>(json, _serializerOptions).ConfigureAwait(false);

                // make sure client & server are in-sync
                if (info.TimeDifference.TotalMinutes > 1)
                    throw new Exception("There is a too much of a time difference between the client and server.");

                return info;
            }
            catch (Exception e)
            {
                throw new Exception("The server at " + _baseUrl + " cannot be reached. Please start the ShellBoost.Samples.CloudFolderSite project before running this sample.", e);
            }
        }

        private static Task<T> ApiGetAsync<T>(string url, bool throwOnError = true)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (throwOnError)
                return get();

            try
            {
                return get();
            }
            catch (Exception e)
            {
                WebShellFolderServer.Current.Log(TraceLevel.Error, "An error has occurred trying to call GET on web api: " + e.Message);
                return default;
            }

            async Task<T> get()
            {
                WebShellFolderServer.Current.Log(TraceLevel.Verbose, "GetStreamAsync " + _baseUrl + url);
                var json = await _client.GetStreamAsync(_baseUrl + url).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<T>(json, _serializerOptions).ConfigureAwait(false);
            }
        }

        // this is %localappdata%\CloudFolder or C:\Users\<username>\AppData\Local\CloudFolder
        private static string GetRootFolderPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CloudFolder");

        private static DirectoryInfo GetLocalDirectory()
        {
            var hiddenPath = Path.Combine(GetRootFolderPath(), _serverInfo.Value.Id.ToString("N"));
            var path = Path.Combine(hiddenPath, "Files");
            var cacheDirectory = new DirectoryInfo(path);
            if (!cacheDirectory.Exists)
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    // do nothing
                }
                cacheDirectory.Refresh();
            }

            // we don't want the user or apps to go in there mess with cached/local files, so we associate it with this virtual folder
            // note the lower "Files" directory stays normal
            var CLSID_MoveToChooseFolder = new Guid("a0202464-b4b4-4b85-9628-ccd46df16942");
            var s = new ShellFolderCustomSettings();
            s.Clsid = CLSID_MoveToChooseFolder;
            s.Set(hiddenPath);

            return cacheDirectory;
        }

        // This is were we store file for the recycle bin.
        // We can't recycle frol somewhere the Shell can't access physical files
        private static DirectoryInfo GetRecycleBinDirectory()
        {
            var path = Path.Combine(GetRootFolderPath(), "RecycleBin");
            var cacheDirectory = new DirectoryInfo(path);
            if (!cacheDirectory.Exists)
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch
                {
                    // do nothing
                }
                cacheDirectory.Refresh();
            }
            return cacheDirectory;
        }

        public static Task<WebItem> CreateAsync(Guid parentId, string filePath = null, string name = null, FileAttributes attributes = FileAttributes.Normal)
        {
            name ??= Path.GetFileName(filePath);
            var item = new UpdateRequest();
            item.ParentId = parentId;
            item.Name = name;
            item.Attributes = attributes;

            if (filePath != null)
            {
                var fi = new FileInfo(filePath);
                if (fi.Exists)
                {
                    item.CreationTimeUtc = fi.CreationTimeUtc;
                    item.LastAccessTimeUtc = fi.LastAccessTimeUtc;
                    item.LastWriteTimeUtc = fi.LastWriteTimeUtc;
                }
            }

            var json = JsonSerializer.Serialize(item);
            return ApiPostAsync<WebItem>("upload", filePath, json);
        }

        public static async Task DownloadAsync(this WebItem item, Stream outputStream)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            WebShellFolderServer.Current.Log(TraceLevel.Verbose, "GetStreamAsync " + _baseUrl + "download/" + item.Id);
            var stream = await _client.GetStreamAsync(_baseUrl + "download/" + item.Id).ConfigureAwait(false);
            if (stream != null)
            {
                await stream.CopyToAsync(outputStream).ConfigureAwait(false);
            }
        }

        public static async Task DownloadAsync(this WebItem item, string filePath)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            await IOUtilities.WrapSharingViolationsAsync(async () =>
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    await DownloadAsync(item, stream).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        public static Task<WebItem> UploadAsync(this WebItem item, Stream stream)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // stream can be null if we want only to update metadata
            if (item.Name == null)
                throw new ArgumentException(null, nameof(item));

            var request = new UpdateRequest();
            request.Id = item.Id;
            request.ParentId = item.ParentId;
            request.Name = item.Name;
            request.Attributes = item.Attributes;

            // note we don' use ms
            request.LastAccessTimeUtc = item.LastAccessTimeUtc.RemoveMilliseconds();
            request.LastWriteTimeUtc = item.LastWriteTimeUtc.RemoveMilliseconds();
            request.CreationTimeUtc = item.CreationTimeUtc.RemoveMilliseconds();

            var json = JsonSerializer.Serialize(request);
            return ApiPostAsync<WebItem>("upload", stream, json);
        }

        public static WebItem GetChild(WebItem parent, string name) => EnumerateChildren(parent).FirstOrDefault(i => i.Name.EqualsIgnoreCase(name));

        public static async Task<bool> RefreshAsync(this WebItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var refresh = await ApiGetAsync<WebItem>("get/" + item.Id, false).ConfigureAwait(false);
            if (refresh == null)
                return false;

            item.Name = refresh.Name;
            item.Attributes = refresh.Attributes;
            item.LastAccessTimeUtc = refresh.LastWriteTimeUtc;
            item.CreationTimeUtc = refresh.CreationTimeUtc;
            item.LastWriteTimeUtc = refresh.LastWriteTimeUtc;
            item.Length = refresh.Length;
            item.IsRoot = refresh.IsRoot;
            return true;
        }

        public static async Task<bool> DeleteAsync(this WebItem item, string path, DeleteOptions options)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var result = await ApiGetAsync<bool>("delete/" + item.Id + "/" + options, false);
            if (result)
            {
                IOUtilities.PathDelete(path, true, false);
            }
            return result;
        }

        public static string Recycle(this WebItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var newName = Conversions.GetNewFileName(item.Name, (s) =>
            {
                var path = Path.Combine(RecycleBinDirectory.FullName, s);
                return IOUtilities.PathExists(path);
            });
            
            var newPath = Path.Combine(RecycleBinDirectory.FullName, newName);
            item.EnsureLocalAsync(newPath, true).Wait();
            return newPath;
        }

        public static Task<WebItem> MoveAsync(this WebItem item, Guid newParentId, MoveOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return ApiGetAsync<WebItem>("move/" + item.Id + "/" + newParentId + "/" + options);
        }

        public static async Task<WebItem> RenameAsync(this WebItem item, string newName, string oldPath, RenameOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (oldPath == null)
                throw new ArgumentNullException(nameof(oldPath));

            if (string.IsNullOrWhiteSpace(newName))
                return null;

            var newItem = await ApiGetAsync<WebItem>("rename/" + item.Id + "/" + newName + "/" + options);
            if (newItem != null)
            {
                // delete old path
                IOUtilities.PathDelete(oldPath, true, false);
            }
            return newItem;
        }

        public static async Task<string> EnsureLocalAsync(this WebItem item, string path, bool recursive = false)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (item.IsRoot) // do nothing to root
                return path;

            if (item.IsFolder)
            {
                // ensure directory exists
                IOUtilities.FileCreateDirectory(Path.Combine(path, "dummy"));
                if (recursive)
                {
                    foreach (var child in item.EnumerateChildren())
                    {
                        await EnsureLocalAsync(child, Path.Combine(path, child.Name), recursive).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                // we could add other checks if server supported it (serial number, version, etc.)
                // but that would need to be added to the local file as well (using alternate streams, or a local database, etc.)
                var file = new FileInfo(path);
                var download = !file.Exists || file.LastWriteTimeUtc < item.LastWriteTimeUtc;
                if (!download)
                    return path;

                // ensure directory exists
                IOUtilities.FileCreateDirectory(path);
                await DownloadAsync(item, path).ConfigureAwait(false);
            }

            IOUtilities.PathSetCreationTimeUtc(path, item.CreationTimeUtc);
            IOUtilities.PathSetLastAccessTimeUtc(path, item.LastAccessTimeUtc);
            IOUtilities.PathSetLastWriteTimeUtc(path, item.LastWriteTimeUtc);
            return path;
        }

        public static string GetNewName(this WebItem parent, string tentativeName)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            // come up with some unique name but that still looks like the original name
            return Conversions.GetNewFileName(tentativeName, (s) =>
            {
                return EnumerateChildren(parent).Any(i => i.Name.EqualsIgnoreCase(s));
            });
        }

        public static IEnumerable<WebItem> EnumerateChildren(this WebItem parent, SHCONTF options = SHCONTF.SHCONTF_NONFOLDERS | SHCONTF.SHCONTF_FOLDERS)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            string apiSuffix;
            if (options.HasFlag(SHCONTF.SHCONTF_FOLDERS))
            {
                if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
                {
                    apiSuffix = "all";
                }
                else
                {
                    apiSuffix = "folders";
                }
            }
            else
            {
                if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
                {
                    apiSuffix = "files";
                }
                else
                    return Enumerable.Empty<WebItem>();
            }

            var enumOptions = new EnumerateOptions();
            if (options.HasFlag(SHCONTF.SHCONTF_INCLUDEHIDDEN))
            {
                enumOptions.IncludeHidden = true;
            }

            var key = "get/" + parent.Id + "/" + apiSuffix + "/" + enumOptions;
            if (TryGetFromCache(parent.Id, key, out var items))
                return items.Select(i => i.Value);

            items = new ConcurrentDictionary<Guid, WebItem>();
            foreach (var item in ApiGetAsync<WebItem[]>(key).Result)
            {
                items[item.Id] = item;
            }
            AddToCache(parent.Id, key, items);
            return items.Select(i => i.Value);
        }

        public static WebItem GetFromCache(Guid id)
        {
            TryGetFromCache(id, out var item);
            return item;
        }

        public static async Task<WebItem> GetAsync(Guid id)
        {
            if (id == Guid.Empty)
                throw new InvalidOperationException();

            if (TryGetFromCache(id, out var item))
                return item;

            item = await ApiGetAsync<WebItem>("get/" + id, false).ConfigureAwait(false);
            AddToCache(id, item);
            return item;
        }
    }
}
