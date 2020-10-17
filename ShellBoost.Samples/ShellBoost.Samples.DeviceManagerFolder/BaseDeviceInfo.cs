//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Windows.Devices.Enumeration;

//namespace ShellBoost.Samples.DeviceManagerFolder
//{
//    // since we have to use some "late binding", we create an internal device representation
//    internal abstract class BaseDeviceInfo
//    {
//        public abstract string Id { get; }
//        public abstract DeviceInformationKind Kind { get; }
//        public abstract IReadOnlyDictionary<string, object> Properties { get; }
//        public string DeviceId => (string)Properties["System.Devices.DeviceInstanceId"];
//        public bool InterfaceEnabled => (bool)Properties["System.Devices.InterfaceEnabled"];

//        public override string ToString() => Id;

//        public async Task<DeviceInformation> GetDeviceAsync() => await DeviceInformation.CreateFromIdAsync(DeviceId, new string[] { "System.Devices.ClassGuid" }, DeviceInformationKind.Device);
//    }
//}
