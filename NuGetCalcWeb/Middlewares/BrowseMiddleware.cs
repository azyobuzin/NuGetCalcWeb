using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb.Middlewares
{
    public class BrowseMiddleware : OwinMiddleware
    {
        public BrowseMiddleware(OwinMiddleware next) : base(next) { }

        public override Task Invoke(IOwinContext context)
        {
            var path = context.Request.Path.Value;

            if (path == "/browse")
                return Browse(context);

            var m = Regex.Match(path, @"^/browse/repositories/([a-zA-Z0-9\+\-]+)/([^/]+)/(.*)$");
            if (m.Success)
            {
                return BrowseRepositories(context,
                    m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
            }

            m = Regex.Match(path, @"^/browse/upload/([a-zA-Z0-9\+\-]+)/(.*)$");
            if (m.Success)
            {
                return BrowseUpload(context,
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

                try
                {
                    var packageDir = await NuGetUtility.GetPackage(source, packageId, nugetVersion).ConfigureAwait(false);
                    var s = packageDir.FullName.Split(Path.DirectorySeparatorChar);
                    redirectTo = string.Format("repositories/{0}/{1}/",
                        s[s.Length - 2], // repository hash
                        s[s.Length - 1] // package name and version
                    );
                }
                catch (NuGetUtilityException ex)
                {
                    context.Response.Error(500, new ErrorModel(
                        "Error while downloading the package",
                        detail: ex.ToString()
                    )).Wait(); //TODO
                    return;
                }
            }
            else
            {
                redirectTo = string.Format("upload/{0}/", hash);
            }

            context.Response.StatusCode = 303;
            context.Response.Headers.Set("Location", string.Format("{0}/{1}",
                context.Request.Uri.GetLeftPart(UriPartial.Path),
                redirectTo
            ));
        }

        private Task BrowseRepositories(IOwinContext context, string hash, string packageName, string path)
        {
            return this.BrowseImpl(context,
                new DirectoryInfo(Path.Combine("App_Data", "repositories", hash, packageName)), path);
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
                if (path == "" || path.EndsWith("/"))
                {
                    var dir = new DirectoryInfo(Path.Combine(root.FullName, Path.Combine(s)));
                    if (!dir.Exists)
                        goto NOT_FOUND;

                    await context.Response.View("FileList", new FileListModel()
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

                    if (context.Request.Query["dl"] == "true")
                    {
                        //TODO: cache, Content-Range
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.ContentLength = file.Length;
                        await context.Response.SendFileAsync(file.FullName).ConfigureAwait(false);
                    }
                    else
                    {
                        await context.Response.View("FilePreview", new FilePreviewModel()
                        {
                            Identity = package.GetIdentity(),
                            Breadcrumbs = s,
                            Content = await FilePreviewGenerator.GenerateHtml(file).ConfigureAwait(false)
                        }).ConfigureAwait(false);
                    }
                }
            }

            return;

        NOT_FOUND:
            await this.Next.Invoke(context).ConfigureAwait(false);
        }
    }
}
