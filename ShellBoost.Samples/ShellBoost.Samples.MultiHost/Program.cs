using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.Client;

namespace ShellBoost.Samples.MultiHost
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("ShellBoost Samples - MultiHost - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine();

            // browse all ShellBoost native proxies in the current directory (corresponding to the current bitness)
            var stopEvents = new List<AutoResetEvent>();
            foreach (var proxyDllPath in Directory.GetFiles(".", "*." + Installer.GetBitnessString(ProcessorArchitecture.None) + ".dll"))
            {
                Task.Run(() =>
                {
                    // foreach proxy found, start a ShellFolderServer in its own AppDomain
                    var domain = AppDomain.CreateDomain(Path.GetFileNameWithoutExtension(proxyDllPath));
                    var server = (ShellFolderServerHost)domain.CreateInstanceAndUnwrap(typeof(ShellFolderServerHost).Assembly.FullName, typeof(ShellFolderServerHost).FullName);
                    var stopEvent = new AutoResetEvent(false);
                    stopEvents.Add(stopEvent);
                    server.Main(stopEvent, proxyDllPath);
                });
            }

            Console.WriteLine("Press any key to stop all extensions.");
            Console.ReadKey(true);

            // ask each extension to stop
            foreach (var stop in stopEvents)
            {
                stop.Set();
            }

            // wait for all
            WaitHandle.WaitAll(stopEvents.ToArray());
        }
    }

    // One instance of this class will run per AppDomain
    public class ShellFolderServerHost: MarshalByRefObject
    {
#pragma warning disable CA1822 // Mark members as static
        public void Main(EventWaitHandle stop, string nativeDllPath)
#pragma warning restore CA1822 // Mark members as static
        {
            using (var server = new OverviewShellFolderServer())
            {
                var config = new ShellFolderConfiguration();
                config.NativeDllRegistration = RegistrationMode.User; // registration can also be done outside of ShellBoost (regsvr32, etc.)
                config.NativeDllPath = nativeDllPath;

                server.Licensing += OnLicensing;
                Log("Starting listening");
                server.Start(config);
                Log("Started listening '" + ShellFolderServer.DisplayName + "' on proxy id " + ShellFolderServer.ProxyId + " Trace id " + ShellFolderServer.TraceId + ".");
                stop.WaitOne();

                // stop this instance, but keep global RPC listening
                server.Stop(ServerStopOptions.WaitForCallsToComplete | ServerStopOptions.DontStopListening);
                Log("Stopped listening");
                stop.Set();
            }
        }

        // we want to know what's the current AppDomain when we log something
        private static void Log(object message) => Console.WriteLine(AppDomain.CurrentDomain.FriendlyName + ":" + message);

        private static void OnLicensing(object sender, LicensingEventArgs e)
        {
            Log("LicenseDataIsValid: " + ShellFolderServer.LicenseDataIsValid);
            Log("LicenseExpirationDate: " + ShellFolderServer.LicenseExpirationDate);
            Log("LicenseRegisteredCompany: " + ShellFolderServer.LicenseRegisteredCompany);
        }
    }
}
