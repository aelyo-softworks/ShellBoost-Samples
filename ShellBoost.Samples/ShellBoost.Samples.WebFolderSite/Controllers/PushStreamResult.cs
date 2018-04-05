using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ShellBoost.Samples.WebFolderSite.Controllers
{
    public class PushStreamResult : IHttpActionResult
    {
        public PushStreamResult(Action<Stream> writeAction, string fileName)
        {
            if (writeAction == null)
                throw new ArgumentNullException(nameof(writeAction));

            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            WriteAction = writeAction;
            FileName = fileName;
        }

        public Action<Stream> WriteAction { get; }
        public string FileName { get; }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new PushStreamContent((stream, content, context) =>
                {
                    WriteAction(stream);
                })
            };

            msg.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(Path.GetExtension(FileName)));
            msg.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = FileName };
            return Task.FromResult(msg);
        }
    }
}