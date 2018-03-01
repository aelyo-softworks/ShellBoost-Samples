using ShellBoost.Core;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class WebShellFolderServer : ShellFolderServer
    {
        private RootWebShellFolder _drive;

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_drive == null)
            {
                _drive = new RootWebShellFolder(this, idl);
            }
            return _drive;
        }
    }
}
