using ShellBoost.Core;

namespace ShellBoost.Samples.Overview
{
    public class OverviewShellFolderServer : ShellFolderServer
    {
        private RootFolder _root;

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(idl);
            }
            return _root;
        }
    }
}
