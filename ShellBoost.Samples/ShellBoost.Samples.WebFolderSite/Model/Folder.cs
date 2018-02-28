using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ShellBoost.Samples.WebFolderSite.Model
{
    public class Folder : Item
    {
        public override ItemType Type => ItemType.Folder;

        [JsonIgnore]
        public IEnumerable<Folder> ChildFolders
        {
            get
            {
                string dir = Path.GetDirectoryName(FullPath);
                if (!Directory.Exists(dir))
                    yield break;

                foreach (var directory in Directory.EnumerateDirectories(dir, "*." + Id.ToString("N") + ".*"))
                {
                    var folder = new Folder();
                    folder.FullPath = directory;
                    yield return folder;
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<Item> ChildItems
        {
            get
            {
                string dir = Path.GetDirectoryName(FullPath);
                if (!Directory.Exists(dir))
                    yield break;

                foreach (var file in Directory.EnumerateFiles(dir, "*." + Id.ToString("N") + ".*"))
                {
                    var item = new Item();
                    item.FullPath = file;
                    yield return item;
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<Item> Children
        {
            get
            {
                foreach (var folder in ChildFolders)
                {
                    yield return folder;
                }

                foreach (var item in ChildItems)
                {
                    yield return item;
                }
            }
        }

        private string EnsureDirectory()
        {
            string dir = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public Item GetItem(Guid id)
        {
            string dir = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(dir))
                return null;

            var file = Directory.EnumerateFileSystemEntries(dir, id.ToString("N") + "." + Id.ToString("N") + ".*").FirstOrDefault();
            if (file == null)
                return null;

            var item = File.Exists(file) ? new Item() : new Folder();
            item.FullPath = file;
            return item;
        }

        public Item GetItem(string name)
        {
            Drive.ValidateName(name);

            string dir = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(dir))
                return null;

            var file = Directory.EnumerateFileSystemEntries(dir, "*." + Id.ToString("N") + "." + name).FirstOrDefault();
            if (file == null)
                return null;

            var item = File.Exists(file) ? new Item() : new Folder();
            item.FullPath = file;
            return item;
        }

        public Item EnsureChildItem(string name) => EnsureChildItem(name, out bool isNew);
        public Item EnsureChildItem(string name, out bool isNew)
        {
            Drive.ValidateName(name);

            var item = GetItem(name);
            if (item != null)
            {
                isNew = false;
                return item;
            }

            string dir = EnsureDirectory();
            item = new Item();
            item.FullPath = Path.Combine(dir, Guid.NewGuid().ToString("N") + "." + Id.ToString("N") + "." + name);
            using (var file = File.OpenWrite(item.FullPath))
            {
                // do nothing special
            }

            isNew = true;

            // reread infos
            var newItem = new Item();
            newItem.FullPath = item.FullPath;
            return newItem;
        }

        public Folder EnsureChildFolder(string name)
        {
            Drive.ValidateName(name);

            string dir = EnsureDirectory();
            var item = GetItem(name);
            if (item != null)
            {
                if (item is Folder fld)
                    return fld;

                throw new InvalidOperationException();
            }

            var folder = new Folder();
            folder.FullPath = Path.Combine(dir, Guid.NewGuid().ToString("N") + "." + Id.ToString("N") + "." + name);
            Directory.CreateDirectory(folder.FullPath);
            return folder;
        }

        public override bool Delete()
        {
            if (FullPath == null || !Directory.Exists(FullPath))
                return false;

            foreach (var item in Children)
            {
                item.Delete();
            }
            Directory.Delete(FullPath, true);
            return true;
        }

        public override bool Rename(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Drive.ValidateName(name);

            if (string.Compare(name, Name, StringComparison.OrdinalIgnoreCase) == 0)
                return false;

            if (FullPath == null || !Directory.Exists(FullPath))
                return false;

            Directory.Move(FullPath, Path.Combine(Path.GetDirectoryName(FullPath), Id.ToString("N") + "." + ParentId.ToString("N") + "." + name));
            return true;
        }
    }
}