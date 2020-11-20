using System;
using System.Diagnostics;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolder.Api;
using ShellBoost.Samples.CloudFolderSync.Utilities;

namespace ShellBoost.Samples.CloudFolderSync
{
    class Program
    {
        public const string Name = "CloudFolder";
        public const string DisplayName = "ShellBoost Cloud Folder";

        static void Main()
        {
            WindowsUtilities.AllocConsole();
            Console.WriteLine("ShellBoost Samples - Cloud Folder Sync - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());
            Console.WriteLine();

            WebApi.Logger = new FileLogger();

            var id = WebApi.ServerInfo.Id; // this will connect to server, if it fails here, check connections, server, etc.
            WebApi.Logger.Log(TraceLevel.Info, "Server root url: " + WebApi.RootUrl);
            WebApi.Logger.Log(TraceLevel.Info, "Server id: " + WebApi.ServerInfo.Id);
            WebApi.Logger.Log(TraceLevel.Info, "Server time diff: " + WebApi.ServerInfo.TimeDifference);
            if (WebApi.ServerInfo.TimeDifference.TotalMinutes > 1)
            {
                WebApi.Logger.Log(TraceLevel.Error, "Server time diff is a bit high!");
            }

            // determine a folder path
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Name);

            // we want events from the server (SignalR)
            var options = new MultiPointSynchronizerOptions();
            options.Logger = WebApi.Logger;

            WebApi.Logger.Log(TraceLevel.Info, "Local folder path: " + folderPath);
            options.BackupState = true; // we want to backup the state (state is by default an sqlite database)

            var reg = new OnDemandLocalFileSystemRegistration { ProviderDisplayName = DisplayName };

            Console.WriteLine();
            Console.WriteLine("Press a key:");
            Console.WriteLine();
            Console.WriteLine("   '1' Register the target folder as a Cloud Storage Provider and enter synchronization loop.");
            Console.WriteLine("   '2' Unregister the target folder as a Cloud Storage Provider. Only necessary when you've stopped all your tests.");
            Console.WriteLine();
            Console.WriteLine("Any other key will exit.");
            Console.WriteLine();
            var key = Console.ReadKey(true);
            switch (key.KeyChar)
            {
                case '1':
                    // register to the Windows file system as a storage provider
                    OnDemandLocalFileSystem.EnsureRegistered(folderPath, reg);

                    // register to the Windows Shell (explorer treeview) as a storage provider
                    ShellRegistration.RegisterCloudStorageProvider(folderPath, DisplayName, id);

                    // create the synchronizer
                    // note: by default, SQLite state database will be created at %localappdata%\ShellBoost.Core.Synchronization\<ShellBoost Project Id> directory
                    using (var mp = new MultiPointSynchronizer(Name + "." + id.ToString(), options: options))
                    {
                        // this our file system implementation
                        var fs = new CloudFolderFileSystem(options.Logger);

                        // this is Windows 10+ files on-demand local file system (ShellBoost provided)
                        var local = new OnDemandLocalFileSystem(folderPath, new OnDemandLocalFileSystemOptions { UpdateAllEntriesStatesAtInitialization = false });

                        // add sync endpoints
                        mp.AddEndPoint("Local", local);

                        // we prefer to put each temporary (upload) file in the target folder
                        // instead of using a temp folder like with the Google Drive sample
                        mp.AddEndPoint(Name, fs, new EndPointSynchronizerOptions { UploadsWaitForParents = true });

                        // start sync loop
                        mp.Start();
                        Console.WriteLine("Sync loop started. Press ESC to stop.");
                        do
                        {
                            key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Escape)
                                break;

                            // console output can be pretty big... press C to clear
                            if (key.Key == ConsoleKey.C)
                            {
                                Console.Clear();
                            }
                        }
                        while (true);

                        mp.Stop();
                    }
                    break;

                case '2':
                    // remove ourselves from the explorer treeview
                    ShellRegistration.UnregisterCloudStorageProvider(id);

                    // unregister to the Windows file system as a storage provider
                    OnDemandLocalFileSystem.Unregister(folderPath, reg);
                    Console.WriteLine(" Cloud Storage Provider has been unregistered.");
                    break;
            }
        }
    }
}
