using System;
using System.IO;
using ShellBoost.Core;

namespace ShellBoost.Samples.LocalFolder
{
    public class LocalShellFolderServer : ShellFolderServer
    {
        private RootFolder _root;

        public LocalShellFolderServer(DirectoryInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (!info.Exists)
                throw new ArgumentException(null, nameof(info));

            Info = info;
        }

        public DirectoryInfo Info { get; }

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(this, idl);
            }
            return _root;
        }
    }
}
