using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.SevenZipFolder
{
    public class ArchiveFolderShellFolder : ShellFolder
    {
        public ArchiveFolderShellFolder(ShellFolder parent, ArchiveFolderInfo folder)
            : base(parent, new StringKeyShellItemId(folder.Name))
        {
            CanCopy = true;
            Folder = folder;
            AddColumn(Props.System.FileAttributes, SHCOLSTATE.SHCOLSTATE_DEFAULT);
            AddColumn(Props.System.Comment, SHCOLSTATE.SHCOLSTATE_DEFAULT);
            AddColumn(Props.System.ZipFolder.Encrypted, SHCOLSTATE.SHCOLSTATE_DEFAULT);
            AddColumn(Props.System.ZipFolder.CRC32, SHCOLSTATE.SHCOLSTATE_DEFAULT);
        }

        public ArchiveFolderInfo Folder { get; }
        public new ArchiveRootShellFolder Root => (ArchiveRootShellFolder)base.Root;

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if ((options & SHCONTF.SHCONTF_FOLDERS) == SHCONTF.SHCONTF_FOLDERS)
            {
                foreach (var folder in Folder.Folders)
                {
                    yield return new ArchiveFolderShellFolder(this, folder);
                }
            }

            if ((options & SHCONTF.SHCONTF_NONFOLDERS) == SHCONTF.SHCONTF_NONFOLDERS)
            {
                foreach (var file in Folder.Files)
                {
                    yield return new ArchiveFileShellItem(this, file);
                }
            }
        }
    }
}
