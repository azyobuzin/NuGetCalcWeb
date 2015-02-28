using LightNode.Formatter;
using LightNode.Server;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

[assembly: OwinStartup(typeof(NuGetCalcWeb.Startup))]

namespace NuGetCalcWeb
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Use<NuGetCalcWebMiddleware>();

            app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString("/content"),
                FileSystem = new PhysicalFileSystem("content")
            });

            app.UseLightNode(new LightNodeOptions(AcceptVerbs.Get, new JsonNetContentFormatter())
            {
                ErrorHandlingPolicy = ErrorHandlingPolicy.ThrowException,
                OperationMissingHandlingPolicy = OperationMissingHandlingPolicy.ThrowException,
                UseOtherMiddleware = true
            });
        }
    }
}