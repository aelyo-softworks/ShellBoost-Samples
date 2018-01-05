using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using ShellBoost.Core;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    public partial class EditValue : Form
    {
        private Control _editor;

        public EditValue()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(ShellFolderServer.LoadedNativeDllPath);
        }

        public string Path { get; private set; }
        public RegistryHive Hive { get; private set; }
        public object NewValue { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            Icon?.Dispose();
            base.Dispose(disposing);
        }

        public bool LoadEditor(RegistryHive hive, string path, string name)
        {
            // path is null means it's a base key
            // name can be null

            object value = null;
            using (var bk = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            {
                if (bk != null)
                {
                    using (var key = !string.IsNullOrEmpty(path) ? bk.OpenSubKey(path, false) : bk)
                    {
                        if (key != null)
                        {
                            var kind = RegistryValueKind.Unknown;
                            try
                            {
                                kind = name != null ? key.GetValueKind(name) : RegistryValueKind.String;
                                value = key.GetValue(name);
                            }
                            catch
                            {
                                // does not exist any more?
                            }

                            switch (kind)
                            {
                                case RegistryValueKind.Binary:
                                    _editor = new EditValueBinary();
                                    break;

                                case RegistryValueKind.MultiString:
                                    _editor = new EditValueMultiString();
                                    break;

                                case RegistryValueKind.ExpandString:
                                case RegistryValueKind.String:
                                    _editor = new EditValueString();
                                    break;

                                case RegistryValueKind.QWord:
                                case RegistryValueKind.DWord:
                                    _editor = new EditValueDWord();
                                    break;
                            }
                        }
                    }
                }
            }

            if (_editor == null)
            {
                TextBoxValueName.Text = "Value does not exist any more or type is not supported.";
                return false;
            }

            _editor.TabIndex = 0;
            TablePanel.Controls.Add(_editor, 0, 1);

            Path = path;
            Hive = hive;
            TextBoxValueName.Text = name ?? RegistryDefaultValueItem.DefaultName;
            ((IEditValueControl)_editor).Value = value;
            return true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (DialogResult == DialogResult.OK)
            {
                NewValue = ((IEditValueControl)_editor)?.Value;
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
