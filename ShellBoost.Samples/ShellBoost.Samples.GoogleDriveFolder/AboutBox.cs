using System.Windows.Forms;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.GoogleDriveFolder
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            Icon = Program.AppIcon;
            Text = "About " + AssemblyUtilities.GetDescription();
            labelProductName.Text = AssemblyUtilities.GetDescription();
            labelVersion.Text = "Version " + AssemblyUtilities.GetInformationalVersion();
            labelCopyright.Text = AssemblyUtilities.GetCopyright();
            labelCompanyName.Text = AssemblyUtilities.GetCompany();
        }
    }
}
