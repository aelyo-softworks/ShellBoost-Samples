using ShellBoost.Core;

namespace ShellBoost.Samples.PhysicalOverview
{
    public class HelloWorldItem : ShellItem
    {
        // note this item doesn't have a valid Win32 name
        // it works fine because it's not marked as a file system one
        public HelloWorldItem(ShellFolder parent)
            : base(parent, new StringKeyShellItemId("< hello! this item is 100% virtual >"))
        {
        }
    }
}
