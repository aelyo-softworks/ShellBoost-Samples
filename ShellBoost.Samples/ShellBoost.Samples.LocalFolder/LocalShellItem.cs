using System.IO;
using ShellBoost.Core;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellItem : ShellItem
    {
        public LocalShellItem(ShellFolder parent, FileInfo info)
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
            //Size = info.Length;
            //DateModified = info.LastWriteTime;
            //DateCreated = info.CreationTime;
        }

        public FileInfo Info { get; }
    }
}
