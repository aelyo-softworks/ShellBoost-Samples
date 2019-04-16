using System;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.SevenZipFolder
{
    public class SevenZipShellFolderServer : ShellFolderServer
    {
        // ShellBoost will call that method each time an archive file is open using the Shell
        protected override RootShellFolder GetRootFolder(ShellItemIdList idList)
        {
            if (idList == null)
                throw new ArgumentNullException(nameof(idList));

            // this check to ensure we're not being called for anything else than what we expect (like a folder, etc.)
            var path = idList.GetPath();
            if (!IOUtilities.FileExists(path))
                return null;

            return new ArchiveRootShellFolder(this, idList);
        }
    }
}
