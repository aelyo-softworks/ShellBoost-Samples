using System.IO;
using ShellBoost.Core;
using ShellBoost.Samples.WebFolder.Api;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class WebShellItem : ShellItem
    {
        public WebShellItem(ShellFolder parent, Item item)
            : base(parent, new GuidKeyShellItemId(item.Id))
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            Item = item;
            DisplayName = item.Name;
            ItemType = Path.GetExtension(item.Name);
            Size = item.Length;
            DateModified = item.LastWriteTimeUtc.ToLocalTime();
            DateCreated = item.CreationTimeUtc.ToLocalTime();
        }

        public Item Item { get; }
    }
}
