using System;
using System.Linq;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using Windows.Devices.Enumeration;
using Props = ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.DeviceManagerFolder
{
    public class DeviceInterfaceItem : ShellItem
    {
        public DeviceInterfaceItem(DeviceFolder parent, DeviceInformation deviceInterface)
            : base(parent, new StringKeyShellItemId(deviceInterface.Id))
        {
            DeviceInterface = deviceInterface;
            DisplayName = deviceInterface.Name;
            IsEnabled = deviceInterface.IsEnabled;

            SetIcon(deviceInterface, (t) => Thumbnail = t);
        }

        public DeviceInformation DeviceInterface { get; }
        public new DeviceFolder Parent => (DeviceFolder)base.Parent;

        internal bool IsEnabled { get; set; }

        internal static void SetIcon(DeviceInformation deviceInterface, Action<ShellThumbnail> action)
        {
            if (deviceInterface.Properties.TryGetValue("System.Devices.Icon", out var value) && value is string path)
            {
                var iconIndex = ShellUtilities.ParseIconLocationPath(path, out var iconPath);
                if (iconPath != null)
                {
                    action(new ShellThumbnail(DeviceClassFolder.NormalizeIconPath(iconPath), iconIndex));
                }
            }
        }

        public override bool TryGetPropertyValue(Props.PropertyKey key, out object value)
        {
            if (key == Props.System.Devices.InterfaceClassGuid)
            {
                value = DeviceInterface.Id;
                return true;
            }

            if (key == Props.System.Devices.IsDefault)
            {
                value = DeviceInterface.IsDefault;
                return true;
            }

            if (key == Props.System.Devices.InterfaceEnabled)
            {
                value = DeviceInterface.IsEnabled;
                return true;
            }

            if (key == Props.System.Devices.ContainerId)
                return DeviceInterface.Properties.TryGetValue("System.Devices.ContainerId", out value);

            return base.TryGetPropertyValue(key, out value);
        }
    }
}
