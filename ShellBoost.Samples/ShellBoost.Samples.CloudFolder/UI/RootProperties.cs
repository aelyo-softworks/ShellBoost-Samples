using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ShellBoost.Samples.CloudFolder.UI
{
    public class RootProperties : Form
    {
        private object _selectedObject;
        private readonly TableLayoutPanel _properties;

        public event EventHandler MemoryCacheCleared;

        public RootProperties()
        {
            // mimic Shell's Properties window
            MinimizeBox = false;
            MaximizeBox = false;
            Text = "CloudFolder Properties";
            Size = new Size(377, 506);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(5);
            Controls.Add(table);
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            var border = new Panel();
            border.Padding = new Padding(1);
            border.Dock = DockStyle.Fill;
            border.BackColor = Color.LightGray;
            table.Controls.Add(border, 0, 0);

            var panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BackColor = Color.White;
            border.Controls.Add(panel);

            var clearCache = new Button();
            clearCache.UseVisualStyleBackColor = true;
            clearCache.BackColor = SystemColors.ButtonFace;
            clearCache.Location = new Point(208, 387);
            clearCache.Text = "Clear Memory Cache";
            clearCache.AutoSize = true;
            clearCache.Click += (s, e) => MemoryCacheCleared?.Invoke(s, e);
            panel.Controls.Add(clearCache);

            _properties = new TableLayoutPanel();
            _properties.Padding = new Padding(5);
            panel.Controls.Add(_properties);
            _properties.BackColor = Color.Transparent;
            _properties.Dock = DockStyle.Fill;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

            _properties.Controls.Add(new Label { Text = "Property" }, 0, 0);
            _properties.Controls.Add(new Label { Text = "Value" }, 1, 0);

            var line = new Panel();
            line.Height = 1;
            line.BackColor = Color.LightGray;
            line.Dock = DockStyle.Fill;
            line.Margin = new Padding(0, 0, 0, 10);
            _properties.SetColumnSpan(line, 2);
            _properties.Controls.Add(line, 0, 1);

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

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ((Control)AcceptButton).Focus();
        }

        public object SelectedObject
        {
            get => _selectedObject;
            set
            {
                if (_selectedObject == value)
                    return;

                _selectedObject = value;
                FillProperties();
            }
        }

        private void FillProperties()
        {
            while (_properties.Controls.Count > 3)
            {
                _properties.Controls.RemoveAt(_properties.Controls.Count - 1);
            }
            if (_selectedObject == null)
                return;

            var row = 2;
            foreach (var prop in _selectedObject.GetType().GetProperties())
            {
                var browsable = prop.GetCustomAttribute<BrowsableAttribute>();
                if (browsable != null && !browsable.Browsable)
                    continue;

                var name = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name;
                var value = prop.GetValue(_selectedObject);
                if (value is Guid g)
                {
                    value = g.ToString("N");
                }
                _properties.Controls.Add(new Label { Margin = new Padding(4), Text = name, AutoSize = true }, 0, row); 
                _properties.Controls.Add(new Label { Margin = new Padding(4), Text = string.Format("{0}", value), AutoSize = true }, 1, row);
                row++;
            }
        }
    }
}
