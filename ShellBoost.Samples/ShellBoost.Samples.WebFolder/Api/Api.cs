using System;
using System.Net;
using Newtonsoft.Json;

namespace ShellBoost.Samples.WebFolder.Api
{
    // check ShellBoost.Samples.WebFolderSite's project DriveController.cs file
    // for more information on public json API
    public static class Api
    {
        // TODO: change this if you run the WebFolderSite project on another url
        public const string ApiBaseUrl = "http://localhost:60311/api/drive/";

        public static Item GetRootFolder() => GetItem(Guid.Empty);
        public static Item GetItem(Guid id)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<Item>(client.DownloadString(ApiBaseUrl + id));
            }
        }

        public static Item[] GetChildren(Guid id)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<Item[]>(client.DownloadString(ApiBaseUrl + id + "/children"));
            }
        }

        public static Item[] GetFolders(Guid id)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<Item[]>(client.DownloadString(ApiBaseUrl + id + "/folders"));
            }
        }

        public static Item[] GetItems(Guid id)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<Item[]>(client.DownloadString(ApiBaseUrl + id + "/items"));
            }
        }
    }
}
