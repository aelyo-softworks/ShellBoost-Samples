using System;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            const string testProperty = "ShellBoost.Samples.LocalFolder.IconUI";
            string already = PropertySystem.GetPropertyDescription(testProperty, false) != null ? " (note: they are already registered)." : ".";

            Console.WriteLine("ShellBoost Samples - Local Folder - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());
            Console.WriteLine();
            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the native proxy, run this sample, and unregister on exit.");
            Console.WriteLine("   '2' Register the native proxy.");
            Console.WriteLine("   '3' Run this sample (the native proxy will need to be registered somehow for Explorer to display something).");
            Console.WriteLine("   '4' Unregister the native proxy.");
            Console.WriteLine("   '5' Restart Windows Explorer.");
            Console.WriteLine("   '6' Register Custom Properties for this sample" + already + (DiagnosticsInformation.GetTokenElevationType() == TokenElevationType.Full ? string.Empty : " You need to restart as admin."));
            Console.WriteLine("   '7' Unregister Custom Properties for this sample." + (DiagnosticsInformation.GetTokenElevationType() == TokenElevationType.Full ? string.Empty : " You need to restart as admin."));
            Console.WriteLine();
            Console.WriteLine("   Any other key will exit.");
            Console.WriteLine();

            string schemaLocation = new ShellFolderConfiguration().ExtractAssemblyResource(typeof(Program).Namespace + ".Resources.LocalFolder.propdesc");
            do
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                    return;

                switch (key.KeyChar)
                {
                    case '1':
                        Run(true);
                        ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
                        return;

                    case '2':
                        ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
                        Console.WriteLine("Registered");
                        break;

                    case '3':
                        Run(false);
                        return;

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
                        try
                        {
                            PropertySystem.RegisterPropertySchema(schemaLocation);
                            Console.WriteLine("Properties are registered. Schema location is: " + schemaLocation);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Properties cannot be registered: " + e.Message);
                        }

                        if (PropertySystem.GetPropertyDescription(testProperty, false) != null)
                        {
                            Console.WriteLine("Properties can be successfully retrieved from database.");
                        }

                        break;

                    case '7':
                        try
                        {
                            PropertySystem.UnregisterPropertySchema(schemaLocation);
                            Console.WriteLine("Properties are unregistered.");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Properties cannot be unregistered: " + e.Message);
                        }

                        if (PropertySystem.GetPropertyDescription(testProperty, false) != null)
                        {
                            Console.WriteLine("Properties can be successfully retrieved from database. They should not.");
                        }
                        break;
                }
            }
            while (true);
        }

        static void Run(bool register)
        {
            var info = new DirectoryInfo(Path.GetFullPath("Data"));
            if (!info.Exists)
            {
                info.Create();
                info.Refresh();
            }

            using (var server = new LocalShellFolderServer(info))
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

                var idl = ShellFolderServer.RootIdList;
                Console.WriteLine("Started listening on proxy id " + ShellFolderServer.ProxyId + ", folder id " + ShellFolderServer.FolderId + ". Press ESC key to stop serving folders.");
                Console.WriteLine("If you open Windows Explorer, you should now see the extension.");
                do
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                        break;

                    if (key.Key == ConsoleKey.C)
                    {
                        Console.Clear();
                        continue;
                    }
                }
                while (true);
                Console.WriteLine("Stopped");
            }
        }
    }
}
