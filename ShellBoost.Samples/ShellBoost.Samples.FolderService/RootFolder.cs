using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            yield return new SimpleItem(this, "Client Principal Name: " + ShellContext.Current.ClientPrincipalName);
            yield return new SimpleItem(this, "Client Process Id: " + ShellContext.Current.ClientProcessId);
            yield return new SimpleItem(this, "Client Process: " + Process.GetProcessById(ShellContext.Current.ClientProcessId)?.ProcessName);

            // if we impersonate, this will be the same as the client principal name
            // otherwise it will be the identity that runs the service process
            yield return new SimpleItem(this, "Server Windows Identity: " + WindowsIdentity.GetCurrent()?.Name);
            yield return new SimpleItem(this, "Server Process: " + Process.GetCurrentProcess().ProcessName);
        }
    }
}
