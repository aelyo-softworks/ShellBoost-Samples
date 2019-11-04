using System;
using System.IO;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsPropertySystem;

namespace ShellBoost.Samples.LocalFolder
{
    public class VirtualAndPhysicalShellItem : ShellItem
    {
        public VirtualAndPhysicalShellItem(ShellFolder parent, string name)
            : base(parent, new StringKeyShellItemId(name))
        {
            // this item is virtual, but we give it a file system path (with nothing in there)
            // so it's half virtual, half physical
            FileSystemPath = Path.Combine(Path.GetFullPath("Data"), VirtualAndPhysicalShellFolder.PhysicalStorageName, name + ".txt");
        }

        // shell is asking for something
        protected override bool TryGetFileSystemValue(PropertyKey key, out object value)
        {
            if (key == PropertyStore.PreviewItem)
            {
                // make sure the directory exists
                IOUtilities.FileCreateDirectory(FileSystemPath);

                // write something in the file
                // we use a ShellBoost utility to avoid sharing violation errors
                IOUtilities.WrapSharingViolations(() =>
                {
                    File.WriteAllText(FileSystemPath, "hello from " + DisplayName + " at " + DateTime.Now);
                });
            }

            return base.TryGetFileSystemValue(key, out value);
        }
    }
}
