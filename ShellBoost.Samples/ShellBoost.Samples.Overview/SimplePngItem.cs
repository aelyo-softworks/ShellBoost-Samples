using System;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.Overview
{
    public class SimplePngItem : ShellItem
    {
        public SimplePngItem(SimpleFolder parent, string text, ulong key)
            : base(parent, new StringKeyShellItemId(text))
        {
            // this is needed for icon
            ItemType = IOUtilities.PathGetExtension(text);

            Thumbnail = new SimpleThumbnail(this, key);
        }

        public new SimpleThumbnail Thumbnail { get => (SimpleThumbnail)base.Thumbnail; set => base.Thumbnail = value; }

        public void ClearCache()
        {
            // note this can fail with a sharing violation if the files are accessed (by the Shell itself or something else)
            Thumbnail.DeleteCache(96, 96);
            Thumbnail.DeleteCache(256, 256);
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // https://docs.microsoft.com/en-us/windows/win32/properties/props-system-thumbnailcacheid
            // NOTE: cache id cannot be 0
            if (key == Core.WindowsPropertySystem.System.ThumbnailCacheId)
            {
                value = Thumbnail.Key;
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }

        // we use a ShellBoost utility class to download (and cache) the file
        public override ShellContent GetContent() => new WebCacheShellContent(
                Parent.Root.FolderServer.Configuration.GetDefaultWebFileCache(),
                SimpleThumbnail.GetUrl(256, 256, Thumbnail.Key),
                new WebFileCacheRequestOptions { DontForceServerCheck = true }
            );
    }

    public class SimpleThumbnail : ShellThumbnail
    {
        public SimpleThumbnail(SimplePngItem item, ulong key)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            Item = item;
            Key = key;
        }

        public ulong Key { get; }
        public SimplePngItem Item { get; }

        // This example uses a well-known Windows icon that represents a generic image.
        public override ShellThumbnailAsIcon GetAsIcon(GILIN inOptions) => new ShellThumbnailAsIcon("shell32.dll") { Index = 325 };

        // don't get surprised if you see cute cats
        // check the doc on https://loremflickr.com/
        public static string GetUrl(int width, int height, ulong key) => "https://loremflickr.com/" + width + "/" + height + "?lock=" + key;

        public string GetCachePath(int width, int height)
        {
            var url = GetUrl(width, height, Key);

            // we use a ShellBoost utility class to download (and cache) the file
            var cache = Item.Parent.FolderServer.Configuration.GetDefaultWebFileCache();
            return cache.Download(url, new WebFileCacheRequestOptions { DontForceServerCheck = true });
        }

        public bool DeleteCache(int width, int height)
        {
            var url = GetUrl(width, height, Key);
            var cache = Item.Parent.FolderServer.Configuration.GetDefaultWebFileCache();
            return cache.Delete(url);
        }

        public override ShellThumbnailAsImage GetAsImage(int width, int height)
        {
            var path = GetCachePath(width, height);
            return path != null ? new ShellThumbnailAsImage(path) : null;
        }
    }
}
