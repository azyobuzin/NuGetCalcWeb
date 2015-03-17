using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LightNode.Server;
using Microsoft.Owin;
using Newtonsoft.Json;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb
{
    public class NuGetCalcWebMiddleware : OwinMiddleware
    {
        private static readonly Encoding encoding = new UTF8Encoding(false);

        public NuGetCalcWebMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                switch (context.Request.Path.Value)
                {
                    case "/":
                        Index(context);
                        break;
                    case "/index.html":
                        IndexHtml(context);
                        break;
                    default:
                        await this.Next.Invoke(context).ConfigureAwait(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                var accept = context.Request.Headers.GetCommaSeparatedValues("Accept");
                var isHtmlRequired = accept != null
                    && accept.Any(x => x.StartsWith("text/html", StringComparison.OrdinalIgnoreCase));

                int statusCode;
                var model = new ErrorModel();

                if (ex is MethodNotAllowdException)
                {
                    statusCode = 405;
                    model.Header = "MethodNotAllowed";
                    model.Detail = (ex as MethodNotAllowdException).Method + " method is not allowed";
                }
                else if (ex is OperationNotFoundException)
                {
                    statusCode = 404;
                    model.Header = "NotFound";
                }
                else if (ex is OperationMissingException)
                {
                    statusCode = 400;
                    model.Header = "BadRequest";
                    model.Detail = ex.ToString();
                }
                else
                {
                    Trace.TraceError("{0}: {1}", context.Request.Path, ex);
                    statusCode = 500;
                    model.Header = "InternalServerError";
                    model.Detail = ex.ToString();
                }

                context.Response.StatusCode = statusCode;
                if (isHtmlRequired)
                {
                    context.Response.View("Error", model);
                }
                else
                {
                    context.Response.Json(model);
                }
            }
        }

        private void Index(IOwinContext context)
        {
            //TODO: caching
            context.Response.View("Index");
        }

        private void IndexHtml(IOwinContext context)
        {
            context.Response.StatusCode = 301;
            context.Response.Headers.Set("Location", "/");
        }
    }
}