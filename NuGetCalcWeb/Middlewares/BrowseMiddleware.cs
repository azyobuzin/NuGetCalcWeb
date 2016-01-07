using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azyobuzi.OwinRazor;
using Microsoft.Owin;
using Microsoft.Owin.Builder;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Microsoft.Owin.StaticFiles.ContentTypes;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetCalcWeb.ViewModels;
using NuGetCalcWeb.ViewModels.FilePreview;
using Owin;

namespace NuGetCalcWeb.Middlewares
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class BrowseMiddleware : OwinMiddleware
    {
        public BrowseMiddleware(OwinMiddleware next, IAppBuilder app)
            : base(next)
        {
            var downloadDir = Path.Combine("App_Data", "packages");
            Directory.CreateDirectory(downloadDir); // Prevent error from FileSystem
            this.downloadApp = app.New()
                .UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString("/browse"),
                    FileSystem = new PhysicalFileSystem(downloadDir),
                    ServeUnknownFileTypes = true
                })
                .Use<NotFoundMiddleware>()
                .Build();

            var filePreviewDir = Path.Combine("App_Data", "html");
            Directory.CreateDirectory(filePreviewDir);
            this.filePreviewApp = app.New()
                .UseStaticFiles(new StaticFileOptions
                {
                    RequestPath = new PathString("/browse"),
                    FileSystem = new PhysicalFileSystem(filePreviewDir),
                    ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>()),
                    ServeUnknownFileTypes = true,
                    DefaultContentType = "text/html"
                })
                .Build();
        }

        private readonly AppFunc downloadApp;
        private readonly AppFunc filePreviewApp;

        public override Task Invoke(IOwinContext context)
        {
            var path = context.Request.Path.Value;

            if (path == "/browse")
                return Browse(context);

            if (context.Request.Query["dl"] == "true")
                return this.downloadApp(context.Environment);

            if (context.RespondNotModified()) return Task.FromResult(true);

            var m = Regex.Match(path, @"^/browse/repositories/([a-zA-Z0-9\+\-]+)/([^/]+)/(.*)$");
            if (m.Success)
            {
                return this.BrowseRepositories(context,
                    m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            }

            m = Regex.Match(path, @"^/browse/upload/([a-zA-Z0-9\+\-]+)/(.*)$");
            if (m.Success)
            {
                return this.BrowseUpload(context,
                    m.Groups[1].Value, m.Groups[2].Value);
            }

            return this.Next.Invoke(context);
        }

        private static async Task Browse(IOwinContext context)
        {
            string redirectTo;

            var q = context.Request.Query;
            var hash = q["hash"];

            if (string.IsNullOrEmpty(hash))
            {
                string error = null;
                var source = q["source"];
                var packageId = q["packageId"];
                var version = q["version"];
                NuGetVersion nugetVersion = null;

                if (string.IsNullOrWhiteSpace(packageId))
                    error = "Package ID is required";
                else if (!string.IsNullOrWhiteSpace(version) && !NuGetVersion.TryParse(version, out nugetVersion))
                    error = "Version is not valid as NuGetVersion";

                if (error != null)
                {
                    await context.Response.Error(400, new ErrorModel(error)).ConfigureAwait(false);
                    return;
                }

                var packageDir = await NuGetUtility.GetPackage(source, packageId, nugetVersion).ConfigureAwait(false);
                var s = packageDir.FullName.Split(Path.DirectorySeparatorChar);
                redirectTo = string.Format("repositories/{0}/{1}/",
                    s[s.Length - 2], // repository hash
                    s[s.Length - 1] // package name and version
                );
            }
            else
            {
                redirectTo = string.Concat("upload/", hash, "/");
            }

            context.Response.StatusCode = 303;
            context.Response.Headers.Set("Location", string.Concat(
                context.Request.Uri.GetLeftPart(UriPartial.Path),
                "/",
                redirectTo
            ));
        }

        private Task BrowseRepositories(IOwinContext context, string hash, string packageName, string path)
        {
            return this.BrowseImpl(context,
                new DirectoryInfo(Path.Combine("App_Data", "packages", "repositories", hash, packageName)), path);
        }

        private Task BrowseUpload(IOwinContext context, string hash, string path)
        {
            return this.BrowseImpl(context, NuGetUtility.GetUploadedPackage(hash), path);
        }

        private async Task BrowseImpl(IOwinContext context, DirectoryInfo root, string path)
        {
            var s = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Any(x => x.Length > 0 && x.All(c => c == '.')))
            {
                await context.Response.Error(404, new ErrorModel("Illegal Path")).ConfigureAwait(false);
                return;
            }

            using (var package = new PackageFolderReader(root))
            {
                if (path == "" || path.EndsWith("/", StringComparison.Ordinal))
                {
                    var dir = new DirectoryInfo(Path.Combine(root.FullName, Path.Combine(s)));
                    if (!dir.Exists)
                        goto NOT_FOUND;

                    await context.Response.View(new Views.FileList(), new FileListModel
                    {
                        Identity = package.GetIdentity(),
                        Breadcrumbs = s,
                        Directories = dir.EnumerateDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)),
                        Files = dir.EnumerateFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                    }).ConfigureAwait(false);
                }
                else
                {
                    var file = new FileInfo(Path.Combine(root.FullName, Path.Combine(s)));
                    if (!file.Exists)
                        goto NOT_FOUND;

                    var gen = new FilePreviewGenerator(file);
                    if (gen.NeedsGenerate)
                    {
                        await gen.GenerateHtml(context, new HeaderModel
                        {
                            Identity = package.GetIdentity(),
                            Breadcrumbs = s
                        }).ConfigureAwait(false);
                    }

                    context.Request.CallCancelled.ThrowIfCancellationRequested();
                    await this.filePreviewApp(context.Environment).ConfigureAwait(false);
                }
            }

            GC.Collect(); // to release a handle of nuspec file
            return;

            NOT_FOUND:
            await this.Next.Invoke(context).ConfigureAwait(false);
        }
    }
}
