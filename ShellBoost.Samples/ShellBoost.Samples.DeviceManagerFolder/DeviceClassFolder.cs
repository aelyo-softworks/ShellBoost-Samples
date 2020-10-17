using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;
using Windows.Devices.Enumeration;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.DeviceManagerFolder
{
    public class DeviceClassFolder : ShellFolder
    {
        private readonly ConcurrentDictionary<string, DeviceFolder> _deviceFolders = new ConcurrentDictionary<string, DeviceFolder>();

        public DeviceClassFolder(DeviceManagerFolder parent, Guid classGuid)
            : base(parent, new GuidKeyShellItemId(classGuid))
        {
            ClassGuid = classGuid;
            var sb = new StringBuilder(1024);
            var size = sb.Capacity;
            SetupDiGetClassDescription(classGuid, sb, sb.Capacity, ref size);
            DisplayName = sb.ToString();

            var iconIndex = GetIconPath(out var iconPath);
            if (iconIndex.HasValue && iconPath != null)
            {
                Thumbnail = new ShellThumbnail(NormalizeIconPath(iconPath), iconIndex.Value);
            }

            RemoveColumn(Props.System.ItemType);
            RemoveColumn(Props.System.Size);
            RemoveColumn(Props.System.DateModified);
            RemoveColumn(Props.System.PerceivedType);
            RemoveColumn(Props.System.Kind);
            AddColumn(Props.System.Devices.DeviceInstanceId, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(Props.System.Devices.ClassGuid, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(Props.System.Devices.ContainerId, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        public Guid ClassGuid { get; }
        public new DeviceManagerFolder Parent => (DeviceManagerFolder)base.Parent;
        public new DeviceManagerFolderServer FolderServer => (DeviceManagerFolderServer)base.FolderServer;

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if (!options.HasFlag(SHCONTF.SHCONTF_FOLDERS))
                return Enumerable.Empty<ShellItem>();

            return _deviceFolders.Values;
        }

        public override bool TryGetPropertyValue(Props.PropertyKey key, out object value)
        {
            if (key == Props.System.Devices.ClassGuid)
            {
                value = ClassGuid.ToString("B");
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }

        internal void AddDeviceInterface(DeviceInformation device, DeviceInformation deviceInterface)
        {
            // add a device folder
            var created = false;
            var deviceFolder = _deviceFolders.AddOrUpdate(device.Id, (id) =>
            {
                created = true;
                return new DeviceFolder(this, device);
            }, (k, o) => o);

            // add the device interface to the device folder
            deviceFolder.AddDeviceInterface(deviceInterface);

            if (created)
            {
                deviceFolder.NotifyCreate();
            }
            else
            {
                deviceFolder.NotifyUpdate();
            }
        }

        private int? GetIconPath(out string filePath)
        {
            filePath = null;
            using (var key = Registry.LocalMachine.OpenSubKey(Path.Combine(@"SYSTEM\CurrentControlSet\Control\Class", ClassGuid.ToString("B")), false))
            {
                if (key == null)
                    return null;

                var obj = key.GetValue("IconPath");
                if (!(obj is string path))
                {
                    path = (obj as string[])?[0];
                }
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                return ShellUtilities.ParseIconLocationPath(path, out filePath);
            }
        }

        internal static string NormalizeIconPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (path.IndexOf("%") >= 0) // ex: %SystemRoot%\system32\setupapi.dll"
                return Environment.ExpandEnvironmentVariables(path);

            return path;
        }

        // not sure we can get these information from WinRT...
        [DllImport("setupapi", CharSet = CharSet.Unicode)]
        private static extern int SetupDiGetClassDescription([MarshalAs(UnmanagedType.LPStruct)] Guid ClassGuid, StringBuilder ClassDescription, int ClassDescriptionSize, ref int RequiredSize);

        [DllImport("setupapi.dll")]
        private static extern int SetupDiLoadClassIcon([MarshalAs(UnmanagedType.LPStruct)] Guid ClassGuid, out IntPtr LargeIcon, IntPtr MiniIconIndex);
    }
}
