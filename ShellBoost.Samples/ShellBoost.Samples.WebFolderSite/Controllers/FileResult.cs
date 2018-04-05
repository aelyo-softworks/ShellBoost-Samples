using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ShellBoost.Samples.WebFolderSite.Controllers
{
    public class FileResult : IHttpActionResult
    {
        public FileResult(string filePath)
            : this(filePath, null)
        {
        }

        public FileResult(string filePath, string contentType)
        {
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));

            FilePath = filePath;
            ContentType = contentType;
        }

        public string FilePath { get; }
        public string ContentType { get; }

        private static Encoding DetectEncoding(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.Default, true))
            {
                reader.Peek();
                return reader.CurrentEncoding;
            }
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(File.OpenRead(FilePath));

            var contentType = ContentType ?? MimeMapping.GetMimeMapping(FilePath);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            if (contentType != null && contentType.StartsWith("text/"))
            {
                var encoding = DetectEncoding(FilePath);
                if (encoding != Encoding.Default)
                {
                    response.Content.Headers.ContentType.CharSet = encoding.WebName;
                }
            }

            return Task.FromResult(response);
        }
    }
}