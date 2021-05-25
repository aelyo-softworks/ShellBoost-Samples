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

        private void ShowProperties(IntPtr owner) => TaskUtilities.EnsureSTAThreadTask(() =>
        {
            using (var form = new Properties())
            {
                WindowsUtilities.RunForm(form, owner);
            }
        });

        // registry is special, a folder can have an item with the same name as a subkey
        // so we need to distinguish between them to be able to parse as correctly as possible (some level of ambiguity cannot be prevented)
        // only \ is invalid in registry names
        public static ShellItemId GetValueItemId(string name) => new StringKeyShellItemId("V\\" + name);
        public static ShellItemId GetKeyItemId(string name) => new StringKeyShellItemId("K\\" + name);
        public static Tuple<bool?, string> ParseItemId(ShellItemId id)
        {
            var name = (KeyShellItemId.From(id?.Data, false) as StringKeyShellItemId)?.Value;
            if (string.IsNullOrEmpty(name))
                return null;

            if (name.StartsWith("V\\"))
                return new Tuple<bool?, string>(false, name.Substring(2));

            if (name.StartsWith("K\\"))
                return new Tuple<bool?, string>(true, name.Substring(2));

            return new Tuple<bool?, string>(null, name);
        }
    }
}
