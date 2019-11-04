using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public partial class Main : Form
    {
        private ILogger _apisLogger;

        public Main()
        {
            InitializeComponent();
            Icon = Program.AppIcon;

            AddLog("ShellBoost Samples - Drive Local Folder - V" + AssemblyUtilities.GetInformationalVersion() + " Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");

            if (!Settings.HasSecretsFile)
            {
                AddLog();
                AddLog("! Google Drive secrets file is not present. Please select the 'File / Configure Secrets File' menu item to install it.");
                return;
            }

            if (!OnDemandSynchronizer.IsSupported)
            {
                AddLog();
                AddLog("! File On-Demand technology is not available on this version of Windows. You need Windows 10, version 1709 or higher.");
                return;
            }

            AddLog();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _apisLogger = new MainLogger(this, "GoogleApis", Settings.Current.GoogleApisLogLevel);
            Account.Logger = _apisLogger;
            if (Settings.Current.SyncAutoStart)
            {
                SynchronizeNow();
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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutBox())
            {
                dlg.ShowDialog(this);
            }
        }

        private void installSecretsToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dlg = new SettingsBox(Settings.Current))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Settings.Current.BackupAndSerialize();
                }
            }
        }

        private void addAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var about = Account.AddAccount();
                if (about == null)
                    return;

                AddLog("Account for '" + about.User.EmailAddress + "' has been added.");

                var account = Account.GetAccount(about.User.EmailAddress);
                if (account != null)
                {
                    Settings.FileSystem.AddAccount(account, true);
                    SynchronizeNow();
                }
            }
            catch (Exception ex)
            {
                if (ex.IsCanceledTask())
                    return;

                AddLog("An error occurred: " + ex.GetAllMessages());
            }
        }

        private void listAccountsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddLog();
            var accounts = Account.GetAllAccounts(true).ToArray();
            AddLog(accounts.Length + " account(s) where found.");
            for (int i = 0; i < accounts.Length; i++)
            {
                var account = accounts[i];
                AddLog("Account[" + i + "]");
                AddLog(" User Email Address           : " + account.UserEmailAddress);
                AddLog(" Data Directory Path          : " + account.DataDirectoryPath);
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

        private void fileToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            addAccountToolStripMenuItem.Enabled = Settings.HasSecretsFile;
            listAccountsToolStripMenuItem.Enabled = Settings.HasSecretsFile;
        }

        private class MainLogger : ILogger
        {
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
                Main.InvokeAddLog("(" + threadName + "):" + Prefix + methodName + ":" + level + ": " + value, true);
            }
        }

        private void StartSynchronizer(bool synchronizeRootFolder = false)
        {
            InvokeAddLog("Starting synchronization on data directory: " + Settings.DataDirectoryPath);
            int i = 0;
            foreach (var account in Account.GetAllAccounts(false))
            {
                Settings.FileSystem.AddAccount(account, synchronizeRootFolder);
                InvokeAddLog("Added account : " + account.UserEmailAddress);
                i++;
            }
            InvokeAddLog("Synchronizer has found " + i + " account(s) to synchronize.");

            Settings.StartSynchronizer(new MainLogger(this, "Synchronizer", Settings.Current.SynchronizerLogLevel));
        }

        private void SynchronizeNow()
        {
            if (!Settings.HasSecretsFile)
                return;

            if (!OnDemandSynchronizer.IsSupported)
                return;

            Task.Run(() =>
            {
                if (Settings.Synchronizer == null)
                {
                    StartSynchronizer();
                    return;
                }

                Settings.Synchronizer.Synchronize();
            });
        }

        private void runSynchronizerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Task.Run(() => StartSynchronizer());
        }

        private void synchronizeNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SynchronizeNow();
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxMain.Clear();
        }

        private void synchronizationToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            runSynchronizerToolStripMenuItem.Enabled = OnDemandSynchronizer.IsSupported && Settings.HasSecretsFile &&
                (Settings.Synchronizer == null || Settings.Synchronizer.SyncPeriod <= 0);

            synchronizeNowToolStripMenuItem.Enabled = OnDemandSynchronizer.IsSupported && Settings.HasSecretsFile;
            stopSynchronizerToolStripMenuItem.Enabled = OnDemandSynchronizer.IsSupported && Settings.HasSecretsFile &&
                Settings.Synchronizer != null && Settings.Synchronizer.SyncPeriod > 0;

            uninstallToolStripMenuItem.Enabled = OnDemandSynchronizer.IsSupported;
        }

        private void resetGoogleDriveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ShowConfirm("You are about to reset the synchronization information from the root folder. This could take some time depending on the number of folders and file it will impact." +
                Environment.NewLine + Environment.NewLine +
                "Are you sure you want to do this?") != DialogResult.Yes)
                return;

            foreach (var account in Settings.FileSystem.Accounts)
            {
                account.InitializeDatabase(true);
            }
        }

        private void stopSynchronizerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Settings.Synchronizer == null)
                return;

            Settings.Synchronizer.SyncPeriod = 0;
            AddLog("Synchronization is stopped.");
        }

        private void uninstallToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (this.ShowConfirm("You are about to deactivate the synchronization that is setup between the '" + Settings.DataDirectoryPath + "' directory and the cloud." +
                Environment.NewLine + Environment.NewLine +
                "Are you sure you want to do this?") != DialogResult.Yes)
                return;

            Settings.UnregisterOnDemandSynchronizer();
        }

        private void openConfigurationBackupPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IOUtilities.DirectoryExists(Settings.ConfigurationBackupDirectoryPath))
            {
                WindowsUtilities.OpenExplorer(Settings.ConfigurationBackupDirectoryPath);
            }
        }

        private void diagnosticsToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            openConfigurationBackupPathToolStripMenuItem.Enabled = IOUtilities.DirectoryExists(Settings.ConfigurationBackupDirectoryPath);
            resetGoogleDriveStateToolStripMenuItem.Enabled = OnDemandSynchronizer.IsSupported && Settings.HasSecretsFile;
        }

        private void openDataDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IOUtilities.DirectoryExists(Settings.DataDirectoryPath))
            {
                WindowsUtilities.OpenExplorer(Settings.DataDirectoryPath);
            }
        }

        private void openDataDirectoryToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            openDataDirectoryToolStripMenuItem.Enabled = IOUtilities.DirectoryExists(Settings.DataDirectoryPath);
        }
    }
}
