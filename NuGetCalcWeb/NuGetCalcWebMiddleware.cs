using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
            var error = new ErrorModel();
            try
            {
                switch (context.Request.Path.Value)
                {
                    case "/":
                        Index(context);
                        return;
                }

                error.StatusCode = 404;
                error.Header = "Not Found";
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}: {1}", context.Request.Path, ex);
                error.StatusCode = 500;
                error.Header = "Internal Server Error";
                error.Detail = ex.ToString();
            }

            context.Response.StatusCode = error.StatusCode;

            var accept = context.Request.Headers.GetCommaSeparatedValues("Accept");
            var isHtmlRequired = accept != null
                && accept.Any(x => x.StartsWith("text/html", StringComparison.OrdinalIgnoreCase));
            if (isHtmlRequired)
                context.Response.View("Error", error);
            else
                context.Response.Json(error);
        }

        private void Index(IOwinContext context)
        {
            //TODO: caching
            context.Response.View("Index");
        }
    }
}