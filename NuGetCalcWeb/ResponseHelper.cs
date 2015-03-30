using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGetCalcWeb.RazorSupport;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb
{
    public static class ResponseHelper
    {
        public static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        private static async Task View(IOwinResponse response, string viewName, Type modelType, object model)
        {
            var context = response.Context;
            var cancel = context.Request.CallCancelled;
            cancel.ThrowIfCancellationRequested();
            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, DefaultEncoding))
            {
                RazorHelper.Run(context, writer, viewName, modelType, model, null);
                await writer.FlushAsync().ConfigureAwait(false);
                cancel.ThrowIfCancellationRequested();
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength = stream.Length;
                if (!context.Request.IsHeadRequest())
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(response.Body, 81920, cancel).ConfigureAwait(false);
                }
            }
        }

        public static Task View<T>(this IOwinResponse response, string viewName, T model)
        {
            return View(response, viewName, typeof(T), model);
        }

        public static Task View(this IOwinResponse response, string viewName)
        {
            return View(response, viewName, null, null);
        }

        public static Task Error(this IOwinResponse response, int statusCode, ErrorModel errorModel)
        {
            response.StatusCode = statusCode;
            return response.View("Error", errorModel);
        }
    }
}
