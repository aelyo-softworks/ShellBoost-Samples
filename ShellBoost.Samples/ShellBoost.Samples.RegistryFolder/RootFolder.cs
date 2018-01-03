using System;
using System.Collections.Generic;
using Microsoft.Win32;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RootFolder : RootShellFolder
    {
        public RootFolder(RegistryShellFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));

            Server = server;
            RemoveColumn(Props.System.ItemType);
            RemoveColumn(Props.System.Size);
            RemoveColumn(Props.System.DateModified);
            RemoveColumn(Props.System.PerceivedType);
            RemoveColumn(Props.System.Kind);
        }

        public RegistryShellFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            yield return new RegistryBaseKeyFolder(this, RegistryHive.ClassesRoot);
            yield return new RegistryBaseKeyFolder(this, RegistryHive.CurrentUser);
            yield return new RegistryBaseKeyFolder(this, RegistryHive.LocalMachine);
            yield return new RegistryBaseKeyFolder(this, RegistryHive.Users);
            yield return new RegistryBaseKeyFolder(this, RegistryHive.CurrentConfig);
        }
    }
}
