using System;
using System.IO;
using Newtonsoft.Json;

namespace ShellBoost.Samples.WebFolderSite.Model
{
    public class Item
    {
        // private API
        private string _fullPath;

        [JsonIgnore]
        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                var split = Path.GetFileName(_fullPath).Split(new char[] { '.'}, 3);
                Id = Guid.Parse(split[0]);
                ParentId = Guid.Parse(split[1]);
                if (split.Length > 2)
                {
                    Name = split[2];
                }

                if (Type == ItemType.Item)
                {
                    var fi = new FileInfo(_fullPath);
                    if (fi.Exists)
                    {
                        Length = fi.Length;
                        Attributes = fi.Attributes;
                        LastWriteTimeUtc = fi.LastWriteTimeUtc;
                        CreationTimeUtc = fi.CreationTimeUtc;
                    }
                }
                else
                {
                    var di = new DirectoryInfo(_fullPath);
                    if (di.Exists)
                    {
                        Attributes = di.Attributes;
                        LastWriteTimeUtc = di.LastWriteTimeUtc;
                        CreationTimeUtc = di.CreationTimeUtc;
                    }
                }
            }
        }

        // public API R/W properties
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public virtual ItemType Type { get; set; }
        public FileAttributes Attributes { get; set; }

        // public API R/O properties
        public long Length { get; private set; }
        public DateTime CreationTimeUtc { get; private set; }
        public DateTime LastWriteTimeUtc { get; private set; }

        public virtual void Update(Item item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (item.Name != Name)
            {
                Rename(item.Name);
            }

            var atts = item.Attributes & ~FileAttributes.Directory;
            File.SetAttributes(FullPath, atts);
        }

        public virtual bool Delete()
        {
            if (FullPath == null  || !File.Exists(FullPath))
                return false;

            File.Delete(FullPath);
            return true;
        }

        public virtual bool Rename(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (string.Compare(name, Name, StringComparison.OrdinalIgnoreCase) == 0)
                return false;

            if (FullPath == null || !File.Exists(FullPath))
                return false;

            File.Move(FullPath, Path.Combine(Path.GetDirectoryName(FullPath), Id.ToString() + "." + ParentId.ToString() + "." + name));
            return true;
        }
    }
}