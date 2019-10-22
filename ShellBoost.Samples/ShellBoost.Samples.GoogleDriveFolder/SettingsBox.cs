using System.Windows.Forms;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public partial class SettingsBox : Form
    {
        public SettingsBox(object selectedObject)
        {
            InitializeComponent();
            Icon = Program.AppIcon;
            propertyGridMain.SelectedObject = selectedObject;
        }
    }
}
