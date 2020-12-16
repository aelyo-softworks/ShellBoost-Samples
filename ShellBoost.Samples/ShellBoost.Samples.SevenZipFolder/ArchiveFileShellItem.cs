using System.IO;
using SevenZip;
using ShellBoost.Core;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.SevenZipFolder
{
    public class ArchiveFileShellItem : ShellItem
    {
        public ArchiveFileShellItem(ShellFolder parent, ArchiveFileInfo file)
            : base(parent, new StringKeyShellItemId(Path.GetFileName(file.FileName)))
        {
            CanCopy = true;
            File = file;

            // set common properties
            DateCreated = file.CreationTime;
            DateAccessed = file.LastAccessTime;
            DateModified = file.LastWriteTime;
            Size = unchecked((long)file.Size);

            // since this item is fully virtual, we must add the item's type (a generalization to physical item's Extension)
            ItemType = Path.GetExtension(file.FileName);

            // set rarer properties (column are added to the containing folder)
            SetPropertyValue(Props.System.FileAttributes, file.Attributes);
            SetPropertyValue(Props.System.Comment, file.Comment);
            SetPropertyValue(Props.System.ZipFolder.Encrypted, file.Encrypted);
            SetPropertyValue(Props.System.ZipFolder.CRC32, file.Crc);
        }

        public ArchiveFileInfo File { get; }
        public new ArchiveFolderShellFolder Parent => (ArchiveFolderShellFolder)base.Parent;
        public ArchiveRootShellFolder Root => Parent.Root;

        // shell's asking for data, for copy/paste, drag/drop, etc.
        public override ShellContent GetContent() => Root.Archive.ExtractFile(File);
    }
}
