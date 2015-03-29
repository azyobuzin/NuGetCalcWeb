using System.Threading.Tasks;
using Microsoft.Owin;
using NuGetCalcWeb.RazorSupport;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb.Middlewares
{
    public class NotFoundMiddleware : OwinMiddleware
    {
        public NotFoundMiddleware(OwinMiddleware next) : base(next) { }

        public override Task Invoke(IOwinContext context)
        {
            return context.Response.Error(404, new ErrorModel("Not Found"));
        }
    }
}
