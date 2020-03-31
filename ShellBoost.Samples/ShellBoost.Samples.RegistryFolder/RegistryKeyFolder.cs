using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using ShellBoost.Samples.RegistryFolder.UI;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryKeyFolder : ShellFolder
    {
        // Declared in RegistryFolder.propdesc schema file. This file must be registered once. Check Program.cs.
        public static readonly PropertyDescription TypeProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.RegistryFolder.Type", true);
        public static readonly PropertyDescription DataProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.RegistryFolder.Data", true);

        public RegistryKeyFolder(ShellFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            CanDelete = true;
            CanRename = true;
            DisplayName = name;
            RemoveColumn(Props.System.ItemType);
            RemoveColumn(Props.System.Size);
            RemoveColumn(Props.System.DateModified);
            RemoveColumn(Props.System.PerceivedType);
            RemoveColumn(Props.System.Kind);
            AddColumn(TypeProperty.PropertyKey, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(DataProperty.PropertyKey, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        public virtual RegistryBaseKeyFolder BaseParent => ((RegistryKeyFolder)Parent).BaseParent;
        public new RootFolder Root => (RootFolder)base.Root;

        public virtual string Path
        {
            get
            {
                string parent = ((RegistryKeyFolder)Parent).Path;
                if (parent == null)
                    return DisplayName;

                return parent + "\\" + DisplayName;
            }
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            using (var key = OpenKey(false))
            {
                var list = new List<ShellItem>();
                if (key != null)
                {
                    if ((options & SHCONTF.SHCONTF_FOLDERS) == SHCONTF.SHCONTF_FOLDERS)
                    {
                        foreach (var name in key.GetSubKeyNames())
                        {
                            list.Add(new RegistryKeyFolder(this, name));
                        }
                    }

                    if ((options & SHCONTF.SHCONTF_NONFOLDERS) == SHCONTF.SHCONTF_NONFOLDERS)
                    {
                        // always add the default value
                        var def = new RegistryDefaultValueItem(this);
                        list.Add(def);
                        foreach (var name in key.GetValueNames())
                        {
                            if (name.Length == 0) // default value, already added
                                continue;

                            list.Add(new RegistryValueItem(this, name));
                        }
                    }
                }
                return list;
            }
        }

        public RegistryKey OpenKey(bool writable)
        {
            var bp = BaseParent;
            var bk = RegistryKey.OpenBaseKey(bp.Hive, RegistryView.Default);
            string path = Path;
            if (string.IsNullOrEmpty(path))
                return bk; // always writable

            var key = bk.OpenSubKey(path, writable);
            bk.Close();
            return key;
        }

        protected override void MergeContextMenu(ShellFolder folder, IReadOnlyList<ShellItem> items, ShellMenu existingMenu, ShellMenu appendMenu)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (appendMenu == null)
                throw new ArgumentNullException(nameof(appendMenu));

            appendMenu.AddInvokeItemHandler(OnShellMenuItemInvoke);
            if (items.Count == 1 && !items[0].IsFolder)
            {
                var modifyItem = new ShellMenuItem(appendMenu, "Modify...");
                modifyItem.Tag = MenuCommand.Modify;
                modifyItem.IsDefault = true;
                appendMenu.Items.Add(modifyItem);
            }
            else
            {
                if (existingMenu.Items.FirstOrDefault(i => i.Text == "New") == null)
                {
                    var newItem = new ShellMenuItem(appendMenu, "New");
                    appendMenu.Items.Add(newItem);

                    newItem.Items.Add(new ShellMenuItem(appendMenu, "Key") { Tag = MenuCommand.NewKey });
                    newItem.Items.Add(new ShellMenuSeparatorItem());
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "String Value") { Tag = MenuCommand.NewValueString });
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "Binary Value") { Tag = MenuCommand.NewValueBinary });
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "DWORD (32-bit) Value") { Tag = MenuCommand.NewValueDWord });
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "QWORD (64-bit) Value") { Tag = MenuCommand.NewValueQWord });
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "Multi-String Value") { Tag = MenuCommand.NewValueMultiString });
                    newItem.Items.Add(new ShellMenuItem(appendMenu, "Expandable String Value") { Tag = MenuCommand.NewValueExpandString });
                }
            }
        }

        private enum MenuCommand
        {
            Unknown = RegistryValueKind.Unknown,

            NewValueString = RegistryValueKind.String,
            NewValueBinary = RegistryValueKind.Binary,
            NewValueDWord = RegistryValueKind.DWord,
            NewValueQWord = RegistryValueKind.QWord,
            NewValueMultiString = RegistryValueKind.MultiString,
            NewValueExpandString = RegistryValueKind.ExpandString,

            NewKey = 100,
            Modify,
        }

        private async void OnShellMenuItemInvoke(object sender, ShellMenuInvokeEventArgs e)
        {
            // e.MenuItem can be null for standard commands
            var menu = (ShellMenu)sender;

            // log something (if a logger is configured)
            menu.Server.Configuration?.Logger?.Log(TraceLevel.Info, "Shell Item '" + e.MenuItem + "' (cmd:" + e.Command + ") called.");

            var mc = Conversions.ChangeType(e.MenuItem?.Tag, MenuCommand.Unknown);
            switch (mc)
            {
                case MenuCommand.Modify:
                    if (e.Items.Count == 1) // we only support modification of one value at a time
                    {
                        using (var form = new EditValue()) // note because of async + await, Dispose will happen in continuing task
                        {
                            var valueItem = (RegistryValueItem)e.Items[0];
                            form.LoadEditor(BaseParent.Hive, Path, valueItem.KeyName);
                            await WindowsUtilities.ShowModelessAsync(form, e.HwndOwner).ContinueWith((task) =>
                            {
                                if (task.Result == DialogResult.OK)
                                {
                                    using (var key = OpenKey(true))
                                    {
                                        key.SetValue(valueItem.KeyName, form.NewValue);
                                        e.Folder.RefreshShellViews();
                                    }
                                }
                            });
                        }
                        return;
                    }
                    break;

                case MenuCommand.NewKey:
                    using (var key = OpenKey(true))
                    {
                        if (key != null)
                        {
                            string newName = GetNewName("New Key #", key.GetSubKeyNames());
                            key.CreateSubKey(newName);
                            e.Folder.RefreshShellViews();
                        }
                    }
                    break;

                case MenuCommand.NewValueString:
                case MenuCommand.NewValueBinary:
                case MenuCommand.NewValueDWord:
                case MenuCommand.NewValueQWord:
                case MenuCommand.NewValueMultiString:
                case MenuCommand.NewValueExpandString:
                    var kind = (RegistryValueKind)mc;
                    using (var key = OpenKey(true))
                    {
                        if (key != null)
                        {
                            string newName = GetNewName("New Value #", key.GetValueNames());
                            key.SetValue(newName, GetDefaultValue(kind), kind);
                            e.Folder.RefreshShellViews();
                        }
                    }
                    break;
            }
        }

        private static object GetDefaultValue(RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return 0;

                case RegistryValueKind.QWord:
                    return 0L;

                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    return string.Empty;

                case RegistryValueKind.MultiString:
                    return new string[] { string.Empty };

                default:
                    return null;
            }
        }

        private static string GetNewName(string baseName, string[] names)
        {
            int i = 1;
            do
            {
                string name = baseName + i;
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                    return name;

                i++;
            }
            while (true);
        }

        protected override void OnOperate(ShellOperationEventArgs e)
        {
            switch (e.Operation)
            {
                case ShellOperation.RenameItem:
                    OnRename(e);
                    break;

                case ShellOperation.RemoveItem:
                    OnRemove(e);
                    break;
            }
        }

        private void OnRemove(ShellOperationEventArgs e)
        {
            if (e.Item.IsFolder)
            {
                // delete a key
                using (var key = OpenKey(true))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree(e.Item.DisplayName, false);
                        e.HResult = ShellUtilities.S_OK;
                    }
                }
                return;
            }

            // delete a value
            using (var key = OpenKey(true))
            {
                if (key != null)
                {
                    string keyName = ((RegistryValueItem)e.Item).KeyName;
                    key.DeleteValue(keyName, false);

                    // deleting the default value will in fact unset its value, so we don't want explorer to remove the item visually
                    if (string.IsNullOrEmpty(keyName))
                    {
                        const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);
                        e.HResult = COPYENGINE_E_USER_CANCELLED;
                        e.Item.NotifyUpdate();
                    }
                    else
                    {
                        e.HResult = ShellUtilities.S_OK;
                    }
                }
            }
        }

        private async void OnRename(ShellOperationEventArgs e)
        {
            if (e.Item.IsFolder)
            {
                // rename a key
                using (var key = OpenKey(true))
                {
                    if (key != null)
                    {
                        NtRenameKey(key.Handle, e.NewName);
                        e.NewId = new StringKeyShellItemId(e.NewName);
                        e.HResult = ShellUtilities.S_OK;
                    }
                }
                return;
            }

            // rename a value (delete + set)
            using (var key = OpenKey(true))
            {
                if (key != null)
                {
                    if (key.GetValueNames().Contains(e.NewName, StringComparer.OrdinalIgnoreCase))
                    {
                        await WindowsUtilities.DoModelessAsync(() =>
                        {
                            MessageBox.Show(new Win32Window(e.HwndOwner), "The Registry Folder cannot rename '" + e.Item.DisplayName + "'. The specified value name already exists. Type another name and try again.", "Registry Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                    }
                    else
                    {
                        object value = key.GetValue(e.Item.DisplayName);
                        if (value == null)
                        {
                            await WindowsUtilities.DoModelessAsync(() =>
                            {
                                MessageBox.Show(new Win32Window(e.HwndOwner), "The Registry Folder cannot rename '" + e.Item.DisplayName + "'. The specified value name does not exists. Refresh the view and try again.", "Registry Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
                        }
                        else
                        {
                            key.SetValue(e.NewName, value);
                            key.DeleteValue(e.Item.DisplayName, false);
                            e.NewId = new StringKeyShellItemId(e.NewName);
                            e.HResult = ShellUtilities.S_OK;
                        }
                    }
                }
            }
        }

        // Note: there's no easier way to rename a key
        // https://msdn.microsoft.com/en-us/library/cc512138.aspx
        [DllImport("ntdll.dll")]
        private static extern int NtRenameKey(SafeRegistryHandle hKey, [MarshalAs(UnmanagedType.LPWStr)] string newname);
    }
}
