using ShellBoost.Core;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryShellFolderServer : ShellFolderServer
    {
        protected override RootShellFolder GetRootFolder(ShellItemIdList idl) => new RootFolder(this, idl);
    }
}
