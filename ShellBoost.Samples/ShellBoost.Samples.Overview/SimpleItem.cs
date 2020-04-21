using System;
using ShellBoost.Core;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.Overview
{
    public class SimpleItem : ShellItem
    {
        public SimpleItem(RootFolder parent, string text)
            : base(parent, new StringKeyShellItemId(text))
        {
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            // dynamic infotip (aka: tooltip)
            if (key == Core.WindowsPropertySystem.System.InfoTipText)
            {
                value = "This is item " + DisplayName;
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }

        public override ShellContent GetContent()
        {
            return new MemoryShellContent(DisplayName + " hello " + DateTime.Now);
        }
    }
}
