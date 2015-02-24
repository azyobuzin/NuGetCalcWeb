using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGetCalcWeb
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class IndexMiddleware
    {
        private static readonly byte[] content;

        static IndexMiddleware()
        {
            var analytics = "";
            var analyticsEnv = Environment.GetEnvironmentVariable("NUGETCALC_ANALYTICS");
            if (analyticsEnv != null && File.Exists(analyticsEnv))
                analytics = File.ReadAllText(analyticsEnv);

            var ad = "";
            var adEnv = Environment.GetEnvironmentVariable("NUGETCALC_AD");
            if (adEnv != null && File.Exists(adEnv))
                ad = File.ReadAllText(adEnv);

            var index = File.ReadAllText("index.html");
            index = index.Replace("<script>/*Analytics*/</script>", analytics)
                .Replace("<script>/*Ad*/</script>", ad);

            content = new UTF8Encoding(false).GetBytes(index);
        }

        public IndexMiddleware(AppFunc next)
        {
            this.next = next;
        }

        private AppFunc next;

        public async Task Invoke(IDictionary<string, object> environment)
        {
            try
            {
                var context = new OwinContext(environment);

                if (context.Request.Path.Value == "/")
                {
                    //var etag = content.GetHashCode().ToString();
                    //context.Response.ETag = etag;

                    //var etags = context.Request.Headers.GetCommaSeparatedValues("If-None-Match");
                    //if (etags != null && etags.Contains(etag))
                    //{
                    //    context.Response.StatusCode = 304;
                    //    return;
                    //}

                    context.Response.ContentType = "text/html; charset=utf-8";
                    context.Response.ContentLength = content.LongLength;
                    await context.Response.WriteAsync(content).ConfigureAwait(false);
                    return;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                throw;
            }

            await this.next(environment).ConfigureAwait(false);
        }
    }
}