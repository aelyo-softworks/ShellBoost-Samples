using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.WebFolder.Api;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class WebShellFolder : ShellFolder
    {
        // we use a GuidKeyShellItemId for the IdList (it's an IDL wrapper on a Guid)
        public WebShellFolder(ShellFolder parent, Item folder)
            : base(parent, new GuidKeyShellItemId(folder.Id))
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            DateModified = folder.LastWriteTimeUtc.ToLocalTime();
            DateCreated = folder.CreationTimeUtc.ToLocalTime();
            Folder = folder;
            DisplayName = folder.Name;
        }

        public Item Folder { get; }

        public static async Task<Item[]> EnumItemsAsync(Guid itemId, bool includeFolders, bool includeItems)
        {
            if (includeFolders && includeItems)
                return await WebFolderApi.GetChildrenAsync(itemId);

            if (includeFolders)
                return await WebFolderApi.GetFoldersAsync(itemId);

            return await WebFolderApi.GetItemsAsync(itemId);
        }

        // same function used for all folders, including root folder
        public static Item[] EnumItems(Guid itemId, bool includeFolders, bool includeItems) =>
            Task.Run(async () =>
            {
                return await EnumItemsAsync(itemId, includeFolders, includeItems);
            }).Result;

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            foreach (var item in EnumItems(Folder.Id, options.HasFlag(SHCONTF.SHCONTF_FOLDERS), options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS)))
            {
                if (item.Type == Api.ItemType.Folder)
                {
                    yield return new WebShellFolder(this, item);
                }
                else
                {
                    yield return new WebShellItem(this, item);
                }
            }
        }

        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            // because we don't rely on file system, we won't have the "new" menu automatically set for us
            // so we must build one, from the standard one
            // fortunately, there is a helper for that
            appendMenu.MergeNewMenu();
        }
    }
}
