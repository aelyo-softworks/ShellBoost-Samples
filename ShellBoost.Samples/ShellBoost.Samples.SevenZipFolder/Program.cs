using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using SevenZip;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.SevenZipFolder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ShellBoost Samples - SevenZip Folder - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine();

            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the native proxy, file associations, and run this sample.");
            Console.WriteLine("   '2' Unregister the native proxy and file associations.");
            Console.WriteLine("   '3' Restart Windows Explorer.");
            Console.WriteLine();
            Console.WriteLine("   Any other key will exit.");
            Console.WriteLine();
            var key = Console.ReadKey(true);
            switch (key.KeyChar)
            {
                case '1':
                    Run();
                    break;

                case '2':
                    ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
                    UnregisterAsVirtualFolder(".7z");
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

        static void Run()
        {
            Console.WriteLine("ShellBoost - Samples - SevenZip Folder - Copyright (C) 2018-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine(new DiagnosticsInformation(assembly: null).AssemblyDisplayName);
            Console.WriteLine();
            if (CommandLine.HelpRequested)
            {
                Help();
                return;
            }

            SetSevenZipLibraryPath();
            using (var server = new SevenZipShellFolderServer())
            {
                var config = new ShellFolderConfiguration();
                config.NativeDllRegistration = RegistrationMode.User;
#if DEBUG
                config.Logger = new ConsoleLogger { AddThreadId = true };
#endif
                server.Start(config);

                RegisterAsVirtualFolder(".7z", ShellFolderServer.FolderId);

                Console.WriteLine("Started listening on proxy id " + ShellFolderServer.ProxyId + ". Press ESC key to stop serving 7z files.");
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

        // you must install 7-Zip for this sample to work
        // this is a utility method to set 7z dll path
        // make sure you compile "any cpu" to ease this out
        public static void SetSevenZipLibraryPath()
        {
            string path = null;
            using (var reg = Registry.LocalMachine.OpenSubKey(@"Software\7-Zip", false))
            {
                if (reg != null)
                {
                    path = (string)(IntPtr.Size == 4 ? reg.GetValue("Path") : reg.GetValue("Path64"));
                }
            }

            path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip");
            path = Path.Combine(path, "7z.dll");
            if (File.Exists(path))
            {
                SevenZipBase.SetLibraryPath(path);
            }
        }

        public static void UnregisterAsVirtualFolder(string extension)
        {
            const string classes = @"Software\Classes";
            Registry.CurrentUser.DeleteSubKeyTree(Path.Combine(classes, typeof(Program).Namespace), false);

            string progid = null;
            using (var key = Registry.CurrentUser.OpenSubKey(Path.Combine(classes, extension), true))
            {
                if (key != null)
                {
                    progid = (string)key.GetValue(null);
                    if (string.IsNullOrWhiteSpace(progid))
                        return;

                    if (progid.EqualsIgnoreCase(typeof(Program).Namespace))
                    {
                        key.DeleteValue(null, false);
                    }
                }
            }

            using (var key = Registry.CurrentUser.OpenSubKey(Path.Combine(classes, progid, "CLSID"), true))
            {
                RestoreStringFromBackup(key, null);
            }

            using (var key = Registry.CurrentUser.OpenSubKey(Path.Combine(classes, progid, "shell", "open", "command"), true))
            {
                RestoreStringFromBackup(key, null);
                RestoreStringFromBackup(key, "DelegateExecute");
            }
        }

        public static void RegisterAsVirtualFolder(string extension, Guid shellFolderClassId)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            const string classes = @"Software\Classes";
            string progid = null;
            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, Path.Combine(classes, extension)))
            {
                progid = (string)key.GetValue(null);
                if (string.IsNullOrWhiteSpace(progid))
                {
                    progid = typeof(Program).Namespace;
                    key.SetValue(null, progid);
                }
            }

            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, Path.Combine(classes, progid, "CLSID")))
            {
                SetStringKeyWithBackup(key, null, shellFolderClassId.ToString("B"));
            }

            using (var key = WindowsUtilities.EnsureSubKey(Registry.CurrentUser, Path.Combine(classes, progid, "shell", "open", "command")))
            {
                SetStringKeyWithBackup(key, null, @"%SystemRoot%\explorer.exe /idlist,%I,%L");

                // this is CLSID_ExecuteFolder from shobjidl.h
                var CLSID_ExecuteFolder = new Guid("11dbb47c-a525-400b-9e80-a54615a090c0");
                SetStringKeyWithBackup(key, "DelegateExecute", CLSID_ExecuteFolder.ToString("B"));
            }
        }

        static void SetStringKeyWithBackup(RegistryKey key, string name, string value)
        {
            var oldValue = key.GetValue(name) as string;
            if (oldValue == value)
                return;

            if (oldValue != null)
            {
                key.SetValue(typeof(Program).Namespace + name, oldValue);
            }
            key.SetValue(name, value);
        }

        static void RestoreStringFromBackup(RegistryKey key, string name)
        {
            if (key == null)
                return;

            var value = key.GetValue(typeof(Program).Namespace + name) as string;
            if (value != null)
            {
                key.SetValue(name, value);
            }
            key.DeleteValue(typeof(Program).Namespace + name, false);
        }

        static void Help()
        {
            Console.WriteLine(Assembly.GetEntryAssembly().GetName().Name.ToUpperInvariant());
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("    This sample server demonstrates how to serve .7z files.");
            Console.WriteLine();
        }
    }
}
