using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellFolder : ShellFolder
    {
        public LocalShellFolder(ShellFolder parent, DirectoryInfo info)
            : base(parent, info)
        {
            //CanCopy = true;
            //CanDelete = true;
            //CanLink = true;
            //CanMove = true;
            //CanPaste = true;
            //CanRename = true;
            Info = info;
            //DisplayName = info.Name;
            //ItemType = info.Extension;
            //DateModified = info.LastWriteTime;
            //DateCreated = info.CreationTime;
        }

        public DirectoryInfo Info { get; }

        // same function used for all folders, including root folder
        public static IEnumerable<FileSystemInfo> EnumFileSystemInfos(DirectoryInfo info, bool includeFolders, bool includeItems)
        {
            // for demonstration purpose, we filter any item with "hidden" in the name
            return RawEnumFileSystemInfos(info, includeFolders, includeItems).Where(i => i.Name.IndexOf("hidden", StringComparison.OrdinalIgnoreCase) < 0);
        }

        public static IEnumerable<FileSystemInfo> RawEnumFileSystemInfos(DirectoryInfo info, bool includeFolders, bool includeItems)
        {
            if (includeFolders && includeItems)
                return info.EnumerateFileSystemInfos();

            if (includeFolders)
                return info.EnumerateDirectories();

            return info.EnumerateFiles();
        }

        public static IEnumerable<ShellItem> EnumItems(SHCONTF options, ShellFolder parent, DirectoryInfo info)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            foreach (var item in EnumFileSystemInfos(info, options.HasFlag(SHCONTF.SHCONTF_FOLDERS), options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS)))
            {
                if (item is DirectoryInfo di)
                {
                    yield return new LocalShellFolder(parent, di);
                }
                else
                {
                    if (string.Compare(item.Extension, ".xml", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        yield return new XmlLocalShellFolder(parent, (FileInfo)item);
                    }
                    else
                    {
                        yield return new LocalShellItem(parent, (FileInfo)item);
                    }
                }
            }
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options) => EnumItems(options, this, Info);
    }
}
