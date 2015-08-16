using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb.Middlewares
{
    public class UploadMiddleware : OwinMiddleware
    {
        public UploadMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            var httpContent = new StreamContent(context.Request.Body);
            foreach (var kvp in context.Request.Headers)
                httpContent.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

            if (!httpContent.IsMimeMultipartContent("form-data"))
            {
                await context.Response.Error(415, new ErrorModel(
                    "Unsupported Media Type",
                     "You must upload nupkgs with multipart/form-data."
                )).ConfigureAwait(false);
                return;
            }

            var provider = await httpContent
                .ReadAsMultipartAsync(new MultipartFormDataStreamProvider(Path.GetTempPath()))
                .ConfigureAwait(false);
            var formData = provider.FormData;

            var hash = formData["hash"];
            var method = formData["method"];
            if (method == null)
            {
                await context.Response.Error(400, new ErrorModel(
                    "Bad Request",
                    "\"method\" parameter is required."
                )).ConfigureAwait(false);
                return;
            }

            var file = provider.FileData
                .FirstOrDefault(f => f.Headers.ContentDisposition.Name.Trim('"') == "file");

            if (file != null && new FileInfo(file.LocalFileName).Length > 0)
            {
                hash = await NuGetUtility.ExtractUploadedFile(file).ConfigureAwait(false);
            }
            else if (string.IsNullOrEmpty(hash))
            {
                await context.Response.Error(400, new ErrorModel(
                    "Bad Request",
                    "\"file\" parameter is required."
                )).ConfigureAwait(false);
                return;
            }

            var baseUriEnv = Environment.GetEnvironmentVariable("NUGETCALC_BASEURI");
            var baseUri = baseUriEnv != null
                ? new Uri(new Uri(baseUriEnv), context.Request.Path.Value)
                : context.Request.Uri;
            var redirectUri = new UriBuilder(new Uri(baseUri, method));
            redirectUri.Query = string.Join("&",
                Enumerable.Range(0, formData.Count)
                    .Select(i => Tuple.Create(formData.GetKey(i), formData.Get(i)))
                    .Where(t => t.Item1 != "method" && t.Item1 != "hash" && !string.IsNullOrEmpty(t.Item2))
                    .Concat(new[] { Tuple.Create("hash", hash) })
                    .Select(t => string.Concat(Uri.EscapeDataString(t.Item1), "=", Uri.EscapeDataString(t.Item2)))
            );

            context.Response.StatusCode = 303;
            context.Response.Headers.Set("Location", redirectUri.ToString());
        }
    }
}
