using System;
using ShellBoost.Core;

namespace ShellBoost.Samples.Overview
{
    public class SimpleItem : ShellItem
    {
        public SimpleItem(RootFolder parent, string text)
            : base(parent, new StringKeyShellItemId(text))
        {
        }

        public override ShellContent GetContent()
        {
            return new MemoryShellContent(DisplayName + " hello " + DateTime.Now);
        }
    }
}
