using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using ShellBoost.Core;
using ShellBoost.Core.Handlers;
using ShellBoost.Core.Synchronization;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderSync
{
    [Guid("7503e3a5-21e3-43de-830b-6b87e4b98552")] // TODO: don't use this guid! create a new one!
    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public class CloudFolderThumbnailProvider : ThumbnailProvider
    {
        private readonly OnDemandLocalFileSystem _fileSystem;
        private readonly CloudFolderFileSystem _cloudFileSystem;

        // this will be called by our ComClassFactory
        public CloudFolderThumbnailProvider(OnDemandLocalFileSystem fileSystem, CloudFolderFileSystem cloudFileSystem)
        {
            _fileSystem = fileSystem;
            _cloudFileSystem = cloudFileSystem;
        }

        public static void Register() => new CloudFolderThumbnailProvider(null, null).RegisterOutOfProcess(RegistrationMode.User, Process.GetCurrentProcess().MainModule.FileName);
        public static void Unregister() => new CloudFolderThumbnailProvider(null, null).Unregister(RegistrationMode.User);

        protected override Bitmap GetThumbnailBitmap(int width)
        {
            if (_fileSystem == null || _cloudFileSystem == null) // wrong context
                return null;

            var ep = _cloudFileSystem.EndPointSynchronizer;
            if (ep == null)
                return null;

            if (Item == null) // in this Cloud Folder context, we should have an Item (a native IShellItem)
                return null;

            ep.MultiPointSynchronizer?.Log(TraceLevel.Info, "Item:" + Item.SIGDN_FILESYSPATH + " width: " + width);

            // get relative path from passed item
            var relativePath = _fileSystem.GetRelativePathFromFullPath(Item.SIGDN_FILESYSPATH);

            if (!WebApi.IsSupportedThumbnailFile(Path.GetExtension(relativePath)))
                return null;

            // get cloud id from cloud fs
            var entry = ep.StateProvider.GetEntryByFilePath(ep, relativePath);
            if (entry == null)
                return null; // entry is not yet available for some reason

            using (var ms = new MemoryStream())
            {
                WebApi.DownloadAsync(new WebItem { Id = CloudFolderFileSystem.ToId(entry.Id) }, ms, width).Wait();
                if (ms.Length == 0) // error?
                    return null;

                ms.Position = 0;
                return (Bitmap)Image.FromStream(ms);
            }
        }
    }
}
