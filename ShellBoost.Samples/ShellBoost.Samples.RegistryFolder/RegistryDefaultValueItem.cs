namespace ShellBoost.Samples.RegistryFolder
{
    public class RegistryDefaultValueItem : RegistryValueItem
    {
        public RegistryDefaultValueItem(RegistryKeyFolder parent)
            : base(parent, "(Default)") // this is a valid value name, but this is just a sample, otherwise, use something really unique
        {
            CanRename = false;
        }

        public override bool IsDefault => true;
        public override string KeyName => null;
    }
}
