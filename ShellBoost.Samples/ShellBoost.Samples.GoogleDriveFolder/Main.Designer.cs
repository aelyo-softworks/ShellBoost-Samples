namespace ShellBoost.Samples.GoogleDriveFolder
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addAccountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listAccountsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeAConfiguredAccountToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.installSecretsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.synchronizationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startSynchronizerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopSynchronizerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.synchronizeNowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDataDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.clearLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.diagnosticsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetGoogleDriveStateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.openConfigurationBackupPathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLogsPathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBoxMain = new System.Windows.Forms.TextBox();
            this.contextMenuStripLog = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStripMain.SuspendLayout();
            this.contextMenuStripLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStripMain
            // 
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.synchronizationToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1042, 24);
            this.menuStripMain.TabIndex = 0;
            this.menuStripMain.Text = "Main Menu Strip";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addAccountToolStripMenuItem,
            this.listAccountsToolStripMenuItem,
            this.removeAConfiguredAccountToolStripMenuItem,
            this.toolStripSeparator3,
            this.installSecretsToolStripMenuItem,
            this.preferencesToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.FileToolStripMenuItem_DropDownOpening);
            // 
            // addAccountToolStripMenuItem
            // 
            this.addAccountToolStripMenuItem.Name = "addAccountToolStripMenuItem";
            this.addAccountToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.addAccountToolStripMenuItem.Text = "Configure a New &Account...";
            this.addAccountToolStripMenuItem.Click += new System.EventHandler(this.AddAccountToolStripMenuItem_Click);
            // 
            // listAccountsToolStripMenuItem
            // 
            this.listAccountsToolStripMenuItem.Name = "listAccountsToolStripMenuItem";
            this.listAccountsToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.listAccountsToolStripMenuItem.Text = "&List Configured Accounts";
            this.listAccountsToolStripMenuItem.Click += new System.EventHandler(this.ListAccountsToolStripMenuItem_Click);
            // 
            // removeAConfiguredAccountToolStripMenuItem
            // 
            this.removeAConfiguredAccountToolStripMenuItem.Name = "removeAConfiguredAccountToolStripMenuItem";
            this.removeAConfiguredAccountToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.removeAConfiguredAccountToolStripMenuItem.Text = "Remove a Configured Account...";
            this.removeAConfiguredAccountToolStripMenuItem.Click += new System.EventHandler(this.RemoveAConfiguredAccountToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(243, 6);
            // 
            // installSecretsToolStripMenuItem
            // 
            this.installSecretsToolStripMenuItem.Name = "installSecretsToolStripMenuItem";
            this.installSecretsToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.installSecretsToolStripMenuItem.Text = "Configure &Secrets File...";
            this.installSecretsToolStripMenuItem.Click += new System.EventHandler(this.InstallSecretsToolStripMenuItem_Click);
            // 
            // preferencesToolStripMenuItem
            // 
            this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            this.preferencesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.P)));
            this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.preferencesToolStripMenuItem.Text = "&Preferences...";
            this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.PreferencesToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(243, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            // 
            // synchronizationToolStripMenuItem
            // 
            this.synchronizationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startSynchronizerToolStripMenuItem,
            this.stopSynchronizerToolStripMenuItem,
            this.synchronizeNowToolStripMenuItem});
            this.synchronizationToolStripMenuItem.Name = "synchronizationToolStripMenuItem";
            this.synchronizationToolStripMenuItem.Size = new System.Drawing.Size(104, 20);
            this.synchronizationToolStripMenuItem.Text = "Synchronization";
            this.synchronizationToolStripMenuItem.DropDownOpening += new System.EventHandler(this.SynchronizationToolStripMenuItem_DropDownOpening);
            // 
            // startSynchronizerToolStripMenuItem
            // 
            this.startSynchronizerToolStripMenuItem.Name = "startSynchronizerToolStripMenuItem";
            this.startSynchronizerToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.startSynchronizerToolStripMenuItem.Text = "Start Synchronizer";
            this.startSynchronizerToolStripMenuItem.Click += new System.EventHandler(this.StartSynchronizerToolStripMenuItem_Click);
            // 
            // stopSynchronizerToolStripMenuItem
            // 
            this.stopSynchronizerToolStripMenuItem.Name = "stopSynchronizerToolStripMenuItem";
            this.stopSynchronizerToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.stopSynchronizerToolStripMenuItem.Text = "Stop Synchronizer";
            this.stopSynchronizerToolStripMenuItem.Click += new System.EventHandler(this.StopSynchronizerToolStripMenuItem_Click);
            // 
            // synchronizeNowToolStripMenuItem
            // 
            this.synchronizeNowToolStripMenuItem.Name = "synchronizeNowToolStripMenuItem";
            this.synchronizeNowToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.synchronizeNowToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.synchronizeNowToolStripMenuItem.Text = "Synchronize Now";
            this.synchronizeNowToolStripMenuItem.Click += new System.EventHandler(this.SynchronizeNowToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openDataDirectoryToolStripMenuItem,
            this.toolStripSeparator6,
            this.clearLogToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // openDataDirectoryToolStripMenuItem
            // 
            this.openDataDirectoryToolStripMenuItem.Name = "openDataDirectoryToolStripMenuItem";
            this.openDataDirectoryToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openDataDirectoryToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.openDataDirectoryToolStripMenuItem.Text = "&Open Data Directory...";
            this.openDataDirectoryToolStripMenuItem.DropDownOpening += new System.EventHandler(this.OpenDataDirectoryToolStripMenuItem_DropDownOpening);
            this.openDataDirectoryToolStripMenuItem.Click += new System.EventHandler(this.OpenDataDirectoryToolStripMenuItem_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(230, 6);
            // 
            // clearLogToolStripMenuItem
            // 
            this.clearLogToolStripMenuItem.Name = "clearLogToolStripMenuItem";
            this.clearLogToolStripMenuItem.Size = new System.Drawing.Size(233, 22);
            this.clearLogToolStripMenuItem.Text = "Clear Log";
            this.clearLogToolStripMenuItem.Click += new System.EventHandler(this.ClearLogToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.diagnosticsToolStripMenuItem,
            this.toolStripSeparator5,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // diagnosticsToolStripMenuItem
            // 
            this.diagnosticsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resetGoogleDriveStateToolStripMenuItem,
            this.toolStripSeparator4,
            this.openConfigurationBackupPathToolStripMenuItem,
            this.openLogsPathToolStripMenuItem});
            this.diagnosticsToolStripMenuItem.Name = "diagnosticsToolStripMenuItem";
            this.diagnosticsToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.diagnosticsToolStripMenuItem.Text = "Diagnostics";
            this.diagnosticsToolStripMenuItem.DropDownOpening += new System.EventHandler(this.DiagnosticsToolStripMenuItem_DropDownOpening);
            // 
            // resetGoogleDriveStateToolStripMenuItem
            // 
            this.resetGoogleDriveStateToolStripMenuItem.Name = "resetGoogleDriveStateToolStripMenuItem";
            this.resetGoogleDriveStateToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.resetGoogleDriveStateToolStripMenuItem.Text = "Reset Google Drive State";
            this.resetGoogleDriveStateToolStripMenuItem.Click += new System.EventHandler(this.ResetGoogleDriveStateToolStripMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(255, 6);
            // 
            // openConfigurationBackupPathToolStripMenuItem
            // 
            this.openConfigurationBackupPathToolStripMenuItem.Name = "openConfigurationBackupPathToolStripMenuItem";
            this.openConfigurationBackupPathToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.openConfigurationBackupPathToolStripMenuItem.Text = "Open Configuration Backup Path...";
            this.openConfigurationBackupPathToolStripMenuItem.Click += new System.EventHandler(this.OpenConfigurationBackupPathToolStripMenuItem_Click);
            // 
            // openLogsPathToolStripMenuItem
            // 
            this.openLogsPathToolStripMenuItem.Name = "openLogsPathToolStripMenuItem";
            this.openLogsPathToolStripMenuItem.Size = new System.Drawing.Size(258, 22);
            this.openLogsPathToolStripMenuItem.Text = "Open Logs Path...";
            this.openLogsPathToolStripMenuItem.Click += new System.EventHandler(this.OpenLogsPathToolStripMenuItem_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(132, 6);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.aboutToolStripMenuItem.Text = "&About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.AboutToolStripMenuItem_Click);
            // 
            // textBoxMain
            // 
            this.textBoxMain.BackColor = System.Drawing.Color.White;
            this.textBoxMain.ContextMenuStrip = this.contextMenuStripLog;
            this.textBoxMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxMain.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxMain.Location = new System.Drawing.Point(0, 24);
            this.textBoxMain.Multiline = true;
            this.textBoxMain.Name = "textBoxMain";
            this.textBoxMain.ReadOnly = true;
            this.textBoxMain.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxMain.Size = new System.Drawing.Size(1042, 426);
            this.textBoxMain.TabIndex = 1;
            // 
            // contextMenuStripLog
            // 
            this.contextMenuStripLog.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearAllToolStripMenuItem});
            this.contextMenuStripLog.Name = "contextMenuStripLog";
            this.contextMenuStripLog.Size = new System.Drawing.Size(160, 26);
            // 
            // clearAllToolStripMenuItem
            // 
            this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
            this.clearAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
            this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(159, 22);
            this.clearAllToolStripMenuItem.Text = "Clear All";
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.ClearAllToolStripMenuItem_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1042, 450);
            this.Controls.Add(this.textBoxMain);
            this.Controls.Add(this.menuStripMain);
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ShellBoost Samples - Google Drive Folder";
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.contextMenuStripLog.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxMain;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem addAccountToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem listAccountsToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripLog;
        private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDataDirectoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem synchronizationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startSynchronizerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem synchronizeNowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopSynchronizerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem installSecretsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem diagnosticsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetGoogleDriveStateToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem openConfigurationBackupPathToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLogsPathToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripMenuItem clearLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem removeAConfiguredAccountToolStripMenuItem;
    }
}

