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
            Console.WriteLine("ShellBoost Samples - Cloud Folder Sync - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit - Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            Console.WriteLine("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());
            Console.WriteLine("Windows Version " + Environment.OSVersion.VersionString);
            Console.WriteLine("Windows Kernel Version " + WindowsUtilities.KernelVersion);
            Console.WriteLine("Windows Filter Driver Version " + OnDemandLocalFileSystem.FilterDriverVersion);
            Console.WriteLine();

            var fl = new FileLogger();
            WebApi.Logger = fl;

            var id = WebApi.ServerInfo.Id; // this will connect to server, if it fails here, check connections, server, etc.
            WebApi.Logger.Log(TraceLevel.Info, "Log file path: " + fl.FilePath);
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

            // thumbnails without hydration support
            var comServer = new ComLocalServer();

            var reg = new OnDemandLocalFileSystemRegistration
            {
                ProviderName = "Samples.CloudFolder",
                ProviderDisplayName = DisplayName,
                HydrationPolicy = OnDemandHydrationPolicy.Full,
                HydrationPolicyModifier = OnDemandHydrationPolicyModifier.ValidationRequired | OnDemandHydrationPolicyModifier.AutoDehydrationAllowed,
                PopulationPolicy = OnDemandPopulationPolicy.AlwaysFull,

                // indicate the CLSID of the thumbnail provider COM object
                ThumbnailProviderClsid = typeof(CloudFolderThumbnailProvider).GUID,
            };

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
                    // register thumbnail provider COM object in registry
                    CloudFolderThumbnailProvider.Register();

                    // register thumbnail provider COM object as a service
                    // we want a custom factory since we want to pass context information to the class
                    var factory = new ComClassFactory<CloudFolderThumbnailProvider>();
                    comServer.RegisterClassObject<CloudFolderThumbnailProvider>(factory: factory);

                    // register to the Windows file system as a storage provider
                    OnDemandLocalFileSystem.EnsureRegistered(folderPath, reg);

                    // register to the Windows Shell (explorer treeview) as a storage provider
                    ShellRegistration.RegisterCloudStorageProvider(folderPath, DisplayName, id);

                    // create the synchronizer
                    // note: by default, SQLite state database will be created at %localappdata%\ShellBoost.Core.Synchronization\<ShellBoost Project Id> directory
                    using (var mp = new MultiPointSynchronizer(Name + "." + id.ToString(), options: options))
                    {
                        // this our file system implementation
                        var cloud = new CloudFolderFileSystem(options.Logger);

                        // this is Windows 10+ files on-demand local file system (ShellBoost provided)
                        var local = new OnDemandLocalFileSystem(folderPath, new OnDemandLocalFileSystemOptions
                        {
                            SynchronizationStateEndPointSynchronizerIdentifiers = { "*" }, // make sure a local file is in-sync only when the corresponding file is in-sync on Cloud server (* means all other endpoints)
                        });

                        // pass the OnDemandLocalFileSystem instance to the thumbnail provider
                        factory.CreateInstance += (s, e) =>
                        {
                            e.Instance = new CloudFolderThumbnailProvider(local, cloud);
                        };

                        // add sync endpoints
                        mp.AddEndPoint("Local", local);

                        // we prefer to put each temporary (upload) file in the target folder
                        // instead of using a temp folder like with the Google Drive sample
                        mp.AddEndPoint(Name, cloud, new EndPointSynchronizerOptions { UploadsWaitForParents = true });

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
                    // unregister thumbnail provider COM object from registry
                    CloudFolderThumbnailProvider.Unregister();

                    // remove ourselves from the explorer treeview
                    ShellRegistration.UnregisterCloudStorageProvider(id);

                    // unregister to the Windows file system as a storage provider
                    OnDemandLocalFileSystem.Unregister(folderPath, reg);
                    Console.WriteLine(" Cloud Storage Provider has been unregistered.");
                    break;
            }

            comServer.Dispose();
        }
    }
}
