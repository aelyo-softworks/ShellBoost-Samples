using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ShellBoost.Samples.CloudFolderClient.Events;

namespace ShellBoost.Samples.CloudFolderClient
{
    public partial class Log : Form
    {
        public event EventHandler<EventArgs> ClearLog;

        public Log()
        {
            InitializeComponent();
            Icon = Program.AppIcon;
            clearLogStripMenuItem.Click += (s, e) => ClearLog?.Invoke(this, EventArgs.Empty);
        }

        public void AddEvents(IEnumerable<ServerEventArgs> events)
        {
            if (events == null)
                return;

            ListViewItem last = null;
            listViewLog.BeginUpdate();
            foreach (var e in events)
            {
                last = Add(e);
            }

            if (last != null)
            {
                last.EnsureVisible();
                last.Selected = true;
            }
            listViewLog.EndUpdate();
        }

        public void AddEvent(ServerEventArgs e)
        {
            if (e == null)
                return;

            var item = Add(e);
            item.EnsureVisible();
            item.Selected = true;
        }

        private ListViewItem Add(ServerEventArgs e)
        {
            var item = new ListViewItem(e.Type.ToString());
            item.SubItems.Add(e.Id.ToString());
            item.SubItems.Add(e.ParentId.ToString());
            item.SubItems.Add(e.CreationTimeUtc.ToLocalTime().ToString());
            item.SubItems.Add(e.OldName ?? string.Empty);
            item.SubItems.Add(e.OldParentId.HasValue ? e.OldParentId.Value.ToString() : string.Empty);
            listViewLog.Items.Add(item);
            return item;
        }
    }
}
