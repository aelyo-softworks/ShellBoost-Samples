using System;
using System.ComponentModel;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.Mirror
{
    public class MirrorFolderServer : ShellFolderServer
    {
        private ShellFolder _root;

        public MirrorFolderServer(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (!Directory.Exists(path))
                throw new Win32Exception(ShellUtilities.E_PATH_NOT_FOUND);

            Path = System.IO.Path.GetFullPath(path);
        }

        public string Path { get; }

        protected override ShellFolder GetFolderAsRoot(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(Path, idl);
            }
            return _root;
        }
    }
}
