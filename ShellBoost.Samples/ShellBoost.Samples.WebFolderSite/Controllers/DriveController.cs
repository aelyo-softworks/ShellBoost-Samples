using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using ShellBoost.Samples.WebFolderSite.Model;

namespace ShellBoost.Samples.WebFolderSite.Controllers
{
    public class DriveController : ApiController
    {
        [Route("api/drive")]
        public Item Get() => Drive.Root.RootFolder;

        [Route("api/drive/{id}")]
        public Item Get(Guid id) => Drive.Root.GetItem(id);

        [Route("api/drive/{id}/children")]
        public IEnumerable<Item> GetChildren(Guid id)
        {
            var folder = Drive.Root.GetFolder(id);
            if (folder == null)
                yield break;

            foreach (var child in folder.Children)
            {
                yield return child;
            }
        }

        [Route("api/drive/{id}/folders")]
        public IEnumerable<Item> GetFolders(Guid id)
        {
            var folder = Drive.Root.GetFolder(id);
            if (folder == null)
                yield break;

            foreach (var child in folder.ChildFolders)
            {
                yield return child;
            }
        }

        [Route("api/drive/{id}/items")]
        public IEnumerable<Item> GetItems(Guid id)
        {
            var folder = Drive.Root.GetFolder(id);
            if (folder == null)
                yield break;

            foreach (var child in folder.ChildItems)
            {
                yield return child;
            }
        }

        [Route("api/drive/{id}/content")]
        [HttpGet]
        public IHttpActionResult Download(Guid id, string contentETag = null)
        {
            var item = Drive.Root.GetItem(id);
            if (item == null)
                return NotFound();

            if (item.Type == ItemType.Folder)
                return StatusCode(HttpStatusCode.NoContent);

            if (item.ContentETag == contentETag)
                return StatusCode(HttpStatusCode.NotModified);

            return new FileResult(item.FullPath);
        }

        [Route("api/drive")]
        [HttpPost]
        public IHttpActionResult Update([FromBody] Item value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.Name))
                return BadRequest("A valid Item must be provided.");

            var folder = Drive.Root.GetFolder(value.ParentId);
            if (folder == null)
                return BadRequest("Folder must be specified.");

            var item = folder.EnsureChildItem(value.Name);
            item.Update(value);
            return Ok(item);
        }

        [Route("api/drive/{id}")]
        [HttpPut]
        public async Task<IHttpActionResult> Upload(Guid id)
        {
            var item = Drive.Root.GetItem(id);
            if (item.Type == ItemType.Folder)
                return BadRequest("A valid Item must be provided.");

            using (var stream = await Request.Content.ReadAsStreamAsync())
            {
                using (var file = File.OpenWrite(item.FullPath))
                {
                    await stream.CopyToAsync(file);
                    return Ok();
                }
            }
        }

        [Route("api/drive/{id}")]
        [HttpDelete]
        public bool Delete(Guid id) => Drive.Root.DeleteItem(id);
    }
}