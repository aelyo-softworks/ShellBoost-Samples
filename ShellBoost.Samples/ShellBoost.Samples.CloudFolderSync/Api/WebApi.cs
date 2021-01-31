using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.CloudFolder.Api
{
    public static class WebApi
    {
        // TODO: update this if you change ShellBoost.Samples.CloudFolderSite's project url.
        public const string RootUrl = "https://localhost:5001/";

        public static ILogger Logger { get; set; }
        public static ServerInfo ServerInfo => _serverInfo.Value;

        private static readonly HttpClient _client = new HttpClient();
        private static readonly Lazy<ServerInfo> _serverInfo = new Lazy<ServerInfo>(() => GetServerInfo().Result, true);
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        private const string _baseUrl = RootUrl + "api/";

        // standard HTTP calls
        // note we don't support any auth here
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
                Logger?.Log(TraceLevel.Error, "An error has occurred trying to call POST on web api: " + e.Message);
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

                Logger?.Log(TraceLevel.Verbose, "PostAsync " + _baseUrl + url + Environment.NewLine + json);
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
                Logger?.Log(TraceLevel.Error, "An error has occurred trying to call GET on web api: " + e.Message);
                return default;
            }

            async Task<T> get()
            {
                Logger?.Log(TraceLevel.Verbose, "GetStreamAsync " + _baseUrl + url);
                var json = await _client.GetStreamAsync(_baseUrl + url).ConfigureAwait(false);
                return await JsonSerializer.DeserializeAsync<T>(json, _serializerOptions).ConfigureAwait(false);
            }
        }

        public static Task<WebItem> CreateAsync(Guid parentId, string name, FileAttributes attributes = FileAttributes.Normal)
        {
            var item = new UpdateRequest();
            item.ParentId = parentId;
            item.Name = name;
            item.Attributes = attributes;

            var json = JsonSerializer.Serialize(item);
            return ApiPostAsync<WebItem>("upload", (Stream)null, json);
        }

        public static async Task DownloadAsync(this WebItem item, Stream outputStream, SyncContext context = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            Logger?.Log(TraceLevel.Verbose, "GetStreamAsync " + _baseUrl + "download/" + item.Id);
            var resp = await _client.GetAsync(_baseUrl + "download/" + item.Id).ConfigureAwait(false);
            if (resp != null)
            {
                if (context?.ProgressSink != null && resp.Content.Headers.ContentLength.HasValue)
                {
                    using (var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        var completed = 0L;
                        var buffer = new byte[65536]; // below 85K (LOH)

                        do
                        {
                            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false);
                            if (read == 0)
                                break;

                            completed += read;
                            await outputStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                            context.ProgressSink.Progress(context, resp.Content.Headers.ContentLength.Value, completed);
                        }
                        while (true);
                    }
                }
                else
                {
                    await resp.Content.CopyToAsync(outputStream).ConfigureAwait(false);
                }
            }
        }

        public static Task<WebItem> UploadAsync(this WebItem item, Stream stream = null)
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

            // note we don't use ms
            request.LastAccessTimeUtc = item.LastAccessTimeUtc.RemoveMilliseconds();
            request.LastWriteTimeUtc = item.LastWriteTimeUtc.RemoveMilliseconds();
            request.CreationTimeUtc = item.CreationTimeUtc.RemoveMilliseconds();

            var json = JsonSerializer.Serialize(request);
            return ApiPostAsync<WebItem>("upload", stream, json);
        }

        public static Task<WebItem> GetChildAsync(Guid parentId, string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return ApiGetAsync<WebItem>("getchild/" + parentId + "/" + name, false);
        }

        public static WebItem GetChild(WebItem parent, string name) => EnumerateChildren(parent, new EnumerateOptions { IncludeHidden = true }).FirstOrDefault(i => i.Name.EqualsIgnoreCase(name));

        public static Task<WebItem> GetAsync(Guid id) => ApiGetAsync<WebItem>("get/" + id, false);

        public static Task<bool> DeleteAsync(this WebItem item, DeleteOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return ApiGetAsync<bool>("delete/" + item.Id + "/" + options, false);
        }

        public static Task<WebItem> MoveAsync(this WebItem item, Guid newParentId, MoveOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            return ApiGetAsync<WebItem>("move/" + item.Id + "/" + newParentId + "/" + options);
        }

        public static Task<WebItem> RenameAsync(this WebItem item, string newName, RenameOptions options = null)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(newName))
                return Task.FromResult<WebItem>(null);

            return ApiGetAsync<WebItem>("rename/" + item.Id + "/" + newName + "/" + options);
        }

        public static IEnumerable<WebChange> EnumerateChanges(DateTime startTime)
        {
            var url = "changes/" + startTime.ToUniversalTime().ToString("O");
            return ApiGetAsync<WebChange[]>(url).Result;
        }

        public static IReadOnlyList<WebItem> EnumerateChildren(this WebItem parent, EnumerateOptions options = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            string apiSuffix;
            if (options.IncludeFolders)
            {
                if (options.IncludeFiles)
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
                if (options.IncludeFiles)
                {
                    apiSuffix = "files";
                }
                else
                    return Array.Empty<WebItem>();
            }

            var url = "get/" + parent.Id + "/" + apiSuffix + "/" + options;
            return ApiGetAsync<WebItem[]>(url).Result;
        }
    }
}
