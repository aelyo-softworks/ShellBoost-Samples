using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.LocalFolder
{
    public class DropTargetShellFolder : ShellFolder
    {
        public DropTargetShellFolder(ShellFolder parent)
            : base(parent, new StringKeyShellItemId("<Drop Target>"))
        {
            CanPaste = true;
        }

        protected async override void OnDragDropTarget(DragDropTargetEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
            if (e.Type == DragDropTargetEventType.DragDrop)
            {
                // you can use other formats but this one is the only one that contains PIDLs
                var list = string.Join(Environment.NewLine, e.DataObject.ItemsIdLists.Select(id => id.GetName(Core.WindowsShell.SIGDN.SIGDN_NORMALDISPLAY)));

                await WindowsUtilities.DoModelessAsync(() =>
                {
                    MessageBox.Show(null, "UAC level is " + DiagnosticsInformation.GetTokenElevationType(), "Registry Folder");
                });
            }
        }
    }
}
