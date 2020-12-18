using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.Overview
{
    public class SimpleFolder : ShellFolder
    {
        public SimpleFolder(SimpleFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            Level = parent.Level + 1;
        }

        // constructor reserved for root
        public SimpleFolder(ShellItemIdList idList)
            : base(idList)
        {
            // level = 0
        }

        public new OverviewShellFolderServer FolderServer => (OverviewShellFolderServer)base.FolderServer;
        public int Level { get; }
        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // add folders
            // only add folders up to two levels
            if (options.HasFlag(SHCONTF.SHCONTF_FOLDERS) && Level < 3)
            {
                var max = 2;
                for (int i = 0; i < max; i++)
                {
                    yield return new SimpleFolder(this, "Virtual Folder " + Level + "." + i);
                }
            }

            // add items
            if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
            {
                var max = 2;
                for (int i = 0; i < max; i++)
                {
                    yield return new SimpleItem(this, "Virtual Item #" + i + ".txt");
                }
            }
        }
    }
}
