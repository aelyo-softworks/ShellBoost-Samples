using System;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.Overview
{
    class Program
    {
        static void Main()
        {
            WindowsUtilities.AllocConsole(); // see https://github.com/dotnet/winforms/issues/4246
            // this source is used by two projects: Overview (.NET Framework) and CoreOverview (.NET Core 3+)
            Console.WriteLine("ShellBoost Samples - " + (Core.Client.Installer.IsNetCore? "Core" : null) + "Overview - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());
            Console.WriteLine();

            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the native proxy, run this sample, and unregister on exit.");
            Console.WriteLine("   '2' Register the native proxy.");
            Console.WriteLine("   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).");
            Console.WriteLine("   '4' Unregister the native proxy.");
            Console.WriteLine();
            Console.WriteLine("   Any other key will exit.");
            Console.WriteLine();
            var key = Console.ReadKey(true);
            switch (key.KeyChar)
            {
                case '1':
                    Run(true);
                    ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
                    break;

                case '2':
                    ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
                    Console.WriteLine("Registered");
                    break;

                case '3':
                    Run(false);
                    break;

                case '4':
                    ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
                    Console.WriteLine("Unregistered");
                    break;
            }
        }

        static void Run(bool register)
        {
            using (var server = new OverviewShellFolderServer())
            {
                var config = new ShellFolderConfiguration();
                if (register)
                {
                    config.NativeDllRegistration = RegistrationMode.User;
                }

#if DEBUG
                config.Logger = new Core.Utilities.ConsoleLogger();
#endif
                server.Licensing += OnLicensing;

                server.Start(config);
                Console.WriteLine("Started listening on proxy id " + ShellFolderServer.ProxyId + ". Press ESC key to stop serving folders.");
                Console.WriteLine("If you open Windows Explorer and have registered, you should now see the extension.");
                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                }
                Console.WriteLine("Stopped");
            }
        }

        private static void OnLicensing(object sender, LicensingEventArgs e)
        {
            Console.WriteLine("LicenseDataIsValid: " + ShellFolderServer.LicenseDataIsValid);
            Console.WriteLine("LicenseExpirationDate: " + ShellFolderServer.LicenseExpirationDate);
            Console.WriteLine("LicenseRegisteredCompany: " + ShellFolderServer.LicenseRegisteredCompany);
        }
    }
}
