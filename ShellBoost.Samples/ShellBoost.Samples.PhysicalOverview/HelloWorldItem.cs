using ShellBoost.Core;

namespace ShellBoost.Samples.PhysicalOverview
{
    public class HelloWorldItem : ShellItem
    {
        public HelloWorldItem(ShellFolder parent)
            : base(parent, new StringKeyShellItemId("< hello! this item is 100% virtual >"))
        {
        }
    }
}
