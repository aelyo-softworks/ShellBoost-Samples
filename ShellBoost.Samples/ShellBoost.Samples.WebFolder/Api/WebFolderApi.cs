using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.WebFolder.Api
{
    // check ShellBoost.Samples.WebFolderSite's project DriveController.cs file
    // for more information on public json API
    public static class WebFolderApi
    {
        // TODO: change this if you run the WebFolderSite project on another url
        public const string ApiBaseUrl = "http://localhost:60311/api/drive/";

        private static Lazy<string> _cacheDirectoryPath = new Lazy<string>(() => Path.GetFullPath("cache"), true);
        public static string CacheDirectoryPath => _cacheDirectoryPath.Value;

        public static async Task<Item> GetRootFolderAsync() => await GetItemAsync(Guid.Empty).ConfigureAwait(false);
        public static async Task<Item> GetItemAsync(Guid id)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(ApiBaseUrl + id).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Item>(json);
            }
        }

        public static async Task<Item[]> GetChildrenAsync(Guid id)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(ApiBaseUrl + id + "/children").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Item[]>(json);
            }
        }

        public static async Task<Item[]> GetFoldersAsync(Guid id)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(ApiBaseUrl + id + "/folders").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Item[]>(json);
            }
        }

        public static async Task<Item[]> GetItemsAsync(Guid id)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(ApiBaseUrl + id + "/items").ConfigureAwait(false);
                return JsonConvert.DeserializeObject<Item[]>(json);
            }
        }

        public static async Task<string> DownloadContentToFileAsync(Guid id, string contentETag) => await DownloadContentToFileAsync(id, contentETag, CancellationToken.None).ConfigureAwait(false);
        public static async Task<string> DownloadContentToFileAsync(Guid id, string contentETag, CancellationToken cancellationToken)
        {
            if (contentETag == null)
                throw new ArgumentNullException(nameof(contentETag));

            // use long file names
            string path = @"\\?\" + Path.Combine(CacheDirectoryPath, id.ToString("N"), contentETag);
            if (IOUtilities.FileExists(path))
                return path;

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(ApiBaseUrl + id + "/content").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                IOUtilities.FileCreateDirectory(path);
                using (var file = File.OpenWrite(path))
                {
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await stream.CopyToAsync(file, 81920, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // delete old tags
            await Task.Run(() =>
            {
                string dir = Path.GetDirectoryName(path);
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.EnumerateFiles(dir).Where(f => !f.EqualsIgnoreCase(path)))
                    {
                        IOUtilities.FileDelete(file);
                    }
                }
            }).ConfigureAwait(false);
            return path;
        }
    }
}
