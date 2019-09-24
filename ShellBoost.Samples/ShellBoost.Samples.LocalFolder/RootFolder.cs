using System;
using System.Collections.Generic;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class RootFolder : RootShellFolder
    {
        private readonly DropTargetShellFolder _dropTarget;

        public RootFolder(LocalShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
            _dropTarget = new DropTargetShellFolder(this);
        }

        public LocalShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            yield return new VirtualAndPhysicalShellFolder(this, "This is a virtual folder");

            foreach (var fi in LocalShellFolder.EnumerateFileSystemItems(Server.Info, "*"))
            {
                if (fi is DirectoryInfo di)
                {
                    if (di.Name.EqualsIgnoreCase(VirtualAndPhysicalShellFolder.PhysicalStorageName))
                        continue;

                    yield return new LocalShellFolder(this, di);
                }
                else
                {
                    yield return new LocalShellItem(this, (FileInfo)fi);
                }
            }

            yield return _dropTarget;
        }
    }
}
