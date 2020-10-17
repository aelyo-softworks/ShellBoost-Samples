using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Windows.Devices.Enumeration;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.DeviceManagerFolder
{
    public class DeviceManagerFolder : ShellFolder
    {
        // we must keep a list of all interfaces because updates don't carry all informations
        internal readonly ConcurrentDictionary<string, DeviceInterfaceItem> _deviceInterfaces = new ConcurrentDictionary<string, DeviceInterfaceItem>();
        private readonly ConcurrentDictionary<Guid, DeviceClassFolder> _deviceClassFolders = new ConcurrentDictionary<Guid, DeviceClassFolder>();

        public DeviceManagerFolder(ShellItemIdList idList)
            : base(idList)
        {
            RemoveColumn(Props.System.ItemType);
            RemoveColumn(Props.System.Size);
            RemoveColumn(Props.System.DateModified);
            RemoveColumn(Props.System.PerceivedType);
            RemoveColumn(Props.System.Kind);
            AddColumn(Props.System.Devices.ClassGuid, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        public new DeviceManagerFolderServer FolderServer => (DeviceManagerFolderServer)base.FolderServer;

        internal void UpdateDeviceInterface(DeviceInformationUpdate deviceInterface)
        {
            if (!_deviceInterfaces.TryGetValue(deviceInterface.Id, out var item))
                return;

            item.Parent.UpdateDeviceInterface(deviceInterface);
        }

        internal void RemoveDeviceInterface(DeviceInformationUpdate deviceInterface)
        {
            if (!_deviceInterfaces.TryGetValue(deviceInterface.Id, out var item))
                return;

            item.Parent.RemoveDeviceInterface(deviceInterface);
        }

        private async Task<DeviceInformation> GetDeviceAsync(DeviceInformation info) => await DeviceInformation.CreateFromIdAsync((string)info.Properties["System.Devices.DeviceInstanceId"], new string[] { "System.Devices.ClassGuid" }, DeviceInformationKind.Device);

        internal async Task AddDeviceInterface(DeviceInformation deviceInterface)
        {
            // add a class folder
            var device = await GetDeviceAsync(deviceInterface);
            var classGuid = (Guid)device.Properties["System.Devices.ClassGuid"];

            var created = false;
            var deviceClassFolder = _deviceClassFolders.AddOrUpdate(classGuid, (g) =>
            {
                created = true;
                return new DeviceClassFolder(this, g);
            }, (k, o) =>
            {
                return o;
            });

            // add the device (and interfaces) to the class folder
            deviceClassFolder.AddDeviceInterface(device, deviceInterface);

            if (created)
            {
                deviceClassFolder.NotifyCreate();
            }
            else
            {
                deviceClassFolder.NotifyUpdate();
            }
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if (!options.HasFlag(SHCONTF.SHCONTF_FOLDERS))
                return Enumerable.Empty<ShellItem>();

            return _deviceClassFolders.Values;
        }
    }
}
