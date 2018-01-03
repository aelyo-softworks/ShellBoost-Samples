using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryShellFolderServer : ShellFolderServer
    {
        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            return new RootFolder(this, idl);
        }
    }
}
