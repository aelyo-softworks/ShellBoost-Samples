using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShellBoost.Samples.CloudFolder.UI
{
    public class RootProperties : Form
    {
        public RootProperties()
        {
            // mimic Shell's Properties window
            MinimizeBox = false;
            MaximizeBox = false;
            Text = "Device Manager Folder Properties";
            Size = new Size(377, 506);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(5);
            Controls.Add(table);
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            PropertyGrid = new PropertyGrid();
            PropertyGrid.ToolbarVisible = false;
            PropertyGrid.HelpVisible = false;
            PropertyGrid.Dock = DockStyle.Fill;
            table.Controls.Add(PropertyGrid, 0, 0);

            var btn = new Button();
            btn.UseVisualStyleBackColor = true;
            btn.DialogResult = DialogResult.OK;
            AcceptButton = btn;
            CancelButton = btn;
            btn.Dock = DockStyle.Right;
            btn.Text = "OK";
            btn.Width = 75;
            btn.Height = 23;
            table.Controls.Add(btn, 0, 1);
            AcceptButton = btn;
            btn.Click += (s, e) => Close();
        }

        public PropertyGrid PropertyGrid { get; }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ((Control)AcceptButton).Focus();
        }
    }
}
