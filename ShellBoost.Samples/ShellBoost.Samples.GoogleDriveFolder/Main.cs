using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public partial class Main : Form
    {
        private ILogger _apisLogger;
        private ILogger _syncLogger;

        public Main()
        {
            InitializeComponent();
            Icon = Program.AppIcon;

            AddLog("ShellBoost Samples - Drive Local Folder - V" + AssemblyUtilities.GetInformationalVersion() + " Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            AddLog("ShellBoost Runtime Version " + typeof(ShellContext).Assembly.GetInformationalVersion());

            if (!Settings.HasSecretsFile)
            {
                AddLog();
                AddLog("! Google Drive secrets file is not present. Please select the 'File / Configure Secrets File' menu item to install it.");
                return;
            }

            if (!OnDemandLocalFileSystem.IsSupported)
            {
                AddLog();
                AddLog("! File On-Demand technology is not available on this version of Windows. You need Windows 10, version 1709 or higher.");
                return;
            }

            AddLog();
        }

        private void SynchronizeNow()
        {
            if (!Settings.HasSecretsFile)
                return;

            if (!OnDemandLocalFileSystem.IsSupported)
                return;

            foreach (var account in Settings.Current.Accounts)
            {
                account.FileSystem.Synchronize();
                account.Synchronizer.DoRunJobs();
            }
        }

        private void StartSynchronization()
        {
            if (Settings.Current.Accounts.Count == 0)
                return;

            foreach (var account in Settings.Current.Accounts)
            {
                account.Synchronizer.Start();
            }
            AddLog("Synchronization is started.");
        }

        private void StopSynchronization()
        {
            if (Settings.Current.Accounts.Count == 0)
                return;

            foreach (var account in Settings.Current.Accounts)
            {
                account.Synchronizer.Stop();
            }
            AddLog("Synchronization is stopped.");
        }

        private void ResetSynchronization()
        {
            if (Settings.Current.Accounts.Count == 0)
                return;

            foreach (var account in Settings.Current.Accounts)
            {
                account.Synchronizer.Stop(1000);
                account.Synchronizer.StateProvider.Reset();
                account.ResetLastStartPageToken();
            }
            AddLog("Synchronization is reset.");
        }

        protected override void OnClosed(EventArgs e) => Settings.Current.ResetAccounts();
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _apisLogger = new MainLogger(this, "GoogleApis", Settings.Current.GoogleApisLogLevel);
            _syncLogger = new MainLogger(this, "MPSync", Settings.Current.SynchronizerLogLevel);
            Settings.SynchronizerLogger = _syncLogger;
            Account.Logger = _apisLogger;
            ListAccounts();
            if (Settings.Current.SyncAutoStart)
            {
                StartSynchronization();
            }
        }

        public void InvokeAddLog(string text = null, bool addDate = false) => BeginInvoke(new Action(() => AddLog(text, addDate)));
        public void AddLog(string text = null, bool addDate = false)
        {
            if (text == null)
            {
                textBoxMain.AppendText(Environment.NewLine);
                return;
            }

            if (addDate)
            {
                textBoxMain.AppendText(DateTime.Now + ": " + text + Environment.NewLine);
            }
            else
            {
                textBoxMain.AppendText(text + Environment.NewLine);
            }
        }

        private void ListAccountsToolStripMenuItem_Click(object sender, EventArgs e) => ListAccounts();
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e) => Close();
        private void StartSynchronizerToolStripMenuItem_Click(object sender, EventArgs e) => StartSynchronization();
        private void StopSynchronizerToolStripMenuItem_Click(object sender, EventArgs e) => StopSynchronization();
        private void SynchronizeNowToolStripMenuItem_Click(object sender, EventArgs e) => SynchronizeNow();
        private void ClearLogToolStripMenuItem_Click(object sender, EventArgs e) => textBoxMain.Clear();
        private void ClearAllToolStripMenuItem_Click(object sender, EventArgs e) => textBoxMain.Clear();
        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutBox())
            {
                dlg.ShowDialog(this);
            }
        }

        private void InstallSecretsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.HasSecretsFile)
            {
                if (this.ShowConfirm("There is already a secrets file configured. Do you want to overwrite it?") != DialogResult.Yes)
                    return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Choose secrets file to use";
                ofd.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                ofd.DefaultExt = ".json";
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.Multiselect = false;
                ofd.ValidateNames = true;
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                IOUtilities.FileOverwrite(ofd.FileName, Settings.SecretsFilePath, true);
            }

            this.ShowMessage("Secrets file has been configured successfully.");
        }

        private void PreferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new SettingsBox(Settings.Current))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Current.BackupAndSerialize();
                }
            }
        }

        private void AddAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var about = Account.AddAccount();
                if (about == null)
                    return;

                AddLog("Account for '" + about.User.EmailAddress + "' has been added.");

                var account = Settings.Current.GetAccount(about.User.EmailAddress);
                if (account != null)
                {
                    account.Synchronizer.Start();
                }
            }
            catch (Exception ex)
            {
                if (ex.IsCanceledTask())
                    return;

                AddLog("An error occurred: " + ex.GetAllMessages());
            }
        }

        private void ListAccounts()
        {
            AddLog();
            AddLog(Settings.Current.Accounts.Count + " valid account(s) where found.");
            for (int i = 0; i < Settings.Current.Accounts.Count; i++)
            {
                var account = Settings.Current.Accounts[i];
                AddLog("Account[" + i + "]");
                AddLog(" User Email Address           : " + account.UserEmailAddress);
                AddLog(" Data Directory Path          : " + account.DataDirectoryPath);
                AddLog(" File System RootId           : " + account.FileSystem.RootId);
                AddLog(" Synchronizer Started         : " + account.Synchronizer.IsStarted);
                if (account.About != null)
                {
                    AddLog(" User Display Name            : " + account.About.User.DisplayName);
                    if (account.About.MaxUploadSize.HasValue)
                    {
                        AddLog(" Max Upload Size              : " + account.About.MaxUploadSize + " bytes (" + ShellUtilities.FormatByteSize(account.About.MaxUploadSize.Value) + ")");
                    }

                    if (account.About.StorageQuota != null)
                    {
                        if (account.About.StorageQuota.Limit.HasValue)
                        {
                            AddLog(" Storage Quota Limit          : " + account.About.StorageQuota.Limit + " bytes (" + ShellUtilities.FormatByteSize(account.About.StorageQuota.Limit.Value) + ")");
                        }

                        if (account.About.StorageQuota.Usage.HasValue)
                        {
                            string ratio = null;
                            if (account.About.StorageQuota.Limit.HasValue && account.About.StorageQuota.Limit.Value != 0)
                            {
                                var percent = (double)account.About.StorageQuota.Usage.Value / account.About.StorageQuota.Limit.Value;
                                ratio = "(" + percent.ToString("P") + ")";
                            }

                            AddLog(" Storage Quota Usage          : " + account.About.StorageQuota.Usage + " bytes (" + ShellUtilities.FormatByteSize(account.About.StorageQuota.Usage.Value) + ")" + ratio);
                        }

                        if (account.About.StorageQuota.UsageInDrive.HasValue)
                        {
                            AddLog(" Storage Quota Usage In Drive : " + account.About.StorageQuota.UsageInDrive + " bytes (" + ShellUtilities.FormatByteSize(account.About.StorageQuota.UsageInDrive.Value) + ")");
                        }

                        if (account.About.StorageQuota.UsageInDrive.HasValue)
                        {
                            AddLog(" Storage Quota Usage In Trash : " + account.About.StorageQuota.UsageInDriveTrash + " bytes (" + ShellUtilities.FormatByteSize(account.About.StorageQuota.UsageInDriveTrash.Value) + ")");
                        }
                    }
                }
            }
        }

        private void FileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            addAccountToolStripMenuItem.Enabled = Settings.HasSecretsFile;
            listAccountsToolStripMenuItem.Enabled = Settings.HasSecretsFile;
            removeAConfiguredAccountToolStripMenuItem.Enabled = Settings.HasSecretsFile && Settings.Current.Accounts.Any();
        }

        private void SynchronizationToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            bool setupOk = OnDemandLocalFileSystem.IsSupported && Settings.HasSecretsFile;
            startSynchronizerToolStripMenuItem.Enabled = setupOk && !Settings.Current.IsSynchronizationStarted && Settings.Current.Accounts.Any();
            stopSynchronizerToolStripMenuItem.Enabled = setupOk && Settings.Current.IsSynchronizationStarted && Settings.Current.Accounts.Any();
            synchronizeNowToolStripMenuItem.Enabled = setupOk && Settings.Current.Accounts.Any();
        }

        private void ResetGoogleDriveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ShowConfirm("You are about to reset the synchronization information for all configured accounts. This could take some time depending on the number of folders and file it will impact." +
                Environment.NewLine + Environment.NewLine +
                "Are you sure you want to do this?") != DialogResult.Yes)
                return;

            ResetSynchronization();
        }

        private void OpenConfigurationBackupPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IOUtilities.DirectoryExists(Settings.ConfigurationBackupDirectoryPath))
            {
                WindowsUtilities.OpenExplorer(Settings.ConfigurationBackupDirectoryPath);
            }
        }

        private void DiagnosticsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            openConfigurationBackupPathToolStripMenuItem.Enabled = IOUtilities.DirectoryExists(Settings.ConfigurationBackupDirectoryPath);
            resetGoogleDriveStateToolStripMenuItem.Enabled = OnDemandLocalFileSystem.IsSupported && Settings.HasSecretsFile && Settings.Current.Accounts.Any();
        }

        private void OpenDataDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IOUtilities.DirectoryExists(Settings.DataDirectoryPath))
            {
                WindowsUtilities.OpenExplorer(Settings.DataDirectoryPath);
            }
        }

        private void OpenDataDirectoryToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            openDataDirectoryToolStripMenuItem.Enabled = IOUtilities.DirectoryExists(Settings.DataDirectoryPath);
            openLogsPathToolStripMenuItem.Enabled = IOUtilities.DirectoryExists(Settings.LogsDirectoryPath);
        }

        private void OpenLogsPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IOUtilities.DirectoryExists(Settings.LogsDirectoryPath))
            {
                WindowsUtilities.OpenExplorer(Settings.LogsDirectoryPath);
            }
        }

        private class MainLogger : ILogger
        {
            private readonly static SingleThreadTaskScheduler _scheduler;
            private readonly static string _logPath;

            static MainLogger()
            {
                _scheduler = new SingleThreadTaskScheduler((t) =>
                {
                    t.Name = string.Format("_gd_logger{0}", Environment.TickCount);
                    return true;
                });

                var name = string.Format("{1}_{0:yyyy}_{0:MM}_{0:dd}_{0:HHmmss}.log", DateTime.Now, Environment.MachineName);
                _logPath = Path.Combine(Settings.LogsDirectoryPath, name);
                IOUtilities.FileCreateDirectory(_logPath);
            }

            public MainLogger(Main main, string prefix, TraceLevel maxLevel = TraceLevel.Verbose)
            {
                Main = main;
                Prefix = prefix;
                MaxLevel = maxLevel;
            }

            public Main Main { get; }
            public string Prefix { get; }
            public TraceLevel MaxLevel { get; }

            public void Log(TraceLevel level, object value, string methodName)
            {
                if (level > MaxLevel)
                    return;

                if (methodName != null)
                {
                    methodName = ":" + methodName;
                }

                var threadName = Thread.CurrentThread.Name.Nullify() ?? Thread.CurrentThread.ManagedThreadId.ToString();
                var text = "(" + threadName + "):" + Prefix + methodName + ":" + level + ": " + value;
                Main.InvokeAddLog(text, true);

                Task.Factory.StartNew(() =>
                {
                    WriteLineToLog(DateTime.Now + ": " + text);
                }, CancellationToken.None, TaskCreationOptions.None, _scheduler);
            }

            private void WriteLineToLog(string text)
            {
                // we don't lock the file, we open it each time (note it's a less fast, especially when it gets bigger over time)
                using (var writer = new StreamWriter(_logPath, true, Encoding.UTF8))
                {
                    writer.WriteLine(text);
                    writer.Flush();
                }
            }
        }

        private void RemoveAConfiguredAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new ChooseAccount())
            {
                dlg.ShowDialog(this);
                var account = dlg.SelectedAccount;
                if (account != null)
                {
                    if (this.ShowConfirm("Are you sure you want to remove account '" + account.UserEmailAddress + "'?") != DialogResult.Yes)
                        return;

                    account.Synchronizer.Stop();
                    account.Synchronizer.StateProvider.Reset();
                    account.Remove();
                    account.UnregisterOnDemandSynchronizer();
                    IOUtilities.DirectoryDelete(account.DataDirectoryPath, true, false);
                    Settings.Current.ResetAccounts();
                    this.ShowMessage("Account '" + account.UserEmailAddress + "' was removed successfully.");
                }
            }
        }
    }
}
