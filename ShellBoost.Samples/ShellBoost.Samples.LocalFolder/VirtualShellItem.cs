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
        }
    }
}
