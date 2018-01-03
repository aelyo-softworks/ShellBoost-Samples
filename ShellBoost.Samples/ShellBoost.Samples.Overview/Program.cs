using System;
using ShellBoost.Core;

namespace ShellBoost.Samples.Overview
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ShellBoost Samples - Overview - Copyright © 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
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
                server.Start(config);
                Console.WriteLine("Started listening on proxy id " + server.ProxyId + ". Press ESC key to stop serving folders.");
                Console.WriteLine("If you open Windows Explorer, you should now see the extension.");
                while (Console.ReadKey(true).Key != ConsoleKey.Escape)
                {
                }
                Console.WriteLine("Stopped");
            }
        }
    }
}
