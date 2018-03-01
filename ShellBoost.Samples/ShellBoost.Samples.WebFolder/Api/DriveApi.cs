using System;
using System.Net;
using Newtonsoft.Json;

namespace ShellBoost.Samples.WebFolder.Api
{
    public static class DriveApi
    {
        // TODO: change this if you run the WebFolderSite project on another url
        public const string ApiBaseUrl = "http://localhost:60311/api/drive";

        public static DriveItem GetRootFolder() => GetItem(Guid.Empty);
        public static DriveItem GetItem(Guid id)
        {
            using (var client = new WebClient())
            {
                return JsonConvert.DeserializeObject<DriveItem>(client.DownloadString(ApiBaseUrl));
            }
        }
    }
}
