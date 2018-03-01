using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShellBoost.Core;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class WebFolderServer : ShellFolderServer
    {
        private DriveFolder _drive;

        protected override RootShellFolder GetRootFolder(ShellItemIdList idl)
        {
            if (_drive == null)
            {
                _drive = new DriveFolder(this, idl);
            }
            return _drive;
        }
    }
}
