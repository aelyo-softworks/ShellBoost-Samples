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
using ShellBoost.Core.Utilities;
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
            fileSystem.Logger = Logger;
        }

        public IFileSystem FileSystem { get; }
        public IContentTypeProvider ContentTypeProvider { get; }
        public ILogger<FileSystemController> Logger { get; }

        private void Log(string text, [CallerMemberName] string methodName = null) => Logger.LogInformation(Thread.CurrentThread.ManagedThreadId + ": " + methodName + ": " + text);
        private void LogWarning(string text, [CallerMemberName] string methodName = null) => Logger.LogWarning(Thread.CurrentThread.ManagedThreadId + ": " + methodName + ": " + text);

        private static IDictionary<string, string> ParseOptions(string options) => DictionarySerializer<string>.Deserialize(options, separator: '|', assignment: ':');

        private async IAsyncEnumerable<object> Enumerate(Guid id, EnumerateOptions options = null)
        {
            Log("id: " + id + " includeFiles: " + options?.IncludeFiles + " includeFolders: " + options?.IncludeFolders + " includeHidden: " + options?.IncludeHidden + " foldersFirst: " + options?.FoldersFirst + " sortByName: " + options?.SortByName);
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

        private static EnumerateOptions GetEnumerateOptions(string options, bool folders, bool files)
        {
            var enumOptions = new EnumerateOptions();
            enumOptions.IncludeFolders = folders;
            enumOptions.IncludeFiles = files;
            enumOptions.IncludeHidden = ParseOptions(options).GetValue(nameof(EnumerateOptions.IncludeHidden), false);
            enumOptions.IncludeTemps = ParseOptions(options).GetValue(nameof(EnumerateOptions.IncludeTemps), false);
            enumOptions.FoldersFirst = ParseOptions(options).GetValue(nameof(EnumerateOptions.FoldersFirst), false);
            enumOptions.SortByName = ParseOptions(options).GetValue(nameof(EnumerateOptions.SortByName), false);
            return enumOptions;
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
                RootId = Guid.Empty,
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration,
                FileVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
                Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            };
            return info;
        }

        [HttpGet]
        [Route("api/changes/{startTime}")]
        public IAsyncEnumerable<object> Get(DateTime startTime) => FileSystem.EnumerateChangesAsync(startTime);

        [HttpGet]
        [Route("api/get/{id}")]
        public async Task<object> Get(Guid id)
        {
            var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
            Log("id: " + id + " item: " + item);
            return Json(item); // if item is null, this will return the null json, instead of a null string which will cause client's JsonSerializer to fail
        }

        [HttpGet]
        [Route("api/getchild/{parentId}")]
        public async Task<object> GetChild(Guid parentId, [FromQuery] string name)
        {
            if (name == null)
                return BadRequest();

            var item = await FileSystem.GetItemAsync(parentId).ConfigureAwait(false);
            Log("parentId: " + parentId + " item: " + item + " name:" + name);
            if (item == null)
            {
                LogWarning("Item " + parentId + " was not found.");
                return Json(null);
            }

            if (!item.IsFolder())
            {
                LogWarning("Item " + item.Id + " is not a folder.");
                return BadRequest();
            }

            var folder = (IFolderInfo)item;
            var child = await folder.GetByNameAsync(name).ConfigureAwait(false);
            return Json(child); // if item is null, this will return the null json, instead of a null string which will cause client's JsonSerializer to fail
        }

        [Route("api/get/{id}/all")]
        public IAsyncEnumerable<object> GetAll(Guid id, [FromQuery] string options) => Enumerate(id, GetEnumerateOptions(options, true, true));

        [Route("api/get/{id}/folders")]
        public IAsyncEnumerable<object> GetFolders(Guid id, [FromQuery] string options) => Enumerate(id, GetEnumerateOptions(options, true, false));

        [Route("api/get/{id}/files")]
        public IAsyncEnumerable<object> GetFiles(Guid id, [FromQuery] string options) => Enumerate(id, GetEnumerateOptions(options, false, true));

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
            try
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
                    LogWarning("Item " + item.Id + " is a folder and cannot be downloaded.");
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [Route("api/move/{id}/{newParentId}")]
        public async Task<object> Move(Guid id, Guid newParentId, [FromQuery] string options)
        {
            try
            {
                Log("id: " + id + " newParentId: " + newParentId + " options: " + options);
                var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
                if (item == null)
                {
                    LogWarning("Item " + id + " was not found.");
                    return Json(null);
                }

                var moveOptions = new MoveOptions();
                moveOptions.Copy = ParseOptions(options).GetValue("copy", false);
                return await item.MoveToAsync(newParentId, moveOptions).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [Route("api/rename/{id}")]
        public async Task<object> Rename(Guid id, [FromQuery] string newName, [FromQuery] string options)
        {
            try
            {
                Log("id: " + id + " newName: " + newName + " options: " + options);
                var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
                if (item == null)
                {
                    LogWarning("Item " + id + " was not found.");
                    return Json(null);
                }

                if (string.IsNullOrWhiteSpace(newName))
                    return BadRequest();

                var op = ParseOptions(options);
                var updateOptions = new UpdateOptions { Name = newName.Nullify() };
                updateOptions.EnsureUniqueName = true; // this is currently not an option

                // move?
                if (op.TryGetValue("pid", out var pid) & Conversions.TryChangeType(pid, out Guid parentId))
                {
                    updateOptions.ParentId = parentId;
                }
                updateOptions.Overwrite = op.GetValue("overwrite", false);
                return await item.UpdateAsync(updateOptions).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        [Route("api/delete/{id}")]
        public async Task<object> Delete(Guid id, [FromQuery] string options)
        {
            try
            {
                Log("id: " + id + " options: " + options);
                var item = await FileSystem.GetItemAsync(id).ConfigureAwait(false);
                if (item == null)
                {
                    LogWarning("Item " + id + " was not found.");
                    return false;
                }

                var deleteOptions = new DeleteOptions();
                deleteOptions.Recursive = ParseOptions(options).GetValue("recursive", false);

                var result = await item.DeleteAsync(deleteOptions).ConfigureAwait(false);
                Log("Deleted item " + item.Id + " result: " + result);
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }

        private static bool IsMultipartContentType(string contentType) => !string.IsNullOrEmpty(contentType) && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
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
            try
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

                    createOptions.Overwrite = updateOptions?.Overwrite == true;
                    createOptions.EnsureUniqueName = true;
                    createOptions.Attributes = updateRequest.Attributes ?? FileAttributes.Normal;
                    var created = await ((IFolderInfo)parent).CreateAsync(updateRequest.Name, createOptions).ConfigureAwait(false);
                    if (created == null)
                        return Conflict(); // already exists

                    if (updateOptions != null)
                    {
                        updateOptions.Name = created.Name;
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }
}
