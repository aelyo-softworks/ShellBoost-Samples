using System;
using SevenZip;

namespace ShellBoost.Samples.SevenZipFolder
{
    // folder utility class to represent SevenZipExtractor flat model as a hierarchical model
    public class ArchiveFolderInfo
    {
        public ArchiveFolderInfo(ArchiveFolderInfo parent, string name)
        {
            if (parent == null && parent is ArchiveRootFolderInfo)
                throw new ArgumentNullException(nameof(parent));

            if (name == null && parent is ArchiveRootFolderInfo)
                throw new ArgumentNullException(nameof(name));

            Parent = parent;
            Name = name;
            Folders = Array.Empty<ArchiveFolderInfo>();
            Files = Array.Empty<ArchiveFileInfo>();
        }

        public ArchiveFolderInfo Parent { get; }
        public string Name { get; }
        public ArchiveFolderInfo[] Folders { get; private set; }
        public ArchiveFileInfo[] Files { get; private set; }
        public ArchiveRootFolderInfo Root => IsRoot ? (ArchiveRootFolderInfo)this : Parent.Root;
        public virtual bool IsRoot => false;

        public void AddFolder(ArchiveFolderInfo folder)
        {
            if (folder == null)
                throw new ArgumentNullException(nameof(folder));

            var array = Folders != null ? new ArchiveFolderInfo[Folders.Length + 1] : new ArchiveFolderInfo[1];
            if (Folders != null)
            {
                Array.Copy(Folders, array, Folders.Length);
            }

            array[array.Length - 1] = folder;
            Folders = array;
        }

        public void AddFile(ArchiveFileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            var array = Files != null ? new ArchiveFileInfo[Files.Length + 1] : new ArchiveFileInfo[1];
            if (Files != null)
            {
                Array.Copy(Files, array, Files.Length);
            }

            array[array.Length - 1] = file;
            Files = array;
        }

        public override string ToString() => Name;
    }
}
