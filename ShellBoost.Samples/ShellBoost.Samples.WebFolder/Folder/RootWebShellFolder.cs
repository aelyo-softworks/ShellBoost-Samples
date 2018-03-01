using System;
using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.WebFolder.Folder
{
    // this represents the root of the drive hierarchy
    public class RootWebShellFolder : RootShellFolder
    {
        public RootWebShellFolder(WebShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public WebShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // our sample drive root's id always Guid.Empty
            foreach (var item in WebShellFolder.EnumItems(Guid.Empty, options.HasFlag(SHCONTF.SHCONTF_FOLDERS), options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS)))
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
    }
}
