using ShellBoost.Core;

namespace ShellBoost.Samples.PhysicalOverview
{
    public class PhysicalOverviewShellFolderServer : ShellFolderServer
    {
        private RootFolder _root;

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_root == null)
            {
                _root = new RootFolder(this, idl);
            }
            return _root;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _root?.ChangeNotifier?.Dispose();
            }
        }
    }
}
