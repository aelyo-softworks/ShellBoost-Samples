using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using Windows.Devices.Enumeration;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.DeviceManagerFolder
{
    public class DeviceFolder : ShellFolder
    {
        private readonly ConcurrentDictionary<string, DeviceInterfaceItem> _items = new ConcurrentDictionary<string, DeviceInterfaceItem>();

        public DeviceFolder(DeviceClassFolder parent, DeviceInformation device)
            : base(parent, new StringKeyShellItemId(device.Id))
        {
            Device = device;
            DisplayName = device.Name;

            // we start hidden and will show if any deviceinterface is visible
            Attributes |= SFGAO.SFGAO_HIDDEN;

            // remove default columns we don't need here
            RemoveColumn(Props.System.ItemType);
            RemoveColumn(Props.System.Size);
            RemoveColumn(Props.System.DateModified);
            RemoveColumn(Props.System.PerceivedType);
            RemoveColumn(Props.System.Kind);

            // add these ones, already defined by Windows
            AddColumn(Props.System.Devices.InterfaceClassGuid, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(Props.System.Devices.IsDefault, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(Props.System.Devices.InterfaceEnabled, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
            AddColumn(Props.System.Devices.ContainerId, SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT);
        }

        public DeviceInformation Device { get; }
        public new DeviceClassFolder Parent => (DeviceClassFolder)base.Parent;
        public new DeviceManagerFolder Root => (DeviceManagerFolder)base.Root;
        public new DeviceManagerFolderServer FolderServer => (DeviceManagerFolderServer)base.FolderServer;

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            if (!options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
                return Enumerable.Empty<ShellItem>();

            return _items.Values;
        }

        public override bool TryGetPropertyValue(Props.PropertyKey key, out object value)
        {
            if (key == Props.System.Devices.DeviceInstanceId)
            {
                value = Device.Id;
                return true;
            }

            if (key == Props.System.Devices.ClassGuid)
                return Device.Properties.TryGetValue("System.Devices.ClassGuid", out value);

            if (key == Props.System.Devices.ContainerId)
                return Device.Properties.TryGetValue("System.Devices.ContainerId", out value);

            return base.TryGetPropertyValue(key, out value);
        }

        internal void UpdateDeviceInterface(DeviceInformationUpdate deviceInterface)
        {
            // we're only interested by the enabled property
            if (!deviceInterface.Properties.TryGetValue("System.Devices.InterfaceEnabled", out var obj) || !(obj is bool enabled))
                return;

            if (!_items.TryGetValue(deviceInterface.Id, out var item))
                return;

            item.IsEnabled = enabled;
            item.NotifyUpdate();

            var atts = Attributes;
            var newAtts = atts;
            if (_items.Any(i => i.Value.IsEnabled))
            {
                newAtts &= ~SFGAO.SFGAO_HIDDEN;
            }
            else
            {
                newAtts |= SFGAO.SFGAO_HIDDEN;
            }
            
            if (newAtts != atts)
            {
                Attributes = newAtts;
                NotifyUpdate();
            }
        }

        internal bool RemoveDeviceInterface(DeviceInformationUpdate deviceInterface)
        {
            if (!_items.TryGetValue(deviceInterface.Id, out var item))
                return _items.Count > 0;

            if (_items.TryRemove(deviceInterface.Id, out _))
            {
                Root._deviceInterfaces.TryRemove(deviceInterface.Id, out _);
                item.NotifyDelete();
            }
            return _items.Count > 0;
        }

        internal void AddDeviceInterface(DeviceInformation deviceInterface)
        {
            var created = false;
            // creation time
            var newItem = _items.AddOrUpdate(deviceInterface.Id, (id) =>
            {
                created = true;
                return new DeviceInterfaceItem(this, deviceInterface);
            }, (k, o) => o);

            if (deviceInterface.IsEnabled)
            {
                Attributes &= ~SFGAO.SFGAO_HIDDEN;
            }

            // use the icon of first interface for device
            DeviceInterfaceItem.SetIcon(deviceInterface, (t) => Thumbnail = t);
            if (created)
            {
                newItem.NotifyCreate();
            }

            Root._deviceInterfaces[deviceInterface.Id] = newItem;
        }
    }
}
