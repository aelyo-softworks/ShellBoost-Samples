using System.Linq;
using System.Windows.Forms;

namespace ShellBoost.Samples.RegistryFolder.UI
{
    public partial class EditValueMultiString : UserControl, IEditValueControl
    {
        public EditValueMultiString() => InitializeComponent();
        public object Value { get => TextBoxList.Lines.Where(l => l.Length > 0).ToArray(); set => TextBoxList.Lines = (string[])value; }
    }
}
