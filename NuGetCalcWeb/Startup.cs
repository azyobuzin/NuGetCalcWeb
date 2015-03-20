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
            app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString("/content"),
                FileSystem = new PhysicalFileSystem("content")
            });

            app.Use<NuGetCalcWebMiddleware>();
        }
    }
}