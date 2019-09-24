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
        }

        private class MyShelItem : ShellItem
        {
            // this item is backed by a physical file
            public MyShelItem(ShellFolder parent, FileInfo info)
                : base(parent, info)
            {
                CanDelete = true;
                CanMove = true;
                CanCopy = true;
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
        }

        protected override ShellItem CreateFileSystemFolder(DirectoryInfo info) => new MyShellFolder(this, info);
        protected override ShellItem CreateFileSystemItem(FileInfo info) => new MyShelItem(this, info);

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

        // these 3 following functions overrides allow common dialogs to parse & evaluate an item that doesn't exist yet
        public override bool TryParseItem(string displayName, out int eatenCharacters, out SFGAO attributes, out ShellItemIdList relativeIdl)
        {
            // check path is valid
            if (IOUtilities.PathIsValidFileName(displayName))
            {
                // create some PIDL & attribute
                eatenCharacters = displayName.Length;
                attributes = SFGAO.SFGAO_CANLINK | SFGAO.SFGAO_HASPROPSHEET | SFGAO.SFGAO_STORAGE | SFGAO.SFGAO_STREAM;
                relativeIdl = new ShellItemIdList();
                relativeIdl.Add(new StringKeyShellItemId(displayName));
                return true;
            }
            return base.TryParseItem(displayName, out eatenCharacters, out attributes, out relativeIdl);
        }

        // this is the reverse of the previous function
        public override bool TryGetDisplayName(ShellItemIdList relativeIdl, out string displayName)
        {
            if (KeyShellItemId.From(relativeIdl.Last.Data, false) is StringKeyShellItemId sk)
            {
                displayName = sk.Value;
                return true;
            }

            return base.TryGetDisplayName(relativeIdl, out displayName);
        }

        // get some attributes (the default returned by ShellItem)
        public override bool TryGetAttributes(ShellItemIdList relativeIdl, out SFGAO attributes)
        {
            attributes = SFGAO.SFGAO_CANLINK | SFGAO.SFGAO_HASPROPSHEET | SFGAO.SFGAO_STORAGE | SFGAO.SFGAO_STREAM;
            return true;
        }

        protected override void OnFileDialogEvent(object sender, FileDialogEventArgs e)
        {
            if (e.Type == FileDialogEventType.Overwrite)
            {
                // commdlg can call us for virtual items not even written yet
                // tell it it's ok to overwrite
                if (GetItem(e.ItemIdList.Last) == null)
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

                if (e.DataObject[ShellDataObjectFormat.CFSTR_FILEDESCRIPTOR] != null)
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
