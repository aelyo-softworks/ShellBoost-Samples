﻿using System;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ShellBoost Samples - Registry Folder - Copyright © 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine();

            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the native proxy, run this sample, and unregister on exit.");
            Console.WriteLine("   '2' Register the native proxy.");
            Console.WriteLine("   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).");
            Console.WriteLine("   '4' Unregister the native proxy.");
            Console.WriteLine("   '5' Restart Explorer.");
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

                case '5':
                    var rm = new RestartManager();
                    rm.RestartExplorerProcesses((state) =>
                    {
                        Console.WriteLine("Explorer was stopped. Press any key to restart it ...");
                        Console.ReadKey(true);
                    }, false, out Exception error);

                    if (error != null)
                    {
                        Console.WriteLine("An error has occurred in restart manager: " + error);
                    }
                    break;
            }
        }

        static void Run(bool register)
        {
            using (var server = new RegistryShellFolderServer())
            {
                var config = new ShellFolderConfiguration();
                if (register)
                {
                    config.NativeDllRegistration = RegistrationMode.User;
                }
#if DEBUG
                config.Logger = new ConsoleLogger { AddThreadId = true };
#endif

                if (DiagnosticsInformation.GetTokenElevationType() == TokenElevationType.Full)
                {
                    string location = config.ExtractAssemblyResource(typeof(Program).Namespace + ".Resources.RegistryFolder.propdesc");
                    PropertySystem.RegisterPropertySchema(location);
                    Console.WriteLine("Properties are registered. Schema location: " + location);
                }

                server.Start(config);
                Console.WriteLine("Started listening on proxy id " + server.ProxyId + ". Press ESC key to stop serving folders.");
                Console.WriteLine("If you open Windows Explorer, you should now see the extension.");
                do
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    if (key.Key == ConsoleKey.C)
                    {
                        Console.Clear();
                    }
                }
                while (true);
                Console.WriteLine("Stopped");
            }
        }
    }
}
