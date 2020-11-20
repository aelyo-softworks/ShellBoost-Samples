using System;
using System.IO;
using Microsoft.Win32;
using ShellBoost.Core.Client;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.CloudFolderSync.Utilities
{
    public static class ShellRegistration
    {
        // https://stackoverflow.com/questions/23777688/pin-a-folder-to-navigation-pane-in-windows-explorer/34737590
        // register as a cloud provider in explorer tree view
        public static void RegisterCloudStorageProvider(string folderPath, string displayName, Guid id, string iconPath = null, int sortOrderIndex = 30)
        {
            if (folderPath == null)
                throw new ArgumentNullException(nameof(folderPath));

            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));

            if (id == Guid.Empty)
                throw new ArgumentException(null, nameof(id));

            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, Path.Combine(@"Software\Classes\CLSID", id.ToString("B"))))
            {
                key.SetValue(null, displayName);
                key.SetValue("System.IsPinnedToNameSpaceTree", 1);
                key.SetValue("SortOrderIndex", sortOrderIndex);

                using (var icon = WindowsUtilities.EnsureSubKey(key, "DefaultIcon"))
                {
                    var path = iconPath.Nullify() ?? NativeProxy.Default.DllPath;
                    icon.SetValue(null, path);
                }

                using (var server = WindowsUtilities.EnsureSubKey(key, "InProcServer32"))
                {
                    server.SetValue(null, @"%systemroot%\system32\shell32.dll");
                }

                using (var folder = WindowsUtilities.EnsureSubKey(key, "ShellFolder"))
                {
                    var attributes = SFGAO.SFGAO_HASSUBFOLDER |
                        SFGAO.SFGAO_CANCOPY |
                        SFGAO.SFGAO_CANLINK |
                        SFGAO.SFGAO_STORAGE |
                        SFGAO.SFGAO_HASPROPSHEET |
                        SFGAO.SFGAO_STORAGEANCESTOR |
                        SFGAO.SFGAO_FILESYSANCESTOR |
                        SFGAO.SFGAO_FOLDER |
                        SFGAO.SFGAO_FILESYSTEM;
                    folder.SetValue("Attributes", (int)attributes);
                    folder.SetValue("FolderValueFlags", 0x28);
                }

                using (var instance = WindowsUtilities.EnsureSubKey(key, "Instance"))
                {
                    var CLSID_FileFolderBoth = new Guid("{0E5AAE11-A475-4C5b-AB00-C66DE400274E}");
                    instance.SetValue("CLSID", CLSID_FileFolderBoth.ToString("B"));

                    using (var bag = WindowsUtilities.EnsureSubKey(instance, "InitPropertyBag"))
                    {
                        bag.SetValue("Attributes", 0x11);
                        bag.SetValue("TargetFolderPath", folderPath);
                    }
                }
            }

            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, Path.Combine(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace",id.ToString("B"))))
            {
                key.SetValue(null, displayName);
            }

            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"))
            {
                key.SetValue(id.ToString("B"), 1);
            }
        }

        public static void UnregisterCloudStorageProvider(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException(null, nameof(id));

            Registry.CurrentUser.DeleteSubKeyTree(Path.Combine(@"Software\Classes\CLSID", id.ToString("B")), false);
            Registry.CurrentUser.DeleteSubKeyTree(Path.Combine(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace", id.ToString("B")), false);

            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", true))
            {
                if (key != null)
                {
                    key.DeleteValue(id.ToString("B"), false);
                }
            }
        }
    }
}
