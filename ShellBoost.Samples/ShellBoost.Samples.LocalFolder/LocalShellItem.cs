using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellItem : ShellItem
    {
        public LocalShellItem(ShellFolder parent, FileInfo info)
            : base(parent, info) // there is a specific overload for DirectoryInfo
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            Info = info;
        }

        public FileInfo Info { get; }
    }
}
