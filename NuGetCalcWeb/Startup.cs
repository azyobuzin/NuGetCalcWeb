using System.IO;
using System.Reflection;
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
            app.Use<IndexMiddleware>();

            app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString("/content"),
                FileSystem = new PhysicalFileSystem(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "content"))
            });

            app.UseLightNode(new LightNodeOptions(AcceptVerbs.Get, new JsonNetContentFormatter())
            {
                ErrorHandlingPolicy = ErrorHandlingPolicy.ReturnInternalServerErrorIncludeErrorDetails,
                OperationMissingHandlingPolicy = OperationMissingHandlingPolicy.ReturnErrorStatusCodeIncludeErrorDetails,
                UseOtherMiddleware = true
            });
        }
    }
}