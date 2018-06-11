using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellFolder : ShellFolder
    {
        // Declared in LocalFolder.propdesc schema file. This file must be registered once. Check Program.cs.
        public static readonly PropertyDescription IconUIProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.LocalFolder.IconUI", true);
        public static readonly PropertyDescription IconProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.LocalFolder.Icon", true);

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
            AddColumn(IconUIProperty);
        }

        public DirectoryInfo Info { get; }

        // we export this as internal so the root folder shares this behavior
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

        protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(DirectoryInfo info, SHCONTF options, string searchPattern) => EnumerateFileSystemItems(info, searchPattern);

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new LocalShellFolder(this, info);

        protected override ShellItem CreateFileSystemItem(FileInfo info)
        {
            // for demonstration purpose, we handle XML files like they were folder over their elements
            if (string.Compare(info.Extension, ".xml", StringComparison.OrdinalIgnoreCase) == 0)
                return new XmlDocumentShellFolder(this, info);

            return new LocalShellItem(this, info);
        }

        private List<string> GetPaths(DragDropTargetEventArgs e)
        {
            var list = new List<string>();
            if (e.DataObject[ShellDataObjectFormat.CFSTR_SHELLIDLIST]?.ConvertedData is IEnumerable<ShellItemIdList> idls)
            {
                foreach (var idl in idls)
                {
                    string path;
                    var item = Root.GetItem(idl);
                    if (item != null)
                    {
                        // this comes from ourselves
                        path = item.FileSystemPath;
                    }
                    else
                    {
                        // check it's a file system pidl
                        path = idl.GetFileSystemPath();
                    }

                    if (path != null)
                    {
                        list.Add(path);
                    }
                }
            }
            return list;
        }

        protected override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            e.HResult = ShellUtilities.S_OK;
            var paths = GetPaths(e);
            if (paths.Count > 0)
            {
                e.Effect = DragDropEffects.All;
            }

            if (e.Type == DragDropTargetEventType.DragDrop)
            {
                // file operation events need an STA thread
                WindowsUtilities.DoModelessAsync(() =>
                {
                    using (var fo = new FileOperation(true))
                    {
                        fo.PostCopyItem += (sender, e2) =>
                        {
                            // we could add some logic here
                        };

                        if (paths.Count == 1)
                        {
                            fo.CopyItem(paths[0], FileSystemPath, null);
                        }
                        else
                        {
                            fo.CopyItems(paths, FileSystemPath);
                        }
                        fo.SetOperationFlags(FOF.FOF_ALLOWUNDO | FOF.FOF_NOCONFIRMMKDIR | FOF.FOF_RENAMEONCOLLISION);
                        fo.PerformOperations();
                    }
                });
            }
        }
    }
}
