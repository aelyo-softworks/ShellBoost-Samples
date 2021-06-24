using System.Collections.Generic;
using System.Linq;
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
            // Level = 0
        }

        public new OverviewShellFolderServer FolderServer => (OverviewShellFolderServer)base.FolderServer;

        public int Level { get; }

        // set this to true to see images (of cute cats)
        public bool ShowImages => true;

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // add folders
            // note in this sample we only add folders up to two levels
            var maxLevels = 2;
            if (options.HasFlag(SHCONTF.SHCONTF_FOLDERS) && Level <= maxLevels)
            {
                var maxFolders = 2;
                for (var i = 0; i < maxFolders; i++)
                {
                    yield return new SimpleFolder(this, "Virtual Folder " + Level + "." + i);
                }
            }

            // add items
            if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
            {
                var maxItems = 2;
                if (ShowImages)
                {
                    maxItems *= 2;
                    var i = 0;
                    for (; i < maxItems / 2; i++)
                    {
                        yield return new SimpleItem(this, "Virtual Item #" + i + ".txt");
                    }

                    for (; i < maxItems; i++)
                    {
                        var imgKey = (ulong)(Level * 10 + i);
                        yield return new SimplePngItem(this, "Virtual Image Key#" + imgKey + ".png", imgKey);
                    }
                }
                else
                {
                    for (var i = 0; i < maxItems; i++)
                    {
                        yield return new SimpleItem(this, "Virtual Item #" + i + ".txt");
                    }
                }
            }
        }

        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            // only with selected items
            if (items.OfType<SimplePngItem>().Count() > 0)
            {
                var clearLocal = new ShellMenuItem(appendMenu, "Clear Cache");
                clearLocal.Invoke += (s, e) =>
                {
                    foreach (var item in e.Items.OfType<SimplePngItem>())
                    {
                        item.ClearCache();
                    }
                };

                appendMenu.Items.Add(clearLocal);
            }
        }
    }
}
