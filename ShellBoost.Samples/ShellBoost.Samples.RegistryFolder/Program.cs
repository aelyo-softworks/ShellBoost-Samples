using System;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.RegistryFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ShellBoost Samples - Registry Folder - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());
            Console.WriteLine();

            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the native proxy, run this sample, and unregister on exit.");
            Console.WriteLine("   '2' Register the native proxy.");
            Console.WriteLine("   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).");
            Console.WriteLine("   '4' Unregister the native proxy.");
            Console.WriteLine("   '5' Restart Windows Explorer.");
            Console.WriteLine("   '6' Register Custom Properties for this sample." + (DiagnosticsInformation.GetTokenElevationType() == TokenElevationType.Full ? string.Empty : " You need to restart as admin."));
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

                case '6':
                    string location = new ShellFolderConfiguration().ExtractAssemblyResource(typeof(Program).Namespace + ".Resources.RegistryFolder.propdesc");
                    try
                    {
                        PropertySystem.RegisterPropertySchema(location);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Properties cannot be registered: " + e.Message);
                        break;
                    }
                    Console.WriteLine("Properties are registered. Schema location: " + location);
                    break;
            }
        }

        static void Run(bool register)
        {
            // we may display some UI, let's do like regular winforms apps do
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var server = new RegistryShellFolderServer())
            {
                var config = new ShellFolderConfiguration();
                if (register)
                {
                    config.NativeDllRegistration = RegistrationMode.User;
                }
#if DEBUG
                // add a logger that outputs to current console
                config.Logger = new ConsoleLogger { AddThreadId = true };
#endif

                server.Start(config);
                Console.WriteLine("Started listening on proxy id " + ShellFolderServer.ProxyId + ". Press ESC key to stop serving folders.");
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
