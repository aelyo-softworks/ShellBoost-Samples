using System;
using ShellBoost.Core;

namespace ShellBoost.Samples.Mirror
{
    public class RootFolder : ShellFolder
    {
        // use root folder constructor
        public RootFolder(string rootPath, ShellItemIdList idList)
            : base(idList)
        {
            if (rootPath == null)
                throw new ArgumentNullException(nameof(rootPath));

            FileSystemPath = rootPath;
        }
    }
}
