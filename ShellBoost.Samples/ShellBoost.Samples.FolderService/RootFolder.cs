using System;
using System.Collections.Generic;
using System.Security.Principal;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.FolderService
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
            var wi = WindowsIdentity.GetCurrent().Name;
            if (string.IsNullOrEmpty(wi))
            {
                wi = "???";
            }

            yield return new SimpleItem(this, "Windows Identity: " + wi);

            wi = ShellContext.Current.ClientPrincipalName;
            if (string.IsNullOrEmpty(wi))
            {
                wi = "???";
            }
            yield return new SimpleItem(this, "Client Principal Name: " + wi);
            yield return new SimpleItem(this, "Client Process Id: " + ShellContext.Current.ClientProcessId);
        }
    }
}
