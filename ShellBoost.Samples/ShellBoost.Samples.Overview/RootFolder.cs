using System;
using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.Overview
{
    public class RootFolder : RootShellFolder
    {
        public RootFolder(ShellItemIdList idList)
            : base(idList)
        {
        }

        public new OverviewShellFolderServer FolderServer => (OverviewShellFolderServer)base.FolderServer;
        public override IEnumerable<ShellItem> EnumItems(SHCONTF options) => SimpleFolder.EnumItems(this, options);
    }
}
