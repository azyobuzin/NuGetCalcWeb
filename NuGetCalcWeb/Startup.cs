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
            app.Use<NuGetCalcWebMiddleware>()
                .UseStaticFiles(new StaticFileOptions()
                {
                    RequestPath = new PathString("/content"),
                    FileSystem = new PhysicalFileSystem("content")
                })
                .Use((ctx, next) =>
                {
                    ctx.Set("NuGetCalcWeb#noStaticFile", true);
                    return next();
                });
        }
    }
}