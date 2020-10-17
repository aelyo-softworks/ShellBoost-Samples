using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.DeviceManagerFolder.UI;
using Windows.Devices.Enumeration;

namespace ShellBoost.Samples.DeviceManagerFolder
{
    public partial class DeviceManagerFolderServer : ShellFolderServer
    {
        private readonly object _lock = new object();
        private readonly List<DeviceInformation> _added = new List<DeviceInformation>();
        private readonly List<DeviceInformationUpdate> _removed = new List<DeviceInformationUpdate>();
        private readonly List<DeviceInformationUpdate> _updated = new List<DeviceInformationUpdate>();

        private DeviceManagerFolder _root;

        public DeviceManagerFolderServer()
        {
            Settings = new Settings();
            Settings.PropertyChanged += OnSettingsChanged;
            DeviceInterfaceWatcher = DeviceInformation.CreateWatcher(string.Empty, Array.Empty<string>(), DeviceInformationKind.DeviceInterface);
            DeviceInterfaceWatcher.Added += OnDeviceAdded;
            DeviceInterfaceWatcher.Removed += OnDeviceRemoved;
            DeviceInterfaceWatcher.Updated += OnDeviceUpdated;
            DeviceInterfaceWatcher.Start();
        }

        public DeviceWatcher DeviceInterfaceWatcher { get; }
        public Settings Settings { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DeviceInterfaceWatcher?.Stop();
            }
            base.Dispose(disposing);
        }

        protected override ShellFolder GetFolderAsRoot(ShellItemIdList idList)
        {
            if (_root == null)
            {
                lock (_lock)
                {
                    // now, we're creating the root folder, so we add all previous actions to it
                    _root = new DeviceManagerFolder(idList);

                    var deviceInterfaces = _added.ToArray();
                    _added.Clear();

                    var updates = _updated.ToArray();
                    _updated.Clear();

                    var removed = _removed.ToArray();
                    _removed.Clear();

                    Task.Run(() =>
                    {
                        foreach (var deviceInterface in deviceInterfaces)
                        {
                            _ = _root.AddDeviceInterface(deviceInterface);
                        }

                        foreach (var update in updates)
                        {
                            _root.UpdateDeviceInterface(update);
                        }

                        foreach (var remove in removed)
                        {
                            _root.RemoveDeviceInterface(remove);
                        }
                    });
                }
            }
            return _root;
        }

        private void OnSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var ctx = ShellContext.Current.Clone(); // if we change threads, we must clone the context
            TaskUtilities.EnsureSTAThreadTask(async () =>
            {
                var view = await ctx.GetShellBoostViewAsync(_root);
                if (view != null)
                {
                    //view.FolderView.Flags = Core.WindowsShell.FOLDERFLAGS.
                }
            });
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Console.WriteLine("OnDeviceUpdated id:" + args.Id);
            if (_root == null) // folder not yet created
            {
                _updated.Add(args);
                return;
            }

            _root.UpdateDeviceInterface(args);
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Console.WriteLine("OnDeviceRemoved id:" + args.Id);
            if (_root == null) // folder not yet created
            {
                _removed.Add(args);
                return;
            }

            _root.RemoveDeviceInterface(args);
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            if (_root == null) // folder not yet created
            {
                _added.Add(args);
                return;
            }

            _ = _root.AddDeviceInterface(args);
        }
    }
}
