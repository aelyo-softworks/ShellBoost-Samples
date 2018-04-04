using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ShellBoost.Samples.WebFolder.Api
{
    // check ShellBoost.Samples.WebFolderSite's project DriveController.cs file
    // for more information on public json API
    public static class WebFolderApi
    {
        // TODO: change this if you run the WebFolderSite project on another url
        public const string ApiBaseUrl = "http://localhost:60311/api/drive/";

        public static async Task<Item> GetRootFolderAsync() => await GetItemAsync(Guid.Empty);
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

        public static async Task<string> DownloadToFileAsync(Guid id, string contentETag)
        {
            using (var client = new HttpClient())
            {
                string url = ApiBaseUrl + id + "/content";
                if (!string.IsNullOrWhiteSpace(contentETag))
                {
                    url += "?contentETag=" + contentETag;
                }

                using (var stream = await client.GetStreamAsync(url).ConfigureAwait(false))
                {
                }
            }
        }
    }
}
