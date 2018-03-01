using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class DriveFolder : RootShellFolder
    {
        public DriveFolder(WebFolderServer server, ShellItemIdList idList)
            : base(idList)
        {
            Server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public WebFolderServer Server { get; }

        public override IEnumerable<ShellItem> EnumItems(SHCONTF options)
        {
            // this contains only folders
            if ((options & SHCONTF.SHCONTF_FOLDERS) != SHCONTF.SHCONTF_FOLDERS)
                yield break;

        }
    }
}
