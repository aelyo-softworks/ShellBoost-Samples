using System;
using System.Windows.Forms;
using System.Globalization;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    public partial class EditValueDWord : UserControl, IEditValueControl
    {
        private readonly ValidatingTextBox _vtb;
        private object _value;

        public EditValueDWord()
        {
            InitializeComponent();
            _vtb = new ValidatingTextBox();
            _vtb.Location = TextBoxValueData.Location;
            _vtb.Margin = TextBoxValueData.Margin;
            _vtb.Name = TextBoxValueData.Name;
            _vtb.Size = TextBoxValueData.Size;
            _vtb.TabIndex = TextBoxValueData.TabIndex;
            _vtb.Text = "0";
            _vtb.TextValidating += OnTextValidating;
            Controls.Remove(TextBoxValueData);
            Controls.Add(_vtb);
        }

        private void OnTextValidating(object sender, TextValidatingEventArgs e)
        {
            var styles = RadioButtonHexadecimal.Checked ? NumberStyles.HexNumber : NumberStyles.Integer;
            if (_value is long)
            {
                e.Cancel = !long.TryParse(e.NewText, styles, null, out _);
            }
            else
            {
                e.Cancel = !int.TryParse(e.NewText, styles, null, out _);
            }
        }

        public object Value
        {
            get
            {
                _value = GetValue(RadioButtonHexadecimal.Checked);
                return _value;
            }
            set
            {
                _value = value;
                if (RadioButtonHexadecimal.Checked)
                {
                    _vtb.Text = string.Format("{0:x}", _value);
                }
                else
                {
                    _vtb.Text = string.Format("{0}", _value);
                }
            }
        }

        private object GetValue(bool hexadecimal)
        {
            var styles = hexadecimal ? NumberStyles.HexNumber : NumberStyles.Integer;
            if (_value is long)
                return long.Parse(_vtb.Text, styles);

            return int.Parse(_vtb.Text, styles);
        }

        private void RadioButtonHexadecimal_CheckedChanged(object sender, EventArgs e) => Value = GetValue(!RadioButtonHexadecimal.Checked);
    }
}
