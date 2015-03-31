using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb.Middlewares
{
    public class InternalServerErrorMiddleware : OwinMiddleware
    {
        public InternalServerErrorMiddleware(OwinMiddleware next) : base(next) { }

        public override Task Invoke(IOwinContext context)
        {
            return this.Next.Invoke(context)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        var exception = t.Exception.InnerExceptions.Count > 1
                            ? t.Exception : t.Exception.InnerException;
                        if (!(exception is NuGetUtilityException)) // known error
                            Trace.TraceError("{0}: {1}", context.Request.Path, exception);
                        return context.Response.Error(500, new ErrorModel(
                            "Internal Server Error", detail: exception.ToString()));
                    }
                    return Task.FromResult(true);
                })
                .Unwrap();
        }
    }
}
