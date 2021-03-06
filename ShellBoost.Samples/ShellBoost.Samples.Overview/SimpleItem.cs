﻿using System;
using ShellBoost.Core;
using ShellBoost.Core.Client;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.Overview
{
    public class SimpleItem : ShellItem
    {
        public SimpleItem(ShellFolder parent, string text)
            : base(parent, new StringKeyShellItemId(text))
        {
            // this is needed for icon
            ItemType = IOUtilities.PathGetExtension(text);
            CanCopy = true;
        }

        public override bool TryGetPropertyValue(PropertyKey key, out object value)
        {
            if (key == Core.WindowsPropertySystem.System.PropList.InfoTip)
            {
                value = null;
                return false;
            }

            // dynamic infotip (aka: tooltip)
            if (key == Core.WindowsPropertySystem.System.InfoTipText)
            {
                value = "This is " + DisplayName + ", info created " + DateTime.Now;
                return true;
            }

            return base.TryGetPropertyValue(key, out value);
        }

        // give back some dynamic content
        public override ShellContent GetContent() => new MemoryShellContent(DisplayName + " - this is dynamic content created from .NET " + (Installer.IsNetCore ? "Core" : "Framework") + " at " + DateTime.Now);
    }
}
