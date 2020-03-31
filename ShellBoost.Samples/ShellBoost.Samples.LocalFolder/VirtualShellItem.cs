using System;
using System.IO;
using ShellBoost.Core;

namespace ShellBoost.Samples.LocalFolder
{
    public class VirtualShellItem : ShellItem
    {
        public VirtualShellItem(ShellFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            // the containing folder must override OnDragDropTarget for Copy&Paste and Drag&Drop operations
            CanPaste = true; // sets CanMove, CanCopy and IsDropTarget

            // the containing folder must override OnOperate for Delete operations
            CanDelete = true;

            // force item type using item’s name extension
            ItemType = Path.GetExtension(name);
        }

        public override ShellContent GetContent() => new MemoryShellContent("virtual hello from " + DisplayName + " at " + DateTime.Now);
    }
}
