using System;
using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class RootFolder : RootShellFolder
    {
        public RootFolder(LocalShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
        }

        public LocalShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options) => LocalShellFolder.EnumItems(options, this, Server.Info);
    }
}
