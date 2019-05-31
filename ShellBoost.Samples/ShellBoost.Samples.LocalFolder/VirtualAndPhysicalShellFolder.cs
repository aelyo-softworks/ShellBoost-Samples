using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;
using System.Collections.Generic;

namespace ShellBoost.Samples.LocalFolder
{
    public class VirtualAndPhysicalShellFolder : ShellFolder
    {
        public const string PhysicalStorageName = "PhysicalStorage";

        public VirtualAndPhysicalShellFolder(ShellFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // we have no folder to show
            if (!options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
                yield break;

            // show 10 virtual files
            for (int i = 0; i < 10; i++)
            {
                yield return new VirtualAndPhysicalShellItem(this, "virtual item " + i);
            }
        }

        protected override void OnFileDialogEvent(object sender, FileDialogEventArgs e)
        {
            // OK/Open/etc. button was pressed
            if (e.Type == FileDialogEventType.OnButtonOk)
            {
                if (!e.IsFileSave && e.CurrentSelectionIdList != null)
                {
                    // we're in file open, get the path from its PIDL
                    var filePath = e.CurrentSelectionIdList.GetPath();
                    
                    // TODO: do something with this...
                }
            }
        }
    }
}
