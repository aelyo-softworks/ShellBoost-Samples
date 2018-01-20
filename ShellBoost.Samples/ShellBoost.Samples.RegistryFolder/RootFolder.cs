using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.RegistryFolder.UI;
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

        // context menu on the root folder
        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (appendMenu == null)
                throw new ArgumentNullException(nameof(appendMenu));

            if (items.Count == 0) // 0 means this is called from Explorer's tree view. We don't want to add the menus twice.
                return;

            var regeditItem = new ShellMenuItem(appendMenu, "Run &Regedit...");
            regeditItem.Invoke += (sender, e) => Process.Start("regedit");
            appendMenu.Items.Add(regeditItem);

            appendMenu.Items.Add(new ShellMenuSeparatorItem());

            var propertiesItem = new ShellMenuItem(appendMenu, "&Properties...");
            propertiesItem.Invoke += (sender, e) => ShowProperties(e.HwndOwner);
            appendMenu.Items.Add(propertiesItem);
        }

        private async void ShowProperties(IntPtr owner)
        {
            using (var form = new Properties())
            {
                await WindowsUtilities.ShowModelessAsync(form, owner);
            }
        }
    }
}
