﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;
using ShellBoost.Core.WindowsShell;
using ShellBoost.Samples.RegistryFolder.UI;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryKeyFolder : ShellFolder
    {
        // Declared in RegistryFolder.propdesc schema file. This file must be registered once. Check Program.cs.
        public static readonly PropertyDescription TypeProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.RegistryFolder.Type", true);
        public static readonly PropertyDescription DataProperty = PropertySystem.GetPropertyDescription("ShellBoost.Samples.RegistryFolder.Data", true);

        public RegistryKeyFolder(ShellFolder parent, string name)
            : base(parent, RootFolder.GetKeyItemId(name))
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
                var parent = ((RegistryKeyFolder)Parent).Path;
                if (parent == null)
                    return DisplayName;

                return parent + "\\" + DisplayName;
            }
        }

        public override ShellItem GetItem(ShellItemId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var tuple = RootFolder.ParseItemId(id);
            if (tuple == null)
                return null; // not ours

            using (var key = OpenKey(false))
            {
                if (key != null)
                {
                    switch (tuple.Item1)
                    {
                        case true: // key/folder
                            using (var subKey = key.OpenSubKey(tuple.Item2, false))
                            {
                                if (subKey != null)
                                    return new RegistryKeyFolder(this, tuple.Item2);
                            }
                            break;

                        case false: // value/item
                            var value = key.GetValue(tuple.Item2);
                            if (value != null)
                                return new RegistryValueItem(this, tuple.Item2);

                            break;
                    }

                    // either the type type (folder vs value) doesn't correspond, or we're parsing some external string so fallback on our parser
                    return GetItem(tuple.Item2);
                }
            }
            return null;
        }

        protected override ShellItem GetItem(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return null;

            using (var key = OpenKey(false))
            {
                if (key != null)
                {
                    // here, we can face some level of ambiguity, we choose to favor folder
                    using (var subKey = key.OpenSubKey(displayName, false))
                    {
                        if (subKey != null)
                            return new RegistryKeyFolder(this, displayName);
                    }

                    var value = key.GetValue(displayName);
                    if (value != null)
                        return new RegistryValueItem(this, displayName);
                }
            }
            return null;
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
            var path = Path;
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
                // only for one non-folder item 
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

            if (items.Count > 0)
            {
                // add the "Send To" menu
                appendMenu.Items.Add(new ShellMenuSendToItem());
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

        /// <summary>
        /// Called when [shell menu item invoke].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ShellMenuInvokeEventArgs"/> instance containing the event data.</param>
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
                        // ShellBoost's implicit threads are MTA but we need an STA to show UI (with Winforms, WPF, etc.)
                        _ = TaskUtilities.EnsureSTAThreadTask(() =>
                        {
                            using (var form = new EditValue())
                            {
                                var valueItem = (RegistryValueItem)e.Items[0];
                                form.LoadEditor(BaseParent.Hive, Path, valueItem.KeyName);
                                var result = WindowsUtilities.RunForm(form);
                                if (result == DialogResult.OK)
                                {
                                    using (var key = OpenKey(true))
                                    {
                                        key.SetValue(valueItem.KeyName, form.NewValue);
                                        valueItem.Parent?.NotifyUpdate();
                                    }
                                }
                            }
                        });
                        return;
                    }
                    break;

                case MenuCommand.NewKey:
                    using (var key = OpenKey(true))
                    {
                        if (key != null)
                        {
                            var newName = GetNewName("New Key #", key.GetSubKeyNames());
                            try
                            {
                                key.CreateSubKey(newName);
                                e.Folder.RefreshShellViews();
                                await SelectAndEdit(newName);
                            }
                            catch (Exception ex)
                            {
                                // probably an access denied error
                                await WindowsUtilities.DoModelessAsync(() =>
                                {
                                    MessageBox.Show(new Win32Window(e.HwndOwner), "The Registry Folder cannot set a value here: " + ex.Message, "Registry Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                });
                            }
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
                            var newName = GetNewName("New Value #", key.GetValueNames());
                            try
                            {
                                key.SetValue(newName, GetDefaultValue(kind), kind);
                                e.Folder.RefreshShellViews();
                                await SelectAndEdit(newName);
                            }
                            catch (Exception ex)
                            {
                                // probably an access denied error
                                await WindowsUtilities.DoModelessAsync(() =>
                                {
                                    MessageBox.Show(new Win32Window(e.HwndOwner), "The Registry Folder cannot set a value here: " + ex.Message, "Registry Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                });
                            }
                        }
                    }
                    break;
            }
        }

        private async Task SelectAndEdit(string name)
        {
            var view = await ShellContext.Current.GetShellBoostViewAsync(this);
            var fv = view?.FolderView;
            if (fv != null)
            {
                // we use a retry loop since we don't exactly know when the item appears
                // experience shows retries is usually between 0 to 2
                const int maxRetries = 50;
                const int timeSlice = 100; // ms
                var index = -1;
                var retries = 0;
                do
                {
                    index = fv.GetItemIndex(i => i.SIGDN_NORMALDISPLAY.EqualsIgnoreCase(name), throwOnError: false);
                    if (index >= 0 || retries > maxRetries)
                        break;

                    await Task.Delay(timeSlice);
                    retries++;
                }
                while (true);

                if (index >= 0)
                {
                    fv.DeselectAllItems(false);
                    fv.SelectItem(index, SVSIF.SVSI_SELECT | SVSIF.SVSI_EDIT, false);
                }
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

                case RegistryValueKind.Binary:
                    return System.Text.Encoding.UTF8.GetBytes("Hello World");

                default:
                    return null;
            }
        }

        private static string GetNewName(string baseName, string[] names)
        {
            var i = 1;
            do
            {
                var name = baseName + i;
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
                case ShellOperation.SetNameOf:
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
                    }
                }
                e.HResult = ShellUtilities.S_OK;
                return;
            }

            // delete a value
            using (var key = OpenKey(true))
            {
                if (key != null)
                {
                    var keyName = ((RegistryValueItem)e.Item).KeyName;
                    key.DeleteValue(keyName, false);

                    // deleting the default value will in fact unset its value, so we don't want explorer to remove the item visually
                    if (string.IsNullOrEmpty(keyName))
                    {
                        const int COPYENGINE_E_USER_CANCELLED = unchecked((int)0x80270000);
                        e.HResult = COPYENGINE_E_USER_CANCELLED;
                        e.Item.NotifyUpdate();
                    }
                }
            }
            e.HResult = ShellUtilities.S_OK;
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
                        var error = RegRenameKey(key.Handle, e.Item.DisplayName, e.NewName);
                        if (error != 0)
                        {
                            await WindowsUtilities.DoModelessAsync(() =>
                            {
                                MessageBox.Show(new Win32Window(e.HwndOwner), "The Registry Folder cannot rename '" + e.Item.DisplayName + "'. " + new Win32Exception(error).Message, "Registry Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
                        }
                        else
                        {
                            e.NewId = RootFolder.GetKeyItemId(e.NewName);
                            e.Item.NotifyRename(e.NewId);
                            e.Item.Parent?.NotifyUpdate();
                            e.HResult = ShellUtilities.S_OK;
                        }
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
                        var value = key.GetValue(e.Item.DisplayName);
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
                            e.NewId = RootFolder.GetValueItemId(e.NewName);
                            e.Item.Parent?.NotifyUpdate();
                            e.HResult = ShellUtilities.S_OK;
                        }
                    }
                }
            }
        }

        // note: there's no easier way to rename a key
        // https://stackoverflow.com/questions/1516312/registry-how-to-rename-key-in-registry-using-c
        [DllImport("advapi32")]
        private static extern int RegRenameKey(SafeRegistryHandle hKey, [MarshalAs(UnmanagedType.LPWStr)] string oldname, [MarshalAs(UnmanagedType.LPWStr)] string newname);
    }
}
