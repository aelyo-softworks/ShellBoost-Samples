using System;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolder
{
    public class WebShellItem : ShellItem, IObjectWithApiItem
    {
        public WebShellItem(WebShellFolder parent, WebItem apiItem)
            : base(parent, apiItem.Id)
        {
            ApiItem = apiItem;
            ItemType = IOUtilities.PathGetExtension(apiItem.Name);
            DisplayName = apiItem.Name;
            DateCreated = apiItem.CreationTimeUtc.ToLocalTime();
            DateModified = apiItem.LastWriteTimeUtc.ToLocalTime();
            DateAccessed = apiItem.LastAccessTimeUtc.ToLocalTime();
            Size = apiItem.Length;
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;

            // this is a "physical" shell item
            if (parent.IsRoot)
            {
                FileSystemPath = Path.Combine(WebApi.LocalDirectory.FullName, apiItem.Name);
            }
            else
            {
                FileSystemPath = Path.Combine(parent.FileSystemPath, apiItem.Name);
            }
        }

        public WebItem ApiItem { get; }
        public new WebShellFolder Parent => (WebShellFolder)base.Parent;

        private string EnsureLocal()
        {
            var existed = IOUtilities.PathExists(FileSystemPath);
            ApiItem.EnsureLocalAsync(FileSystemPath).Wait();
            if (existed != IOUtilities.PathExists(FileSystemPath))
            {
                Parent.NotifyUpdate();
            }
            return FileSystemPath;
        }

        // get uri is called before a file is opened
        public override Uri GetUri(out URI_CREATE_FLAGS flags)
        {
            EnsureLocal();
            return base.GetUri(out flags);
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // gives the value of our custom columns
            // note the Shell doesn't support extended types such as GUID
            // so we pass it as a string
            if (key == WebShellFolder.ServerIdPk)
            {
                value = ApiItem.Id.ToString();
                return true;
            }

            if (key == WebShellFolder.LocalStatusPk)
            {
                value = ApiItem.GetLocalStatus(FileSystemPath);
                return true;
            }

            // note for preview to work, the shell needs the local file to be there
            if (key == PropertyStore.IdentityName || key == PropertyStore.PreviewItem)
            {
                value = FileSystemPath;
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }
    }
}
