using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using ShellBoost.Samples.CloudFolderSite.FileSystem;
using ShellBoost.Samples.CloudFolderSite.Utilities;

namespace ShellBoost.Samples.CloudFolderSite.Controllers
{
    [ApiController]
    public class FileSystemController : Controller
    {
        public FileSystemController(
            IFileSystem fileSystem,
            IContentTypeProvider contentTypeProvider,
            ILogger<FileSystemController> logger)
        {
            FileSystem = fileSystem;
            ContentTypeProvider = contentTypeProvider;
            Logger = logger;
        }

        public IFileSystem FileSystem { get; }
        public IContentTypeProvider ContentTypeProvider { get; }
        public ILogger<FileSystemController> Logger { get; }

        private void Log(string text, [CallerMemberName] string methodName = null) => Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ": " + methodName + ": " + text);
        private void LogWarning(string text, [CallerMemberName] string methodName = null) => Logger.LogWarning(Thread.CurrentThread.ManagedThreadId + ": " + methodName + ": " + text);

        private async IAsyncEnumerable<object> Enumerate(Guid id, EnumerateOptions options = null)
        {
            Log("id: " + id + " includeFiles: " + options?.IncludeFiles + " includeFolders: " + options?.IncludeFolders + " includeHidden: " + options?.IncludeHidden);
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
                yield break;

            Log("item: " + item);
            if (!item.IsFolder())
                yield break;

            await foreach (var child in ((IFolderInfo)item).EnumerateAsync(options).ConfigureAwait(false))
            {
                Log(" child: " + child);
                yield return child;
            }
        }

        private static EnumerateOptions GetEnumerateOptions(string s, bool folders, bool files)
        {
            var options = new EnumerateOptions();
            if (s != null && s.IndexOf("includehidden:true", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                options.IncludeHidden = true;
            }

            options.IncludeFolders = folders;
            options.IncludeFiles = files;
            return options;
        }

        [HttpGet]
        [Route("api/info")]
        public object GetInfo()
        {
            var info = new
            {
                FileSystem = FileSystem.GetType().Name,
                Id = FileSystem.UniqueId,
                Bitness = IntPtr.Size == 4 ? "32-bit" : "64-bit",
                DateTime.UtcNow,
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                FileVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            };
            return info;
        }

        [HttpGet]
        [Route("api/get/{id}")]
        public async Task<object> Get(Guid id)
        {
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            Log("id: " + id + " item: " + item);
            return Json(item); // if item is null, this will return the null json, instead of a null string which will cause client's JsonSerializer to fail
        }

        [Route("api/get/{id}/all/{options?}")]
        public IAsyncEnumerable<object> GetAll(Guid id, string options) => Enumerate(id, GetEnumerateOptions(options, true, true));

        [Route("api/get/{id}/folders/{options?}")]
        public IAsyncEnumerable<object> GetFolders(Guid id, string options) => Enumerate(id, GetEnumerateOptions(options, true, false));

        [Route("api/get/{id}/files/{options?}")]
        public IAsyncEnumerable<object> GetFiles(Guid id, string options) => Enumerate(id, GetEnumerateOptions(options, false, true));

        private string GetContentType(string path)
        {
            ContentTypeProvider.TryGetContentType(path, out var contentType);
            if (string.IsNullOrWhiteSpace(contentType))
                return "application/octet-stream";

            return contentType;
        }

        [Route("api/download/{id}")]
        public async Task<IActionResult> Download(Guid id)
        {
            Log("id: " + id);
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
            {
                LogWarning("Item " + id + " was not found.");
                return Json(null);
            }

            if (item.IsFolder())
            {
                LogWarning("Item " + item.Id + " is not a folder.");
                return BadRequest();
            }

            var file = (IFileInfo)item;
            var ct = GetContentType(Path.GetExtension(item.Name));
            Log("ct: " + ct + " length: " + file.Length);
            var stream = await file.OpenReadAsync().ConfigureAwait(false);
            if (stream == null)
            {
                LogWarning("Item " + item.Id + " has no content/stream.");
                return Json(null);
            }

            return new FileStreamResult(stream, ct) { FileDownloadName = item.Name, LastModified = item.LastWriteTimeUtc.ToLocalTime(), EnableRangeProcessing = true };
        }

        [Route("api/move/{id}/{newParentId}/{options?}")]
        public async Task<object> Move(Guid id, Guid newParentId, string options)
        {
            Log("id: " + id + " newParentId: " + newParentId + " options: " + options);
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
            {
                LogWarning("Item " + id + " was not found.");
                return Json(null);
            }

            var moveOptions = new MoveOptions();
            if (options != null && options.IndexOf("copy:true", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                moveOptions.Copy = true;
            }
            return await item.MoveToAsync(newParentId, moveOptions).ConfigureAwait(false);
        }

        [Route("api/rename/{id}/{newName}")]
        public async Task<object> Rename(Guid id, string newName)
        {
            Log("id: " + id + " newName: " + newName);
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
            {
                LogWarning("Item " + id + " was not found.");
                return Json(null);
            }

            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest();

            var options = new UpdateOptions { Name = newName.Nullify() };
            options.EnsureUniqueName = true;
            return await item.UpdateAsync(options).ConfigureAwait(false);
        }

        [Route("api/delete/{id}/{options?}")]
        public async Task<bool> Delete(Guid id, string options)
        {
            Log("id: " + id + " options: " + options);
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            if (item == null)
            {
                LogWarning("Item " + id + " was not found.");
                return false;
            }

            var deleteOptions = new DeleteOptions();
            if (options != null && options.IndexOf("recursive:true", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                deleteOptions.Recursive = true;
            }

            var result = await item.DeleteAsync(deleteOptions).ConfigureAwait(false);
            Log("Deleted item " + item.Id + " result: " + result);
            return result;
        }

        private static bool IsMultipartContentType(string contentType) => !string.IsNullOrEmpty(contentType) && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        private static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            return string.IsNullOrWhiteSpace(boundary) || boundary.Length > 70 ? null : boundary;
        }

        [Route("api/upload")]
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit] // note we added this to disable the 30M request limit https://github.com/aspnet/Announcements/issues/267
        public async Task<IActionResult> Upload()
        {
            Log(string.Empty);
            if (!IsMultipartContentType(Request.ContentType))
            {
                LogWarning("Content is not multipart.");
                return BadRequest();
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
            if (boundary == null)
            {
                LogWarning("Boundary was not found.");
                return BadRequest();
            }

            // note we expect the json document to arrive before the file document
            UpdateRequest updateRequest = null;
            MultipartSection dataSection = null;
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            do
            {
                var section = await reader.ReadNextSectionAsync().ConfigureAwait(false);
                if (section == null)
                    break;

                if (string.IsNullOrWhiteSpace(section.ContentType))
                    continue;

                if (updateRequest == null)
                {
                    if (!MediaTypeHeaderValue.TryParse(new StringSegment(section.ContentType), out var mediaType) ||
                        string.IsNullOrWhiteSpace(mediaType.MediaType.Value))
                        continue;

                    if (mediaType.MediaType.Value.Contains("/json"))
                    {
                        try
                        {
                            updateRequest = await JsonSerializer.DeserializeAsync<UpdateRequest>(section.Body).ConfigureAwait(false);
                            Log("Request id : " + updateRequest.Id + " name: " + updateRequest.Name + " parentId: " + updateRequest.ParentId + " attributes: " + updateRequest.Attributes);
                        }
                        catch (Exception e)
                        {
                            LogWarning("Invalid json document: " + e.Message);
                            return BadRequest();
                        }
                    }
                    continue;
                }
                else if (dataSection == null)
                {
                    if (!section.ContentType.Equals("application/octet-stream"))
                        continue;

                    dataSection = section;
                    break; // don't continue, we don't want to read the rest of stream
                }
            }
            while (true);
            if (updateRequest == null)
            {
                LogWarning("Request was not found.");
                return BadRequest();
            }

            var updateOptions = UpdateOptions.FromUpdateRequest(updateRequest);
            if (updateRequest.Id == Guid.Empty)
            {
                // creation
                var parent = await FileSystem.GetItemAsync(updateRequest.ParentId).ConfigureAwait(false);
                if (parent == null)
                {
                    LogWarning("Parent " + updateRequest.ParentId + " was not found.");
                    return BadRequest();
                }

                if (!parent.IsFolder())
                {
                    LogWarning("Parent " + parent.Id + " is not a folder.");
                    return BadRequest();
                }

                var createOptions = new CreateOptions();
                if (dataSection != null)
                {
                    createOptions.InputStream = dataSection.Body;
                }

                createOptions.Attributes = updateRequest.Attributes;
                var created = await ((IFolderInfo)parent).CreateAsync(updateRequest.Name, createOptions).ConfigureAwait(false);

                if (updateOptions != null)
                {
                    await created.UpdateAsync(updateOptions).ConfigureAwait(false);
                }
                return Ok(created);
            }

            var item = await FileSystem.GetItemAsync(updateRequest.Id).ConfigureAwait(false);
            if (item == null)
            {
                LogWarning("Item " + updateRequest.Id + " was not found.");
                return BadRequest();
            }

            if (item.IsFolder())
            {
                if (dataSection != null)
                {
                    LogWarning("Item " + updateRequest.Id + " is a folder so it's content cannot be set.");
                    return BadRequest();
                }

                if (updateOptions != null)
                {
                    await item.UpdateAsync(updateOptions).ConfigureAwait(false);
                }
                return Ok(item);
            }

            var file = (IFileInfo)item;
            if (dataSection != null)
            {
                await file.WriteAsync(dataSection.Body).ConfigureAwait(false);
            }

            if (updateOptions != null)
            {
                await item.UpdateAsync(updateOptions).ConfigureAwait(false);
            }

            return Ok(item);
        }
    }
}
