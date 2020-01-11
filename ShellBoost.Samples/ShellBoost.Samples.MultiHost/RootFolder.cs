using System;
using System.Collections.Generic;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.MultiHost
{
    public class RootFolder : RootShellFolder
    {
        public RootFolder(OverviewShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
        }

        public OverviewShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            int max = 2;
            for (int i = 0; i < max; i ++)
            {
                yield return new SimpleItem(this, i.ToString());
            }
        }
    }
}
