using System.ComponentModel;
using System.Configuration.Install;

namespace ShellBoost.Samples.FolderService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
