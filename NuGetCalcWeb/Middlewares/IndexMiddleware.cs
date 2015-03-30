using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGetCalcWeb.RazorSupport;

namespace NuGetCalcWeb.Middlewares
{
    public class IndexMiddleware : OwinMiddleware
    {
        public IndexMiddleware(OwinMiddleware next) : base(next) { }

        private static byte[] body;
        private static string etag;

        public override async Task Invoke(IOwinContext context)
        {
            if (body == null)
            {
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream, ResponseHelper.DefaultEncoding))
                {
                    RazorHelper.Run(context, writer, "Index");
                    await writer.FlushAsync().ConfigureAwait(false);
                    body = stream.ToArray();
                    stream.Position = 0;
                    using (var md5 = MD5.Create())
                    {
                        var b = md5.ComputeHash(stream);
                        etag = string.Concat(b.Select(x => x.ToString("x2")));
                    }
                }
                context.Request.CallCancelled.ThrowIfCancellationRequested();
            }

            var res = context.Response;
            res.ETag = string.Format("\"{0}\"", etag);

            var ifNoneMatch = context.Request.Headers.GetCommaSeparatedValues("If-None-Match");
            if (ifNoneMatch != null && ifNoneMatch.Any(x => x == "*" || x == etag))
            {
                res.StatusCode = 304;
            }
            else
            {
                res.ContentType = "text/html; charset=utf-8";
                res.ContentLength = body.LongLength;
                if (!context.Request.IsHeadRequest())
                    await res.WriteAsync(body).ConfigureAwait(false);
            }
        }
    }
}
