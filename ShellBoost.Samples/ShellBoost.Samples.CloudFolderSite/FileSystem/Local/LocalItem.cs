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

            if (!IsFolder)
                throw new InvalidOperationException();

            System.Log("Item: " + Id + " name: " + name);
            var entry = GetLocalItemByName(name);
            var item = await Task.FromResult(entry).ConfigureAwait(false);
            System.Log("=> Item: " + item?.Id + " name: '" + item?.Name + "'");
            return item;
        }

        private LocalItem GetLocalItemByName(string unescapedName)
        {
            var escaped = IOExtensions.EscapeNameToFileName(unescapedName);
            var entry = Entry.EnumerateChildren().FirstOrDefault(e => e.Name.EqualsIgnoreCase(escaped));
            if (entry == null)
                return null;

            return new LocalItem(System, entry);
        }

        private static T WithAttributesUnset<T>(string filePath, Func<FileAttributes, T> action)
        {
            var atts = FileSystemEntry.GetAttributesByPath(filePath);
            var newAtts = atts;
            newAtts &= ~FileAttributes.ReadOnly;
            newAtts &= ~FileAttributes.Hidden;
            if (newAtts == atts)
                return action(atts);

            FileSystemEntry.SetAttributesByPath(filePath, newAtts);
            try
            {
                return action(atts);
            }
            finally
            {
                FileSystemEntry.SetAttributesByPath(filePath, atts);
            }
        }

        private static void WithAttributesUnset(string filePath, Action<FileAttributes> action)
        {
            var atts = FileSystemEntry.GetAttributesByPath(filePath);
            var newAtts = atts;
            newAtts &= ~FileAttributes.ReadOnly;
            newAtts &= ~FileAttributes.Hidden;
            if (newAtts == atts)
            {
                action(atts);
                return;
            }

            try
            {
                action(atts);
            }
            finally
            {
                FileSystemEntry.SetAttributesByPath(filePath, atts);
            }
        }

        public Task<bool> DeleteAsync(DeleteOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " options: " + options);
            var parent = Parent;
            System.DoWithoutLocalEvents(() =>
            {
                if (IsFolder)
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.DirectoryDelete(Entry.GetFinalPath(), true));
                }
                else
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.FileDelete(Entry.GetFinalPath(), true));
                }
            });

            if (!LocalFileSystem.IsTempFile(Name))
            {
                System.AddEvent(Id, ParentId, WatcherChangeTypes.Deleted);
                if (parent != null)
                {
                    System.AddEvent(parent.Id, parent.ParentId, WatcherChangeTypes.Changed);
                }
            }
            return Task.FromResult(true);
        }

        public async Task<IFileSystemInfo> MoveToAsync(Guid newParentId, MoveOptions options = null)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " newParentId: " + newParentId + " options: " + options);
            var parent = await System.GetLocalItemAsync(newParentId).ConfigureAwait(false);
            if (parent == null)
                throw new InvalidOperationException();

            if (!parent.IsFolder)
                throw new InvalidOperationException();

            options ??= new MoveOptions();
            if (options.Copy)
                return CopyTo(this, parent, options);

            if (ParentId == newParentId)
                return this;

            var oldParent = Parent;
            var sourcePath = Entry.GetFinalPath();
            var escaped = EscapedName;
            var targetDir = parent.Entry.GetFinalPath();
            var targetPath = Path.Combine(targetDir, escaped);

            System.DoWithoutLocalEvents(() =>
            {
                if (options.Overwrite)
                {
                    IOUtilities.WrapSharingViolations(() => FileSystemEntry.DeleteByPath(targetPath, true, false));
                }
                else if (options.EnsureUniqueName)
                {
                    // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                    var newName = parent.GetNewChildName(Name);
                    escaped = IOExtensions.EscapeNameToFileName(newName);
                    targetPath = Path.Combine(targetDir, escaped);
                }

                if (IsFolder)
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.DirectoryMove(sourcePath, targetPath));
                }
                else
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.FileMove(Entry.GetFinalPath(), targetPath, true));
                }
            });

            var newItem = parent.GetLocalItemByName(IOExtensions.UnescapeFileNameToName(escaped));
            if (!LocalFileSystem.IsTempFile(Name) && newItem != null)
            {
                System.AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Changed, null, oldParent?.Id);
                if (oldParent != null)
                {
                    System.AddEvent(oldParent.Id, oldParent.ParentId, WatcherChangeTypes.Changed);
                }
                System.AddEvent(parent.Id, parent.ParentId, WatcherChangeTypes.Changed);
            }

            System.Log("=> New Item: " + newItem?.Id + " name: '" + newItem?.Name + "'");
            return newItem;
        }

        private LocalItem CopyTo(LocalItem item, LocalItem newParent, MoveOptions options)
        {
            //var newPath = Path.Combine(newParent.Entry.GetFinalPath(), item.EscapedName);
            var sourcePath = item.Entry.GetFinalPath();
            var escaped = item.EscapedName;
            var targetDir = newParent.Entry.GetFinalPath();
            var targetPath = Path.Combine(targetDir, escaped);

            System.DoWithoutLocalEvents(() =>
            {
                if (options.Overwrite)
                {
                    IOUtilities.WrapSharingViolations(() => FileSystemEntry.DeleteByPath(targetPath, true, false));
                }
                else if (options.EnsureUniqueName)
                {
                    // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                    var newName = newParent.GetNewChildName(item.Name);
                    escaped = IOExtensions.EscapeNameToFileName(newName);
                    targetPath = Path.Combine(targetDir, escaped);
                }


                if (item.IsFolder)
                {
                    IOUtilities.WrapSharingViolations(() => IOExtensions.DirectoryCopy(sourcePath, targetPath));
                }
                else
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.FileOverwrite(sourcePath, targetPath, true));
                }
            });

            var newItem = newParent.GetLocalItemByName(IOExtensions.UnescapeFileNameToName(escaped));
            if (!LocalFileSystem.IsTempFile(item.Name) && newItem != null)
            {
                System.AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Created);
                System.AddEvent(newParent.Id, newParent.ParentId, WatcherChangeTypes.Changed);
            }

            System.Log("=> New Item: " + newItem?.Id + " name: '" + newItem?.Name + "'");
            return newItem;
        }

        public async Task<IFileSystemInfo> UpdateAsync(UpdateOptions options)
        {
            if (IsRoot)
                throw new UnauthorizedAccessException();

            System.Log("Item: " + Id + " '" + Name + "' attributes: " + Attributes + " options: " + options);

            var newItem = this;
            var changed = false;
            var renamed = false;
            string newName = null;
            var oldName = Name;
            Guid? oldParentId = null;

            await System.DoWithoutLocalEvents(async () =>
            {
                // move / rename
                if ((options.Name != null && options.Name != Name) || options.ParentId.HasValue)
                {
                    renamed = true;
                    newName = options.Name; // can in fact be the same (if only parent id has changed)
                    var escaped = IOExtensions.EscapeNameToFileName(newName);
                    var sourcePath = Entry.GetFinalPath();
                    string targetPath;

                    // new parent (move)?
                    if (options.ParentId.HasValue)
                    {
                        var newParent = await System.GetLocalItemAsync(options.ParentId.Value).ConfigureAwait(false);
                        if (newParent == null)
                            throw new InvalidOperationException();

                        targetPath = Path.Combine(newParent.Entry.GetFinalPath(), escaped);
                        if (options.Overwrite)
                        {
                            IOUtilities.WrapSharingViolations(() => FileSystemEntry.DeleteByPath(targetPath, true, false));
                        }
                        else if (options.EnsureUniqueName)
                        {
                            // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                            newName = newParent.GetNewChildName(newName);
                            escaped = IOExtensions.EscapeNameToFileName(newName);
                            targetPath = Path.Combine(newParent.Entry.GetFinalPath(), escaped);
                        } // else overwrite

                        if (IsFolder)
                        {
                            IOUtilities.WrapSharingViolations(() => IOUtilities.DirectoryMove(sourcePath, targetPath));
                        }
                        else
                        {
                            IOUtilities.WrapSharingViolations(() => IOUtilities.FileMove(sourcePath, targetPath, true));
                        }
                        oldParentId = options.ParentId.Value;
                    }
                    else
                    {
                        // rename only
                        targetPath = Path.Combine(Path.GetDirectoryName(sourcePath), escaped);
                        if (options.Overwrite)
                        {
                            IOUtilities.WrapSharingViolations(() => FileSystemEntry.DeleteByPath(targetPath, true, false));
                        }
                        else if (options.EnsureUniqueName)
                        {
                            // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                            newName = ((LocalItem)Parent).GetNewChildName(newName);
                            escaped = IOExtensions.EscapeNameToFileName(newName);
                            targetPath = Path.Combine(Path.GetDirectoryName(sourcePath), escaped);
                        } // else overwrite

                        IOUtilities.WrapSharingViolations(() => FileSystemEntry.RenameByPath(sourcePath, escaped));
                    }

                    // reload what we changed
                    var id = FileSystemEntry.GetIdWithVolumeGuid(targetPath);
                    newItem = await System.GetLocalItemAsync(id.Item2).ConfigureAwait(false);
                }

                var newCreationTime = options.CreationTimeUtc.HasValue ? options.CreationTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
                var newLastAccessTime = options.LastAccessTimeUtc.HasValue ? options.LastAccessTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
                var newLastWriteTime = options.LastWriteTimeUtc.HasValue ? options.LastWriteTimeUtc.Value.ToLocalTime() : DateTime.MinValue;
                var newAttributes = options.Attributes ?? 0;
                if (newCreationTime != DateTime.MinValue || newLastAccessTime != DateTime.MinValue || newLastWriteTime != DateTime.MinValue || newAttributes != 0)
                {
                    IOUtilities.WrapSharingViolations(() => FileSystemEntry.SetBasicInformationById(Entry.Volume.Guid, Entry.Id, newCreationTime, newLastAccessTime, newLastWriteTime, DateTime.MinValue, newAttributes));

                    changed = true;
                    // reload what we changed
                    newItem = await System.GetLocalItemAsync(Id).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            if (newItem != null && !LocalFileSystem.IsTempFile(newItem.Name))
            {
                if (renamed)
                {
                    System.AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Renamed, oldName, oldParentId);
                }

                if (changed)
                {
                    System.AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Changed, null, oldParentId);
                }

                System.AddEvent(newItem.ParentId, (newItem.Parent?.ParentId).GetValueOrDefault(), WatcherChangeTypes.Changed);
            }

            System.Log("=> New Item: " + newItem?.Id + " name: '" + newItem?.Name + "' changed: " + changed + " renamed: " + renamed);
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

            System.Log("Item: " + Id + " offset: " + offset + " count: " + count);
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
            if (IsRoot)
                throw new UnauthorizedAccessException();

            if (IsFolder)
                throw new InvalidOperationException();

            var path = Entry.GetFinalPath();

            // note: stream lengh = 0 here is normal, length will be updated when read from
            System.Log("Item: " + Id + " name: '" + Name + "' length: " + stream?.Length + " path: " + path);

            await System.DoWithoutLocalEvents(async () =>
            {
                await IOUtilities.WrapSharingViolationsAsync(async () =>
                {
                    await WithAttributesUnset(path, async (p) =>
                    {
                        using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            if (stream != null)
                            {
                                await stream.CopyToAsync(file).ConfigureAwait(false);
                            }
                        }
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);

            System.Log("=> length: " + stream?.Length + " position: " + stream?.Position);
            if (!LocalFileSystem.IsTempFile(Name))
            {
                System.AddEvent(Id, ParentId, WatcherChangeTypes.Changed);
            }
        }

        public Task<Stream> OpenThumbnailReadAsync(int width, long? offset, long? count)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));

            if (IsFolder)
                throw new InvalidOperationException();

            System.Log("Item: " + Id + " width: " + width + " offset: " + offset + " count: " + count);
            var ext = Path.GetExtension(EscapedName);
            if (!IsSupportedThumbnailFile(ext))
                throw new InvalidOperationException("Thumbnail are unsupported for extension '" + ext + "'.");

            // build some unique key & cache path
            var key = Conversions.ComputeGuidHash(Id + "\0" + Length + "\0" + LastWriteTimeUtc.Ticks + "\0" + Name);
            var file = Path.Combine(Path.GetTempPath(), "CloudFolderImages", key.ToString("N") + "." + width + ext);
            var path = Entry.GetFinalPath();
            if (!FileSystemEntry.FileExistsByPath(file) && FileSystemEntry.ExistsByPath(path))
            {
                // thumbnail doesn't exists yet
                // ensure directory exists
                FileSystemEntry.CreateDirectoryByPath(Path.GetDirectoryName(file), false);
                using (var stream = new FileStream(Entry.GetFinalPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (stream.Length > 0)
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
            }

            if (FileSystemEntry.ExistsByPath(file))
            {
                var output = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (output.Length > 0)
                {
                    if (offset.HasValue)
                    {
                        output.Seek(offset.Value, SeekOrigin.Begin);
                        // note we don't support count
                    }
                    return Task.FromResult<Stream>(output);
                }
            }
            return Task.FromResult<Stream>(null);
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


        public async Task<IFileSystemInfo> CreateAsync(string name, CreateOptions options = null) => await CreateLocalItemAsync(this, name, options).ConfigureAwait(false);
        private async Task<LocalItem> CreateLocalItemAsync(LocalItem parent, string unescapedName, CreateOptions options = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (!parent.IsFolder)
                throw new InvalidOperationException();

            if (unescapedName == null)
                throw new ArgumentNullException(nameof(unescapedName));

            System.Log("Parent: " + parent.Id + " '" + parent.Name + "' name: '" + unescapedName + "' stream: " + options?.InputStream?.Length);
            options ??= new CreateOptions();

            var escaped = IOExtensions.EscapeNameToFileName(unescapedName);
            var targetDir = parent.Entry.GetFinalPath();
            var targetPath = Path.Combine(targetDir, escaped);

            await System.DoWithoutLocalEvents(async () =>
            {
                if (options.Overwrite)
                {
                    IOUtilities.WrapSharingViolations(() => FileSystemEntry.DeleteByPath(targetPath, true, false));
                }
                else if (options.EnsureUniqueName)
                {
                    // from a file named "file.txt", this may create files like "file (2).txt", "file (3).txt", etc.
                    var newName = parent.GetNewChildName(Name);
                    escaped = IOExtensions.EscapeNameToFileName(newName);
                    targetPath = Path.Combine(targetDir, escaped);
                }

                if (options.Attributes.HasFlag(FileAttributes.Directory))
                {
                    IOUtilities.WrapSharingViolations(() => IOUtilities.DirectoryCreate(targetPath));
                }
                else
                {
                    await IOUtilities.WrapSharingViolationsAsync(async () =>
                    {
                        if (options.InputStream != null)
                        {
                            System.Log("Write length: " + options.InputStream.Length);
                            using (var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                            {
                                await options.InputStream.CopyToAsync(file).ConfigureAwait(false);
                            }
                            System.Log("Write length: " + options.InputStream.Length + " position: " + options.InputStream.Position);
                        }
                        else
                        {
                            System.Log("Write zero length");
                            File.WriteAllBytes(targetPath, Array.Empty<byte>());
                        }
                    }).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            // refresh
            var newItem = parent.GetLocalItemByName(IOExtensions.UnescapeFileNameToName(escaped));
            if (!LocalFileSystem.IsTempFile(Name) && newItem != null)
            {
                System.AddEvent(newItem.Id, newItem.ParentId, WatcherChangeTypes.Created);
                System.AddEvent(parent.Id, parent.ParentId, WatcherChangeTypes.Changed);
            }

            System.Log("=> New Item: " + newItem?.Id + " name: '" + newItem?.Name + "'");
            return newItem;
        }

        public async IAsyncEnumerable<IFileSystemInfo> EnumerateAsync(EnumerateOptions options = null)
        {
            if (!IsFolder)
                throw new InvalidOperationException();

            //System.Log("Item: " + Id + " options: " + options);
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
