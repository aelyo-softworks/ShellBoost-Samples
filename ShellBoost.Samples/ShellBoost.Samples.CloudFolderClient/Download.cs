using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core.Synchronization;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolder.Api;

namespace ShellBoost.Samples.CloudFolderClient
{
    public partial class Download : Form, IProgressSink
    {
        private readonly CancellationTokenSource _source = new CancellationTokenSource();
        private readonly WebItem _item;
        private readonly string _filePath;

        public Download(WebItem item, string filePath)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            _item = item;
            _filePath = filePath;
            InitializeComponent();
            Icon = Program.AppIcon;
            Text = item.Name + " is downloading...";
            FormClosing += (s, e) =>
            {
                if (!IsDownloaded && (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.None))
                {
                    if (this.ShowConfirm("Are you sure you want to cancel the download?") != DialogResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }

                    _source.Cancel(false);
                    IOUtilities.FileDelete(_filePath);
                }
            };
        }

        private bool IsDownloaded => progressBarDownload.Value >= progressBarDownload.Maximum;

        void IProgressSink.Progress(SyncContext context, long total, long completed) => BeginInvoke((Action)(() =>
        {
            var ratio = total != 0 ? (int)(100 * completed / total) : 0;
            if (progressBarDownload.Value == ratio)
                return;

            if (!IsDownloaded)
            {
                progressBarDownload.Value = ratio;
                labelText.Text = completed + " / " + total + " (" + ratio + " %)";
            }

            if (ratio == 100)
            {
                buttonCancel.Text = "Close";
                Text = _item.Name + " has been downloaded";
            }
        }));

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Task.Run(async () =>
            {
                var ctx = new SyncContext(null, progressSink: this);
                using (var file = File.OpenWrite(_filePath))
                {
                    var options = new SyncGetEntryContentOptions();
                    options.CancellationToken = _source.Token;
                    await _item.DownloadAsync(file, null, ctx, options).ConfigureAwait(false);
                }
            });
        }
    }
}
