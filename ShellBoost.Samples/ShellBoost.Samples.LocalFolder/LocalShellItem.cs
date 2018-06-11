using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellItem : ShellItem
    {
        public LocalShellItem(ShellFolder parent, FileInfo info)
            : base(parent, info) // there is a specific overload for FileInfo
        {
            CanCopy = true;
            CanDelete = true;
            CanLink = true;
            CanMove = true;
            CanPaste = true;
            CanRename = true;
            Info = info;

            var ms = new MemoryPropertyStore();
            ms.SetValue(Props.System.PropList.StatusIcons, "prop:" + LocalShellFolder.IconProperty.CanonicalName);
            ms.SetValue(Props.System.PropList.StatusIconsDisplayFlag, (uint)2);

            if (info.Name.Contains("error"))
            {
                ms.SetValue(LocalShellFolder.IconProperty, IconValue.Error);
            }
            else if (info.Name.Contains("warn"))
            {
                ms.SetValue(LocalShellFolder.IconProperty, IconValue.Warning);
            }
            else
            {
                ms.SetValue(LocalShellFolder.IconProperty, IconValue.Ok);
            }

            SetPropertyValue(LocalShellFolder.IconUIProperty, ms);
        }

        public FileInfo Info { get; }
    }
}
