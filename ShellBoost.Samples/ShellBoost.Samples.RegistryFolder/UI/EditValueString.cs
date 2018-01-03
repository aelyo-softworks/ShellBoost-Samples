using System.Windows.Forms;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    public partial class EditValueString : UserControl, IEditValueControl
    {
        public EditValueString() => InitializeComponent();
        public object Value { get => TextBoxValueData.Text; set => TextBoxValueData.Text = string.Format("{0}", value); }
    }
}
