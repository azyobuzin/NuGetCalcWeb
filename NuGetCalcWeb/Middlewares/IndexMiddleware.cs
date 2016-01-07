using System.Threading.Tasks;
using Azyobuzi.OwinRazor;
using Microsoft.Owin;

namespace NuGetCalcWeb.Middlewares
{
    public class IndexMiddleware : OwinMiddleware
    {
        public IndexMiddleware(OwinMiddleware next) : base(next) { }

        private static byte[] body;

        public override async Task Invoke(IOwinContext context)
        {
            if (body == null)
            {
                body = ResponseHelper.DefaultEncoding.GetBytes(
                    await new Views.Index { Context = new TemplateExecutionContext(context) }.RunAsync().ConfigureAwait(false)
                );
                context.Request.CallCancelled.ThrowIfCancellationRequested();
            }

            var res = context.Response;
            res.ContentType = "text/html; charset=utf-8";
            if (context.RespondNotModified()) return;
            res.ContentLength = body.LongLength;
            if (!context.Request.IsHeadRequest())
                await res.WriteAsync(body).ConfigureAwait(false);
        }
    }
}
