using System;
using Microsoft.Win32;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryBaseKeyFolder : RegistryKeyFolder
    {
        private readonly static Guid _adminMessageGuid = Guid.NewGuid();

        public RegistryBaseKeyFolder(RootFolder parent, RegistryHive hive)
            : base(parent, GetName(hive))
        {
            Hive = hive;
            CanRename = false;
            CanDelete = false;
        }

        public RegistryHive Hive { get; }
        public override string Path => null;
        public override RegistryBaseKeyFolder BaseParent => this;

        protected override InformationBar GetInformationBar()
        {
            if (Hive != RegistryHive.LocalMachine || DiagnosticsInformation.GetTokenElevationType() == TokenElevationType.Full)
                return null;

            var bar = new InformationBar();
            bar.Guid = _adminMessageGuid;
            bar.Message = "You must be running with full priviledges to change anything here";
            return bar;
        }

        protected override void CreateInformationBarMenu(InformationBar bar, ShellMenu appendMenu)
        {
            var item = new ShellMenuItem(appendMenu, "What is my UAC level?");
            appendMenu.Items.Add(item);
        }

        protected override async void HandleInformationBarMenu(InformationBar bar, IntPtr hwndOwner, int id)
        {
            if (id == 1) // first item has id 1, etc.
            {
                await WindowsUtilities.DoModelessAsync(() =>
                {
                    System.Windows.Forms.MessageBox.Show(new Win32Window(hwndOwner), "UAC level is " + DiagnosticsInformation.GetTokenElevationType(), "Registry Folder");
                });
                return;
            }
        }

        private static string GetName(RegistryHive hive)
        {
            using (var key = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            {
                return key.Name;
            }
        }
    }
}
