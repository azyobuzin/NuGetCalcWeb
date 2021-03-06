﻿using System;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using NuGetCalcWeb.Middlewares;
using Owin;

[assembly: OwinStartup(typeof(NuGetCalcWeb.Startup))]

namespace NuGetCalcWeb
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var utcNow = DateTime.UtcNow;
            app.Use<InternalServerErrorMiddleware>()
                .MapWhen(
                    ctx => ctx.Request.Path.Value == "/",
                    b => b.Use<CacheControlMiddleware>(utcNow)
                        .Use<IndexMiddleware>()
                )
                .MapWhen(
                    ctx => ctx.Request.Path.Value == "/compatibility",
                    b => b.Use<CacheControlMiddleware>(utcNow)
                        .Use<CompatibilityMiddleware>()
                )
                .MapWhen(
                    ctx => ctx.Request.Path.StartsWithSegments(new PathString("/browse")),
                    b => b.Use<CacheControlMiddleware>(utcNow)
                        .Use<BrowseMiddleware>(b.New())
                        .Use<NotFoundMiddleware>()
                )
                .MapWhen(
                    ctx => ctx.Request.Path.Value == "/upload",
                    b => b.Use<UploadMiddleware>()
                )
                .Map("/content", b =>
                    b.UseStaticFiles(new StaticFileOptions
                    {
                        FileSystem = new PhysicalFileSystem("content")
                    })
                    .Use<NotFoundMiddleware>()
                )
                .Use<NotFoundMiddleware>();
        }
    }
}