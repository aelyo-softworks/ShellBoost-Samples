using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using ShellBoost.Core.Utilities;
using ShellBoost.Samples.CloudFolderSite.FileSystem;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.Pages
{
    public class IndexModel : PageModel
    {
        public IndexModel(
            IFileSystem fileSystem,
            IContentTypeProvider contentTypeProvider,
            ILogger<IndexModel> logger)
        {
            FileSystem = fileSystem;
            ContentTypeProvider = contentTypeProvider;
            Logger = logger;
        }

        public IFileSystem FileSystem { get; }
        public IContentTypeProvider ContentTypeProvider { get; }
        public ILogger<IndexModel> Logger { get; }

        public IFolderInfo Folder { get; set; }
        public IList<IFileSystemInfo> Children { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Children = new List<IFileSystemInfo>();
            var id = Conversions.ChangeType<Guid>(Request.Query["id"]);
            Folder = await FileSystem.GetItemAsync(id) as IFolderInfo;
            if (Folder == null)
                return NotFound();

            ViewData["Title"] = Folder.GetDisplayName();

            var options = new EnumerateOptions();
            options.IncludeHidden = true;
            options.FoldersFirst = true;
            await foreach (var child in Folder.EnumerateAsync(options))
            {
                Children.Add(child);
            }
            return Page();
        }

        private string GetContentType(string path)
        {
            ContentTypeProvider.TryGetContentType(path, out var contentType);
            if (string.IsNullOrWhiteSpace(contentType))
                return "application/octet-stream";

            return contentType;
        }

        public async Task<ActionResult> OnGetDownload(Guid id)
        {
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
                return NotFound();

            if (item.IsFolder())
                return BadRequest();

            var file = (IFileInfo)item;
            var ct = GetContentType(Path.GetExtension(item.Name));
            var stream = await file.OpenReadAsync(null, null).ConfigureAwait(false);
            if (stream == null)
            {
                // send a 0 size file
                stream = new MemoryStream();
            }

            return new FileStreamResult(stream, ct) { FileDownloadName = item.Name, LastModified = item.LastWriteTimeUtc.ToLocalTime(), EnableRangeProcessing = true };
        }
    }
}
