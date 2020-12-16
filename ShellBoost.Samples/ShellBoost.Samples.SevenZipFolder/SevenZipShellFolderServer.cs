using System;
using System.Collections.Concurrent;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.SevenZipFolder
{
    public class SevenZipShellFolderServer : ShellFolderServer
    {
        private readonly ConcurrentDictionary<ShellItemIdList, ArchiveRootShellFolder> _cache = new ConcurrentDictionary<ShellItemIdList, ArchiveRootShellFolder>();

        // ShellBoost will call that method each time an archive file is open using the Shell
        protected override RootShellFolder GetRootFolder(ShellItemIdList idList)
        {
            if (idList == null)
                throw new ArgumentNullException(nameof(idList));

            if (!_cache.TryGetValue(idList, out var folder))
            {
                // this check to ensure we're not being called for anything else than what we expect (like a folder, etc.)
                var path = idList.GetFileSystemPath();
                if (IOUtilities.FileExists(path))
                {
                    folder = new ArchiveRootShellFolder(this, idList);
                }
                _cache[idList] = folder; // we also store null
            }
            return folder;
        }
    }
}
