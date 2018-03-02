using System;
using System.Collections.Generic;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellFolder : ShellFolder
    {
        public LocalShellFolder(ShellFolder parent, DirectoryInfo info)
            : base(parent, info) // there is a specific overload for DirectoryInfo
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            Info = info;
            SetPropertyValue(Props.System.NewMenuAllowedTypes, ".txt");
            SetPropertyValue(Props.System.NewMenuPreferredTypes, ".txt");
        }

        public DirectoryInfo Info { get; }

        // we export this as internal so the roo folder shares this behavior
        internal static IEnumerable<FileSystemInfo> EnumerateFileSystemItems(DirectoryInfo info, string searchPattern)
        {
            // for demonstration purpose, we hide any file or directory that has "hidden" in its name
            foreach (var child in info.EnumerateFileSystemInfos(searchPattern))
            {
                if (child.Name.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                yield return child;
            }
        }

        protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(DirectoryInfo info, SHCONTF options, string searchPattern)
        {
            return EnumerateFileSystemItems(info, searchPattern);
        }

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info)
        {
            return new LocalShellFolder(this, info);
        }

        protected override ShellItem CreateFileSystemItem(FileInfo info)
        {
            // for demonstration purpose, we handle XML files like they were folder over their elements
            if (string.Compare(info.Extension, ".xml", StringComparison.OrdinalIgnoreCase) == 0)
                return new XmlLocalShellFolder(this, info);

            return new LocalShellItem(this, info);
        }

        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (appendMenu == null)
                throw new ArgumentNullException(nameof(appendMenu));

            appendMenu.Items.Add(new ShellMenuItem(appendMenu) { IsTopNew = true });
        }
    }
}
