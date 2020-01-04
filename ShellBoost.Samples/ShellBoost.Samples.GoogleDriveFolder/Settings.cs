using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public class Settings : Serializable<Settings>
    {
        private int _syncPeriod;
        private Lazy<IReadOnlyList<Account>> _accounts;

        public static string ConfigurationDirectoryPath { get; }
        public static string DataDirectoryPath { get; }
        public static string LogsDirectoryPath { get; }
        public static string SecretsFilePath { get; }
        public static string ConfigurationFilePath { get; }
        public static string ConfigurationBackupDirectoryPath { get; }
        public static Settings Current { get; }
        public static ILogger SynchronizerLogger { get; set; }

        static Settings()
        {
            // configuration files are stored in %localappdata%
            ConfigurationDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), typeof(Program).Namespace);
            LogsDirectoryPath = Path.Combine(ConfigurationDirectoryPath, "Logs");

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
            GoogleTempFolderName = _defaultGoogleTempFolderName;
            ResetAccounts();
        }

        internal void ResetAccounts()
        {
            if (_accounts != null && _accounts.IsValueCreated)
            {
                foreach (var account in _accounts.Value)
                {
                    account.Dispose();
                }
            }
            _accounts = new Lazy<IReadOnlyList<Account>>(LoadAccounts);
        }

        private IReadOnlyList<Account> LoadAccounts() => Account.GetAllAccounts(true).ToList();

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

        public Account GetAccount(string emailAddress) => Accounts.FirstOrDefault(a => a.UserEmailAddress.EqualsIgnoreCase(emailAddress));

        [XmlIgnore]
        [Browsable(false)]
        public bool IsSynchronizationStarted => Accounts.Any(a => a.Synchronizer.IsStarted);

        [XmlIgnore]
        [Browsable(false)]
        public IReadOnlyList<Account> Accounts => _accounts.Value;

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

        internal const string _defaultGoogleTempFolderName = "ShellBoostTemp";

        [Category("Google Drive")]
        [DisplayName("Temporary Files Folder Name")]
        [DefaultValue(_defaultGoogleTempFolderName)]
        public string GoogleTempFolderName { get; set; }

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
