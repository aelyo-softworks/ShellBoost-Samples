using System;
using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.Overview
{
    public class SimpleFolder : ShellFolder
    {
        public SimpleFolder(ShellFolder parent, string name, int level)
            : base(parent, new StringKeyShellItemId(name))
        {
            Level = level;
        }

        public int Level { get; }
        public override IEnumerable<ShellItem> EnumItems(SHCONTF options) => EnumItems(this, options);

        public static IEnumerable<ShellItem> EnumItems(ShellFolder folder, SHCONTF options)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            int level;
            if (folder is SimpleFolder sf)
            {
                level = sf.Level + 1;
            }
            else
            {
                level = 0;
            }

            // add folders
            // only add folders up to two levels
            if (options.HasFlag(SHCONTF.SHCONTF_FOLDERS) && level < 3)
            {
                var max = 2;
                for (int i = 0; i < max; i++)
                {
                    yield return new SimpleFolder(folder, "Virtual Folder " + level + "." + i, level);
                }
            }

            // add items
            if (options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
            {
                var max = 2;
                for (int i = 0; i < max; i++)
                {
                    yield return new SimpleItem(folder, "Virtual Item #" + i + ".txt");
                }
            }
        }
    }
}
