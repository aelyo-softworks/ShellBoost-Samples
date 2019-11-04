using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.LocalFolder
{
    public class VirtualAndPhysicalShellFolder : ShellFolder
    {
        public const string PhysicalStorageName = "PhysicalStorage";

        public VirtualAndPhysicalShellFolder(ShellFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            CanPaste = true;
        }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // we have no folder to show
            if (!options.HasFlag(SHCONTF.SHCONTF_NONFOLDERS))
                yield break;

            // show 5 virtual & physical files
            for (int i = 0; i < 5; i++)
            {
                yield return new VirtualAndPhysicalShellItem(this, "virtual and physical item " + i);
            }

            // show 5 virtual files
            for (int i = 0; i < 5; i++)
            {
                yield return new VirtualShellItem(this, "virtual item " + i + ".txt");
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

        protected override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            Console.WriteLine("OnDragDropTarget " + e.Type);
            e.Effect = DragDropEffects.Copy;
            foreach (var data in e.DataObject)
            {
                Console.WriteLine(" format: " + data.Name + " " + data.ConvertedData);
            }
        }

        protected override void OnOperate(ShellOperationEventArgs e)
        {
            Console.WriteLine("OnOperate " + e.Operation);
        }
    }
}
