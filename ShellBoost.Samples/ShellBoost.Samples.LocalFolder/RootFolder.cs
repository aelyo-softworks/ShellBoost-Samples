using System;
using System.Collections.Generic;
using System.IO;
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

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            foreach (var fi in LocalShellFolder.EnumerateFileSystemItems(Server.Info, "*"))
            {
                if (fi is DirectoryInfo di)
                {
                    yield return new LocalShellFolder(this, di);
                }
                else
                {
                    yield return new LocalShellItem(this, (FileInfo)fi);
                }
            }
        }
    }
}
