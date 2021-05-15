using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.FileSystem.Local
{
    // implementation of files and folders for a physical Windows directory
    public class LocalItem : IFileInfo, IFolderInfo
    {
        private readonly Lazy<LocalItem> _parent;

        public LocalItem(LocalFileSystem system, FileSystemEntry entry)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            System = system;
            Entry = entry;
            Name = IOExtensions.UnescapeFileNameToName(Entry.Name);
            EscapedName = Entry.Name;
            _parent = new Lazy<LocalItem>(() => System.GetLocalItemAsync(ParentId).Result);
        }

        [JsonIgnore]
        public LocalFileSystem System { get; }

        [JsonIgnore]
        public FileSystemEntry Entry { get; }

        public Guid ParentId => Entry.Parent.Id;

        [JsonIgnore]
        public bool IsFolder => Attributes.HasFlag(FileAttributes.Directory);

        [JsonIgnore]
        public bool IsHidden => Attributes.HasFlag(FileAttributes.Hidden);

        // IFileSystemInfo
        [JsonIgnore]
        public IFolderInfo Parent => _parent.Value;

        public Guid Id => Entry.Id;
        public string Name { get; }
        public DateTime LastAccessTimeUtc => Entry.LastAccessTimeUtc;
        public DateTime CreationTimeUtc => Entry.CreationTimeUtc;
        public DateTime LastWriteTimeUtc => Entry.LastWriteTimeUtc;
        public FileAttributes Attributes => Entry.Attributes;
        IFileSystem IFileSystemInfo.System => System;

        [JsonIgnore]
        public string EscapedName { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');

            sb.Append("Id=");
            sb.Append(Id);

            sb.Append(" PId=");
            sb.Append(ParentId);

            sb.Append(" Name='");
            sb.Append(Name);
            sb.Append('\'');

            if (EscapedName != Name)
            {
                sb.Append(" EName='");
                sb.Append(EscapedName);
                sb.Append('\'');
            }

            sb.Append(" Atts=");
            sb.Append(Attributes);

            sb.Append(" CreationTime=");
            sb.Append(CreationTimeUtc.ToLocalTime());

            sb.Append(" LastWriteTime=");
            sb.Append(LastWriteTimeUtc.ToLocalTime());

            sb.Append(']');
            return sb.ToString();
        }

        public async Task<IFileSystemInfo> GetByNameAsync(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            System.Log("Item: " + Id + " name :" + name);
            var entry = GetLocalItemByName(name);
            return await Task.FromResult(entry).ConfigureAwait(false);
        }

        private LocalItem GetLocalItemByName(string unescapedName)
        {
            var escaped = IOExtensions.EscapeNameToFileName(unescapedName);
            var entry = Entry.EnumerateChildren().FirstOrDefault(e => e.Name.EqualsIgnoreCase(escaped));
            if (entry == null)
                return null;

            return new LocalItem(System, entry);
        }

        public Task<bool> DeleteAsync(DeleteOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " options :" + options);
            IOUtilities.DirectoryDelete(Entry.GetFinalPath(), true);
            return Task.FromResult(true);
        }

        public async Task<IFileSystemInfo> MoveToAsync(Guid newParentId, MoveOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " newParentId :" + newParentId + " options :" + options);
            if (ParentId == newParentId)
                return this;

            options ??= new MoveOptions();
            if (options.Copy)
                return await CopyToAsync(this, newParentId).ConfigureAwait(false);

            var parent = await System.GetLocalItemAsync(newParentId).ConfigureAwait(false);
            if (parent == null)
                throw new InvalidOperationException();

            if (!IOUtilities.PathIsChildOrEqual(System.ItemsPath, parent.Entry.GetFinalPath()))
                throw new InvalidOperationException();

            var finalPath = parent.Entry.GetFinalPath();
            var newPath = Path.Combine(finalPath, EscapedName);
            if (IsFolder)
            {
                IOUtilities.DirectoryMove(Entry.GetFinalPath(), newPath);
            }
            else
            {
                IOUtilities.FileMove(Entry.GetFinalPath(), newPath, true);
            }

            return parent.GetLocalItemByName(Name);
        }

        private async Task<LocalItem> CopyToAsync(LocalItem item, Guid newParentId)
        {
            var parentItem = await System.GetLocalItemAsync(newParentId).ConfigureAwait(false);
            if (parentItem == null)
                throw new InvalidOperationException();

            if (item.IsRoot)
                throw new UnauthorizedAccessException();

            var newPath = Path.Combine(parentItem.Entry.GetFinalPath(), item.EscapedName);
            if (item.IsFolder)
            {
                IOExtensions.DirectoryCopy(item.Entry.GetFinalPath(), newPath);
            }
            else
            {
                IOUtilities.FileOverwrite(item.Entry.GetFinalPath(), newPath, true);
            }

            return parentItem.GetLocalItemByName(item.Name);
        }

        public async Task<IFileSystemInfo> UpdateAsync(UpdateOptions options)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " '" + Name + "' attributes: " + Attributes + " options :" + options);

            var newItem = this;
            string newName;

            // move / rename
            if ((options.Name != null && options.Name != Name) || options.ParentId.HasValue)
            {
                newName = options.Name; // can in fact be the same (if only parent id has changed)
                string finalPath;

                // new parent (move)?
                if (options.ParentId.HasValue)
                {
                    var newParent = await System.GetLocalItemAsync(options.ParentId.Value).ConfigureAwait(false);
                    if (newParent == null)
                        throw new InvalidOperationException();

                    if (options.EnsureUniqueName)
                    {
                        // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                        newName = newParent.GetNewChildName(newName);
                    } // else overwrite

                    finalPath = newParent.Entry.GetFinalPath();
                    var newPath = Path.Combine(finalPath, IOExtensions.EscapeNameToFileName(newName));
                    if (IsFolder)
                    {
                        IOUtilities.DirectoryMove(finalPath, newPath);
                    }
                    else
                    {
                        IOUtilities.FileMove(finalPath, newPath, true);
                    }
                }
                else
                {
                    // rename only
                    if (options.EnsureUniqueName)
                    {
                        // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                        newName = ((LocalItem)Parent).GetNewChildName(newName);
                    } // else overwrite

                    finalPath = Entry.GetFinalPath();
                    var escaped = IOExtensions.EscapeNameToFileName(newName);
                    FileSystemEntry.RenameByPath(Entry.GetFinalPath(), escaped);
                    finalPath = Path.Combine(Path.GetDirectoryName(finalPath), escaped);
                }

                // reload what we changed
                var id = FileSystemEntry.GetIdWithVolumeGuid(finalPath);
                newItem = await System.GetLocalItemAsync(id.Item2).ConfigureAwait(false);
            }

            var newCreationTime = options.CreationTimeUtc.HasValue ? options.CreationTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
            var newLastAccessTime = options.LastAccessTimeUtc.HasValue ? options.LastAccessTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
            var newLastWriteTime = options.LastWriteTimeUtc.HasValue ? options.LastWriteTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
            var newAttributes = options.Attributes ?? 0;
            if (newCreationTime != DateTime.MinValue || newLastAccessTime != DateTime.MinValue || newLastWriteTime != DateTime.MinValue || newAttributes != 0)
            {
                FileSystemEntry.SetBasicInformationById(Entry.Volume.Guid, Entry.Id, newCreationTime, newLastAccessTime, newLastWriteTime, DateTime.MinValue, newAttributes);

                // reload what we changed
                newItem = await System.GetLocalItemAsync(Id).ConfigureAwait(false);
            }

            return newItem;
        }

        // come up with some unique name but that still looks like the original name
        private string GetNewChildName(string tentativeUnescapedName)
        {
            var escaped = IOExtensions.EscapeNameToFileName(tentativeUnescapedName);
            var finalPath = Entry.GetFinalPath();
            return Conversions.GetNewFileName(escaped, (s) => FileSystemEntry.ExistsByPath(Path.Combine(finalPath, s)));
        }

        // IFileInfo
        public long Length => Entry.Size;

        public Task<Stream> OpenReadAsync(long? offset, long? count)
        {
            if (IsFolder)
                throw new InvalidOperationException();

            System.Log("Item: " + Id + " offset :" + offset + " count :" + count);
            var stream = new FileStream(Entry.GetFinalPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (offset != null)
            {
                stream.Seek(offset.Value, SeekOrigin.Begin);
            }

            // we don't support count
            return Task.FromResult<Stream>(stream);
        }

        public async Task WriteAsync(Stream stream)
        {
            if (IsFolder)
                throw new InvalidOperationException();

            System.Log("Item: " + Id + " '");
            FileSystemEntry.Unprotect(Entry.Volume.Guid, Id);
            using (var file = new FileStream(Entry.GetFinalPath(), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await stream.CopyToAsync(file).ConfigureAwait(false);
            }
        }

        public Task<Stream> OpenThumbnailReadAsync(int width, long? offset, long? count)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (IsFolder)
                throw new InvalidOperationException();

            System.Log("Item: " + Id + " width :" + width + " offset :" + offset + " count :" + count);
            var ext = Path.GetExtension(EscapedName);
            if (!IsSupportedThumbnailFile(ext))
                throw new InvalidOperationException("Thumbnail are unsupported for extension '" + ext + "'.");

            // build some unique key & cache path
            var key = Conversions.ComputeGuidHash(Id + "\0" + Length + "\0" + LastWriteTimeUtc.Ticks + "\0" + Name);
            var file = Path.Combine(Path.GetTempPath(), "CloudFolderImages", key.ToString("N") + "." + width + ext);
            if (!FileSystemEntry.FileExistsByPath(file))
            {
                // thumbnail doesn't exists yet
                // ensure directory exists
                FileSystemEntry.CreateDirectoryByPath(Path.GetDirectoryName(file), false);
                using (var stream = new FileStream(Entry.GetFinalPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var input = Image.FromStream(stream))
                    {
                        using (var bmp = ImageUtilities.ResizeImageByWidth(input, width))
                        {
                            IOUtilities.WrapSharingViolations(() => bmp.Save(file));
                        }
                    }
                }
            }

            var output = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (offset.HasValue)
            {
                output.Seek(offset.Value, SeekOrigin.Begin);
                // note we don't support count
            }
            return Task.FromResult<Stream>(output);
        }

        private static bool IsSupportedThumbnailFile(string ext)
        {
            if (string.IsNullOrWhiteSpace(ext))
                return false;

            ext = ext.ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif" || ext == ".png" || ext == ".tif" || ext == ".tiff";
        }

        // IFolderInfo
        public bool IsRoot => Id == System.ItemsEntry.Id;


        public async Task<IFileSystemInfo> CreateAsync(string name, CreateOptions options = null)
        {
            if (!IsFolder)
                throw new InvalidOperationException();

            var item = await CreateLocalItemAsync(this, name, options).ConfigureAwait(false);
            return item;
        }

        private async Task<LocalItem> CreateLocalItemAsync(LocalItem parentItem, string unescapedName, CreateOptions options = null)
        {
            if (parentItem == null)
                throw new ArgumentNullException(nameof(parentItem));

            if (unescapedName == null)
                throw new ArgumentNullException(nameof(unescapedName));

            System.Log("Parent: " + parentItem.Id + " '" + parentItem.Name + "' name: '" + unescapedName + "'");
            options ??= new CreateOptions();
            var item = parentItem.GetLocalItemByName(unescapedName);
            string escapedName;
            if (item != null && options.EnsureUniqueName)
            {
                unescapedName = parentItem.GetNewChildName(unescapedName);
                escapedName = unescapedName;
                item = null;
            }
            else
            {
                escapedName = IOExtensions.EscapeNameToFileName(unescapedName);
            }

            var newPath = Path.Combine(parentItem.Entry.GetFinalPath(), escapedName);
            if (item == null)
            {
                if (options.Attributes.HasFlag(FileAttributes.Directory))
                {
                    IOUtilities.DirectoryCreate(newPath);
                }
                else
                {
                    File.WriteAllBytes(newPath, Array.Empty<byte>());
                }

                item = parentItem.GetLocalItemByName(unescapedName);
            }
            else if (!options.Overwrite)
                return null;

            if (!item.IsFolder && options.InputStream != null)
            {
                using (var stream = new FileStream(Entry.GetFinalPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    await options.InputStream.CopyToAsync(stream).ConfigureAwait(false);
                }

                // refresh
                item = parentItem.GetLocalItemByName(unescapedName);
            }

            return item;
        }

        public async IAsyncEnumerable<IFileSystemInfo> EnumerateAsync(EnumerateOptions options = null)
        {
            if (!IsFolder)
                throw new InvalidOperationException();

            //System.Log("Item: " + Id + " options :" + options);
            options ??= new EnumerateOptions();
            var enumerate = Enumerate(options);
            if (options.FoldersFirst || options.SortByName)
            {
                enumerate = enumerate.OrderBy(i => i, new Comparer(options));
            }

            foreach (var child in enumerate)
            {
                yield return child;
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private class Comparer : IComparer<LocalItem>
        {
            private readonly bool _foldersFirst;

            public Comparer(EnumerateOptions options)
            {
                _foldersFirst = options.FoldersFirst;
            }

            public int Compare(LocalItem x, LocalItem y)
            {
                if (_foldersFirst)
                {
                    if (x.IsFolder && !y.IsFolder)
                        return -1;

                    if (y.IsFolder)
                        return 1;
                }

                // we're here, so we end up sorting by name anyway
                return string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private IEnumerable<LocalItem> Enumerate(EnumerateOptions options)
        {
            foreach (var child in Entry.EnumerateChildren())
            {
                if (child.Attributes.HasFlag(FileAttributes.Hidden) && !options.IncludeHidden)
                    continue;

                if (child.Attributes.HasFlag(FileAttributes.Directory) && !options.IncludeFolders)
                    continue;

                if (!child.Attributes.HasFlag(FileAttributes.Directory) && !options.IncludeFiles)
                    continue;

                if (LocalFileSystem.IsTempFile(child.Name) && !options.IncludeTemps)
                    continue;

                yield return new LocalItem(System, child);
            }
        }
    }
}
