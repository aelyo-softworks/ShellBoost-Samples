using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using SevenZip;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;

namespace ShellBoost.Samples.SevenZipFolder
{
    // utility class to transform and represent SevenZipExtractor flat model to a hierarchical model
    public class ArchiveRootFolderInfo : ArchiveFolderInfo
    {
        private readonly ConcurrentDictionary<string, ArchiveFolderInfo> _folders = new ConcurrentDictionary<string, ArchiveFolderInfo>(StringComparer.OrdinalIgnoreCase);

        public ArchiveRootFolderInfo(string filePath)
            : base(null, null)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            FilePath = filePath;
            using (var extractor = new SevenZipExtractor(filePath))
            {
                foreach (var data in extractor.ArchiveFileData)
                {
                    if (data.IsDirectory)
                        continue;

                    var dir = Path.GetDirectoryName(data.FileName);
                    if (!_folders.TryGetValue(dir, out var folder))
                    {
                        var segments = dir.Split(Path.DirectorySeparatorChar);
                        folder = EnsureFolder(this, segments);
                        _folders[dir] = folder;
                    }
                    folder.AddFile(data);
                }
            }
        }

        public string FilePath { get; }
        public override bool IsRoot => true;

        private static ArchiveFolderInfo EnsureFolder(ArchiveFolderInfo parent, string[] segments)
        {
            ArchiveFolderInfo folder;
            if (segments.Length == 1)
            {
                if (segments[0].Length == 0)
                {
                    folder = parent;
                }
                else
                {
                    folder = parent.Folders.FirstOrDefault(f => string.Compare(f.Name, segments[0], StringComparison.OrdinalIgnoreCase) == 0);
                    if (folder == null)
                    {
                        folder = new ArchiveFolderInfo(parent, segments[0]);
                        parent.AddFolder(folder);
                    }
                }
            }
            else // > 1
            {
                var child = EnsureFolder(parent, segments.Take(segments.Length - 1).ToArray());
                var lastSegment = segments[segments.Length - 1];
                folder = child.Folders.FirstOrDefault(f => string.Compare(f.Name, lastSegment, StringComparison.OrdinalIgnoreCase) == 0);
                if (folder == null)
                {
                    folder = new ArchiveFolderInfo(child, lastSegment);
                    child.AddFolder(folder);
                }
            }
            return folder;
        }

        public ArchiveFolderInfo GetFolder(string folderPath)
        {
            if (folderPath == null)
                throw new ArgumentNullException(null, nameof(folderPath));

            _folders.TryGetValue(folderPath, out var folder);
            return folder;
        }

        public ShellContent ExtractFile(ArchiveFileInfo file)
        {
            using (var extractor = new SevenZipExtractor(FilePath))
            {
                var content = new ArchiveFileContent(file);
                
                // unfortunately, the extractor doesn't seem to support a pull model.
                // so we push the data into memory first
                // we could also use a physical file as a temporary cache. it maybe better...
                extractor.ExtractFile(file.Index, content.Stream);

                // rewind
                content.Stream.Position = 0;
                return content;
            }
        }

        private class ArchiveFileContent : ShellContent
        {
            public ArchiveFileContent(ArchiveFileInfo file)
            {
                File = file;
                CreationTime = file.CreationTime;
                LastAccessTime = file.LastAccessTime;
                LastWriteTime = file.LastWriteTime;
                Stream = new ChunkedMemoryStream();
            }

            public ArchiveFileInfo File { get; }
            public ChunkedMemoryStream Stream { get; }
            public override long Length => unchecked((long)File.Size);

            public override int Read(byte[] array, int count) => Stream.Read(array, 0, count);
            public override long? Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
        }
    }
}
