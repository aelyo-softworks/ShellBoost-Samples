using System;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.CloudFolderClient
{
    partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            Icon = Program.AppIcon;
            Text = "About " + ProductName;
            pictureBoxLogo.Image = Program.AppIcon.ToBitmap();
            labelText.Text = ProductName + Environment.NewLine
                + "Version " + typeof(ShellContext).Assembly.GetInformationalVersion() + Environment.NewLine
                + AssemblyUtilities.GetCopyright();
        }
    }
}
