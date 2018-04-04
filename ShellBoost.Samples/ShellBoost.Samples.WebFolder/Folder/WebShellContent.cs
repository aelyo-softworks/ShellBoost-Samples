using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShellBoost.Core;
using ShellBoost.Core.WindowsShell;

namespace ShellBoost.Samples.WebFolder.Folder
{
    public class WebShellContent : ShellContent
    {
        private WebShellItem _item;

        public WebShellContent(WebShellItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _item = item;
        }

        public override DateTime CreationTime => _item.DateCreated;
        public override DateTime LastAccessTime => _item.DateAccessed;
        public override DateTime LastWriteTime => _item.DateModified;
        public override long Length => _item.Size;
        public override string Name => _item.DisplayName;
        public override STGTY Type => STGTY.STGTY_STREAM;

        public override int Read(byte[] array, int count)
        {
        }

        public override long? Seek(long offset, SeekOrigin origin)
        {
        }
    }
}
