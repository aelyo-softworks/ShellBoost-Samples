using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public class Settings : Serializable<Settings>
    {
        private int _syncPeriod;

        public static FileSystem FileSystem { get; }
        public static OnDemandSynchronizer Synchronizer { get; private set; }
        public static string ConfigurationDirectoryPath { get; }
        public static string DataDirectoryPath { get; }
        public static string SecretsFilePath { get; }
        public static string ConfigurationFilePath { get; }
        public static string ConfigurationBackupDirectoryPath { get; }
        public static Settings Current { get; }

        static Settings()
        {
            FileSystem = new FileSystem();

            // configuration files are stored in %localappdata%
            ConfigurationDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), typeof(Program).Namespace);

            // data is stored in user's Documents
            DataDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), typeof(Program).Namespace);
            SecretsFilePath = Path.Combine(ConfigurationDirectoryPath, "secrets.json");
            ConfigurationFilePath = Path.Combine(ConfigurationDirectoryPath, "config.xml");
            ConfigurationBackupDirectoryPath = Path.Combine(Path.GetDirectoryName(ConfigurationFilePath), BackupDirectoryName);
            Current = DeserializeFromConfiguration();
        }

        public Settings()
        {
            SyncPeriod = 300;
            SyncAutoStart = true;
            AddAccountClearCookies = true;
            GoogleApisLogLevel = TraceLevel.Info;
            SynchronizerLogLevel = TraceLevel.Verbose;
        }

        public static bool HasSecretsFile => IOUtilities.FileExists(SecretsFilePath);

        public static void BackupFromConfiguration(TimeSpan? maxDuration = null) => Backup(ConfigurationFilePath, maxDuration);
        public static void RemoveAllConfiguration() => RemoveAll(ConfigurationFilePath);
        public static Settings DeserializeFromConfiguration() => Deserialize(ConfigurationFilePath);
        public void SerializeToConfiguration() => Serialize(ConfigurationFilePath);

        public void BackupAndSerialize()
        {
            BackupFromConfiguration(new TimeSpan(30, 0, 0, 0)); // keep backups for ~ a month
            SerializeToConfiguration();
        }

        public static void StartSynchronizer(ILogger logger = null)
        {
            if (Synchronizer == null)
            {
                RegisterOnDemandSynchronizer();
                Synchronizer = new OnDemandSynchronizer(DataDirectoryPath, FileSystem);

            }

            Synchronizer.Logger = logger;
            Synchronizer.Synchronizing += OnSynchronizing;
            Synchronizer.SyncPeriod = Current.SyncPeriod;
            Synchronizer.Synchronize();
        }

        // we handle some synchronizer events in a special way for this sample
        private static void OnSynchronizing(object sender, OnDemandSynchronizerEventArgs e)
        {
            if (e.Type == OnDemandSynchronizerEventType.SynchronizingAll)
            {
                // we couple the synchronizer with Google Drive change tracking mechanism
                foreach (var account in FileSystem.Accounts)
                {
                    account.SynchronizeChanges();
                }
                return;
            }

            // make sure we never delete a folder under root that's not associated with an account
            if (e.Type == OnDemandSynchronizerEventType.EnumeratingLocalResources)
            {
                var localDirectoryFullPath = (string)e.Input["localDirectoryFullPath"];
                if (!localDirectoryFullPath.EqualsIgnoreCase(DataDirectoryPath))
                    return;

                var list = new List<string>();

                // contrary to the standard .NET classes, this ShellBoost utility method just skip locked files or file that would otherwise cause errors
                foreach (var entry in Win32FindData.EnumerateFileSystemEntries(localDirectoryFullPath))
                {
                    // if a folder there doesn't match a valid account, we skip it
                    // so the synchronizer just won't know about it and won't try to delete it
                    if (!Account.IsDirectoryAnAccount(Path.GetFileName(entry.FullName)))
                    {
#if RELEASE
                        Synchronizer.Logger?.Log(TraceLevel.Info, "Found a directory '" + entry.FullName + "' that's not associated with a valid account.");
#endif
                        continue;
                    }

                    list.Add(entry.FullName);
                }

                e.Output["resources"] = list;
                e.Handled = true;
                return;
            }
        }

        public static void RegisterOnDemandSynchronizer()
        {
            // ensure the local data directory exists
            if (!IOUtilities.DirectoryExists(DataDirectoryPath))
            {
                Directory.CreateDirectory(DataDirectoryPath);
            }
            OnDemandSynchronizer.EnsureRegistered(DataDirectoryPath, GetRegistration());
        }

        // customize registration to give a nice name to the cloud provider we represent
        private static OnDemandRegistration GetRegistration()
        {
            var reg = new OnDemandRegistration();
            reg.ProviderName = AssemblyUtilities.GetDescription();
            return reg;
        }

        public static void UnregisterOnDemandSynchronizer()
        {
            if (IOUtilities.DirectoryExists(DataDirectoryPath))
            {
                OnDemandSynchronizer.Unregister(DataDirectoryPath, GetRegistration());
            }
        }

        // the application end-user settings
        [Category("On-Demand Synchronizer")]
        [DisplayName("Synchronization Period (sec)")]
        [DefaultValue(300)]
        public int SyncPeriod
        {
            get => _syncPeriod;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                _syncPeriod = value;
            }
        }

        [Category("On-Demand Synchronizer")]
        [DisplayName("Start Synchronization Automatically")]
        [DefaultValue(true)]
        public bool SyncAutoStart { get; set; }

        [Category("Google Sign-In")]
        [DisplayName("Clear cookies when adding an Account")]
        [DefaultValue(true)]
        public bool AddAccountClearCookies { get; set; }

        [Category("Logging")]
        [DisplayName("Google Apis Log Level")]
        [DefaultValue(TraceLevel.Info)]
        public TraceLevel GoogleApisLogLevel { get; set; }

        [Category("Logging")]
        [DisplayName("Synchronizer Log Level")]
        [DefaultValue(TraceLevel.Verbose)]
        public TraceLevel SynchronizerLogLevel { get; set; }
    }
}
