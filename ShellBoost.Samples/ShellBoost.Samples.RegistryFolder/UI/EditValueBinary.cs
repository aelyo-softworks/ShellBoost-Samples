using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel.Design;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    // note this control does not support write operations. Well, ok, this is just a sample for ShellBoost ...
    public partial class EditValueBinary : UserControl, IEditValueControl
    {
        private ByteViewer _editor;
        private byte[] _value;

        public EditValueBinary()
        {
            InitializeComponent();
            _editor = new ByteViewer();
            _editor.Location = new Point(9, 16);
            _editor.Margin = new Padding(3, 3, 0, 3);
            _editor.Name = "TextBoxValueData";
            _editor.TabIndex = 1;
            Controls.Add(_editor);
        }

        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = (byte[])value;
                _editor.SetBytes(_value);
            }
        }
    }
}
