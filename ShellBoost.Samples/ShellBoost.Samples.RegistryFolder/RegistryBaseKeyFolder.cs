using Microsoft.Win32;

namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryBaseKeyFolder : RegistryKeyFolder
    {
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

        private static string GetName(RegistryHive hive)
        {
            using (var key = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
            {
                return key.Name;
            }
        }
    }
}
