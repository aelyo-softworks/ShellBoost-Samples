
namespace ShellBoost.Samples.CloudFolderClient
{
    partial class Log
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
            this.listViewLog = new System.Windows.Forms.ListView();
            this.columnType = new System.Windows.Forms.ColumnHeader();
            this.columnId = new System.Windows.Forms.ColumnHeader();
            this.columnParentId = new System.Windows.Forms.ColumnHeader();
            this.columnCreationTime = new System.Windows.Forms.ColumnHeader();
            this.columnOldName = new System.Windows.Forms.ColumnHeader();
            this.columnOldParentId = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStripLog = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.clearLogStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // listViewLog
            // 
            this.listViewLog.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnType,
            this.columnId,
            this.columnParentId,
            this.columnCreationTime,
            this.columnOldName,
            this.columnOldParentId});
            this.listViewLog.ContextMenuStrip = this.contextMenuStripLog;
            this.listViewLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewLog.FullRowSelect = true;
            this.listViewLog.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listViewLog.HideSelection = false;
            this.listViewLog.Location = new System.Drawing.Point(0, 0);
            this.listViewLog.MultiSelect = false;
            this.listViewLog.Name = "listViewLog";
            this.listViewLog.Size = new System.Drawing.Size(1252, 670);
            this.listViewLog.TabIndex = 0;
            this.listViewLog.UseCompatibleStateImageBehavior = false;
            this.listViewLog.View = System.Windows.Forms.View.Details;
            // 
            // columnType
            // 
            this.columnType.Name = "columnType";
            this.columnType.Text = "Type";
            // 
            // columnId
            // 
            this.columnId.Name = "columnId";
            this.columnId.Text = "Id";
            this.columnId.Width = 250;
            // 
            // columnParentId
            // 
            this.columnParentId.Name = "columnParentId";
            this.columnParentId.Text = "Parent Id";
            this.columnParentId.Width = 250;
            // 
            // columnCreationTime
            // 
            this.columnCreationTime.Name = "columnCreationTime";
            this.columnCreationTime.Text = "Creation Time";
            this.columnCreationTime.Width = 120;
            // 
            // columnOldName
            // 
            this.columnOldName.Name = "columnOldName";
            this.columnOldName.Text = "Old Name";
            this.columnOldName.Width = 120;
            // 
            // columnOldParentId
            // 
            this.columnOldParentId.Name = "columnOldParentId";
            this.columnOldParentId.Text = "Old Parent Id";
            this.columnOldParentId.Width = 250;
            // 
            // contextMenuStripLog
            // 
            this.contextMenuStripLog.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.clearLogStripMenuItem});
            this.contextMenuStripLog.Name = "contextMenuStripLog";
            this.contextMenuStripLog.Size = new System.Drawing.Size(102, 26);
            // 
            // clearLogStripMenuItem
            // 
            this.clearLogStripMenuItem.Name = "clearLogStripMenuItem";
            this.clearLogStripMenuItem.Size = new System.Drawing.Size(101, 22);
            this.clearLogStripMenuItem.Text = "Clear";
            // 
            // Log
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1252, 670);
            this.Controls.Add(this.listViewLog);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Log";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Events Log";
            this.contextMenuStripLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listViewLog;
        private System.Windows.Forms.ColumnHeader columnType;
        private System.Windows.Forms.ColumnHeader columnId;
        private System.Windows.Forms.ColumnHeader columnParentId;
        private System.Windows.Forms.ColumnHeader columnOldName;
        private System.Windows.Forms.ColumnHeader columnOldParentId;
        private System.Windows.Forms.ColumnHeader columnCreationTime;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripLog;
        private System.Windows.Forms.ToolStripMenuItem clearLogStripMenuItem;
    }
}