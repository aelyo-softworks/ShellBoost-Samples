namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryDefaultValueItem : RegistryValueItem
    {
        public static string DefaultName = "(Default)";

        public RegistryDefaultValueItem(RegistryKeyFolder parent)
            : base(parent, DefaultName) // this is a valid value name, but this is just a sample, otherwise, use something really unique
        {
            CanRename = false;
        }

        public override bool IsDefault => true;
        public override string KeyName => null;
    }
}
