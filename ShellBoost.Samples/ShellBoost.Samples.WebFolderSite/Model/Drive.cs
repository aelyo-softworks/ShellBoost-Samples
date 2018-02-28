using System;
using System.IO;
using System.Linq;

namespace ShellBoost.Samples.WebFolderSite.Model
{
    // In this sample implementation, all items are stored in a flat unique directory.
    // Items can be items (files) or folders.
    // Each item has a name formatted as "<id>.<parentid>.<name>", id and parentid are Guids formatted with N format
    // A folder is represented by a physical folder (which contains nothing).
    // A file is represented by a physical file wich contains the data.
    //
    // NOTE: to support MaxNameLength, we need long path support (using .NET Framework 4.6.2 to compile the project is the simplest way to have that)
    public class Drive
    {
        public const int MaxNameLength = 255 - 66; // 189

        public Drive(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException(nameof(directoryPath));

            DirectoryPath = directoryPath;
            RootFolder = new Folder();
            RootFolder.FullPath = Path.Combine(directoryPath, Guid.Empty.ToString("N") + "." + Guid.Empty.ToString("N"));
        }

        public string DirectoryPath { get; }
        public Folder RootFolder { get; }
        public static Drive Root { get; set; }

        public static void ValidateName(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length > MaxNameLength)
                throw new ArgumentException("Item or Folder name is too long. Maximum is " + MaxNameLength + " characters.", nameof(name));
        }

        public bool DeleteItem(Guid id)
        {
            var item = GetItem(id);
            if (item == null)
                return false;

            return item.Delete();
        }

        public Folder GetFolder(Guid id) => GetItem(id) as Folder;
        public Item GetItem(Guid id)
        {
            if (id == Guid.Empty)
                return RootFolder;

            string dir = Path.GetDirectoryName(RootFolder.FullPath);
            if (!Directory.Exists(dir))
                return null;

            var file = Directory.EnumerateFileSystemEntries(dir, id.ToString("N") + ".*").FirstOrDefault();
            if (file == null)
                return null;

            var item = File.Exists(file) ? new Item() : new Folder();
            item.FullPath = file;
            return item;
        }

        public Folder EnsureFolder(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var split = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var folder = RootFolder;
            for (int i = 0; i < split.Length; i++)
            {
                folder = folder.EnsureChildFolder(split[i]);
            }
            return folder;
        }

        public Item EnsureItem(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var split = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var folder = RootFolder;
            for (int i = 0; i < (split.Length - 1); i++)
            {
                folder = folder.EnsureChildFolder(split[i]);
            }
            return folder.EnsureChildItem(split.Last());
        }
    }
}