using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.PhysicalOverview
{
    public class PhysicalFolder : ShellFolder
    {
        // this item is backed by a physical folder
        public PhysicalFolder(ShellFolder parent, DirectoryInfo info)
            : base(parent, info)
        {
            CanPaste = true;

            // this enables the "New" menus
            Attributes |= SFGAO.SFGAO_STORAGEANCESTOR | SFGAO.SFGAO_STORAGE;
        }

        public PhysicalFolder(ShellFolder parent, ShellItemId fileSystemId, string fileSystemPath)
            : base(parent, fileSystemId, fileSystemPath)
        {
            CanDelete = true;

            // this enables the "New" menus
            Attributes |= SFGAO.SFGAO_STORAGEANCESTOR | SFGAO.SFGAO_STORAGE;
        }

        //enable this for folder deletion / can have border effects...
        protected override void OnGetDataObjectEvent(object sender, GetDataObjectEventArgs e)
        {
            foreach (var item in e.Items)
            {
                var obj = Item.FromParsingName(item.FileSystemPath, throwOnError: false);
                if (obj != null)
                {
                    e.AddDataObject(obj);
                }
            }
            base.OnGetDataObjectEvent(sender, e);
        }

        private class MyShellItem : ShellItem
        {
            // this item is backed by a physical file
            public MyShellItem(ShellFolder parent, FileInfo info)
                : base(parent, info)
            {
                CanDelete = true;
                CanMove = true;
                CanCopy = true;
                CanRename = true;
            }

            public MyShellItem(ShellFolder parent, ShellItemId fileSystemId, string fileSystemPath)
                : base(parent, fileSystemId, fileSystemPath)
            {
                CanDelete = true;
                CanMove = true;
                CanCopy = true;
                //ReadPropertiesFromShell = true;
            }

            public override bool TryGetPropertyValue(Props.PropertyKey key, out object value)
            {
                bool b = base.TryGetPropertyValue(key, out value);
                if (!b)
                {
                    // file equivalence for transfer operations, preview, etc.
                    if (key == PropertyStore.TransferMediumItem || key == PropertyStore.IdentityName ||
                        key == PropertyStore.PreviewItem || key == PropertyStore.CurrentItem ||
                        key == PropertyStore.DelegateItem || key == PropertyStore.DisplayItem ||
                        key == PropertyStore.Moniker)
                    {
                        value = FileSystemPath;
                        return true;
                    }
                }
                return b;
            }
        }

        private class MyShellFolder : PhysicalFolder
        {
            public MyShellFolder(ShellFolder parent, DirectoryInfo info)
                : base(parent, info)
            {
                CanDelete = true;
            }

            public MyShellFolder(ShellFolder parent, ShellItemId fileSystemId, string fileSystemPath)
                : base(parent, fileSystemId, fileSystemPath)
            {
                CanDelete = true;
            }
        }

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new MyShellFolder(this, info);
        protected override ShellItem CreateFileSystemItem(FileInfo info) => new MyShellItem(this, info);
        protected override ShellItem CreateFileSystemItem(ShellItemId fileSystemId, string fileSystemPath) => new MyShellItem(this, fileSystemId, fileSystemPath);
        protected override ShellItem CreateFileSystemFolder(ShellItemId fileSystemId, string fileSystemPath) => new MyShellFolder(this, fileSystemId, fileSystemPath);

        protected override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(DirectoryInfo info, SHCONTF options, string searchPattern)
        {
            // don't display hidden files
            foreach (var fsi in base.EnumerateFileSystemInfos(info, options, searchPattern))
            {
                if (fsi.Attributes.HasFlag(FileAttributes.Hidden))
                    continue;

                yield return fsi;
            }
        }

        protected override ShellItem GetItem(string displayName)
        {
            var item = base.GetItem(displayName);
            if (item != null)
                return item;

            // this is for Common Dialog Save As support
            // here, we create a non-existing item.
            if (IOUtilities.PathIsValidFileName(displayName))
                return new MyShellItem(this, new FileInfo(Path.Combine(FileSystemPath, displayName)));

            return null;
        }

        public override ShellItem GetItem(ShellItemId id)
        {
            var item = base.GetItem(id);
            if (item != null)
                return item;

            // this is for Common Dialog Save As support
            // here, we check it's a non-existing item that we may have built in GetItem(string displayName)
            if (id.TryGetFileSystemName(out var fileName))
                return new MyShellItem(this, new FileInfo(Path.Combine(FileSystemPath, fileName)));

            return null;
        }

        public override bool TryGetPropertyValue(Props.PropertyKey key, out object value)
        {
            bool b = base.TryGetPropertyValue(key, out value);
            if (!b)
            {
                // file equivalence for transfer operations, preview, etc.
                if (key == PropertyStore.TransferMediumItem || key == PropertyStore.IdentityName ||
                    key == PropertyStore.PreviewItem || key == PropertyStore.CurrentItem ||
                    key == PropertyStore.DelegateItem || key == PropertyStore.DisplayItem ||
                    key == PropertyStore.Moniker)
                {
                    value = FileSystemPath;
                    return true;
                }
            }
            return b;
        }

        protected override void OnFileDialogEvent(object sender, FileDialogEventArgs e)
        {
            if (e.Type == FileDialogEventType.Overwrite)
            {
                // commdlg can call us for virtual items (that we sent back) not even written yet
                // tell it it's ok to overwrite
                if (e.ItemIdList.Last.TryGetFileSystemName(out var fileName) &&
                    !IOUtilities.FileExists(Path.Combine(FileSystemPath, fileName)))
                {
                    var ov = (OverwriteFileDialogEventArgs)e;
                    ov.Response = FDE_OVERWRITE_RESPONSE.FDEOR_ACCEPT;
                }
            }
        }

        // handle drag & drop
        protected override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
            if (e.Type == DragDropTargetEventType.DragDrop)
            {
                if (e.DataObject[ShellDataObjectFormat.CFSTR_SHELLIDLIST] != null)
                {
                    // you can use other formats but this one, when present is the only that contains full PIDLs (to possible virtual items, not only file system items)
                    foreach (var idl in e.DataObject.ItemsIdLists)
                    {
                        // note we only handle file paths here but we could also support IStream, etc.
                        var path = idl.GetFileSystemPath();
                        if (path != null && IOUtilities.FileExists(path))
                        {
                            var targetPath = Path.Combine(FileSystemPath, Path.GetFileName(path));
                            IOUtilities.FileOverwrite(path, targetPath);

                            // tell whoever needs to know we've added a file here
                            NotifyUpdate();
                        }
                    }
                    return;
                }

                if (e.DataObject[ShellDataObjectFormat.CFSTR_FILEDESCRIPTORW] != null)
                {
                    // this format can be seen when drag & dropping attachments from outlook (files) or from chrome (data) for example
                    if (e.Type == DragDropTargetEventType.DragDrop)
                    {
                        for (int i = 0; i < e.DataObject.FileDescriptors.Count; i++)
                        {
                            // file content can be a temp file path or a byte[]
                            if (i < e.DataObject.FileContentsPaths.Count)
                            {
                                var tempFilePath = e.DataObject.FileContentsPaths[i];
                                if (tempFilePath != null && IOUtilities.FileExists(tempFilePath))
                                {
                                    var targetPath = Path.Combine(FileSystemPath, e.DataObject.FileDescriptors[i].cFileName);

                                    // temp files can be moved if used only once
                                    IOUtilities.FileMove(tempFilePath, targetPath);

                                    // tell whoever needs to know we've added a file here
                                    NotifyUpdate();
                                }
                            }
                            else if (i < e.DataObject.FileContentsDatas.Count)
                            {
                                var targetPath = Path.Combine(FileSystemPath, e.DataObject.FileDescriptors[i].cFileName);

                                File.WriteAllBytes(targetPath, e.DataObject.FileContentsDatas[i]);
                                // tell whoever needs to know we've added a file here
                                NotifyUpdate();
                            }
                        }
                    }
                    return;
                }
            }
        }
    }
}
