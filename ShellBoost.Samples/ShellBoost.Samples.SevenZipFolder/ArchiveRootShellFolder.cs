using System;
using System.Collections.Generic;
using System.IO;
using SevenZip;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.SevenZipFolder
{
    // in this sample, the root folder corresponds to an archive file
    public class ArchiveRootShellFolder : RootShellFolder
    {
        public ArchiveRootShellFolder(SevenZipShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
            Archive = new ArchiveRootFolderInfo(idList.GetPath());
            DisplayName = Path.GetFileName(Archive.FilePath);
        }

        public ArchiveRootFolderInfo Archive { get; }
        public SevenZipShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if ((options & SHCONTF.SHCONTF_FOLDERS) == SHCONTF.SHCONTF_FOLDERS)
            {
                foreach (var folder in Archive.Folders)
                {
                    yield return new ArchiveFolderShellFolder(this, folder);
                }
            }

            if ((options & SHCONTF.SHCONTF_NONFOLDERS) == SHCONTF.SHCONTF_NONFOLDERS)
            {
                foreach (var file in Archive.Files)
                {
                    yield return new ArchiveFileShellItem(this, file);
                }
            }
        }
    }
}
