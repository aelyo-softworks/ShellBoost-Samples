using ShellBoost.Core;

namespace ShellBoost.Samples.Overview
{
    public class OverviewShellFolderServer : ShellFolderServer
    {
        private SimpleFolder _root;

        protected override ShellFolder GetFolderAsRoot(ShellItemIdList idList)
        {
            if (_root == null)
            {
                _root = new SimpleFolder(idList);
            }
            return _root;
        }
    }
}
