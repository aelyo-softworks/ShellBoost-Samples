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

            // this sample works just because of this line
            // all the rest will be automatically built because we're using file system
            FileSystemPath = rootPath;
        }
    }
}
