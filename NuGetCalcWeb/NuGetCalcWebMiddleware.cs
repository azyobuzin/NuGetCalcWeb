using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb
{
    public class NuGetCalcWebMiddleware : OwinMiddleware
    {
        public NuGetCalcWebMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            var error = new ErrorModel();
            int statusCode;
            try
            {
                var path = context.Request.Path.Value;
                switch (path)
                {
                    case "/":
                        Index(context);
                        return;
                    case "/compatibility":
                        await Compatibility(context).ConfigureAwait(false);
                        return;
                    case "/upload":
                        await Upload(context).ConfigureAwait(false);
                        return;
                    case "/browse":
                        await Browse(context).ConfigureAwait(false);
                        return;
                }

                var m = Regex.Match(path, @"^/browse/repositories/([a-zA-Z0-9\+\-]+)/([^/]+)/(.*)$");
                if (m.Success)
                {
                    await BrowseRepositories(context,
                        m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value).ConfigureAwait(false);
                    return;
                }

                m = Regex.Match(path, @"^/browse/upload/([a-zA-Z0-9\+\-]+)/(.*)$");
                if (m.Success)
                {
                    await BrowseUpload(context,
                        m.Groups[1].Value, m.Groups[2].Value).ConfigureAwait(false);
                    return;
                }

                await this.Next.Invoke(context).ConfigureAwait(false);
                if (!context.Get<bool>("NuGetCalcWeb#noStaticFile")) return;

                statusCode = 404;
                error.Header = "Not Found";
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}: {1}", context.Request.Path, ex);
                statusCode = 500;
                error.Header = "Internal Server Error";
                error.Detail = ex.ToString();
            }

            context.Response.StatusCode = statusCode;

            var accept = context.Request.Headers.GetCommaSeparatedValues("Accept");
            var isHtmlRequired = accept != null
                && accept.Any(x => x.StartsWith("text/html", StringComparison.OrdinalIgnoreCase));
            if (isHtmlRequired)
                context.Response.View("Error", error);
            else
                context.Response.Json(error);
        }

        private static void Index(IOwinContext context)
        {
            //TODO: caching
            context.Response.View("Index", new PackageSelectorModel());
        }

        private static async Task Compatibility(IOwinContext context)
        {
            var q = context.Request.Query;
            var hash = q["hash"];
            var source = q["source"];
            var packageId = q["packageId"];
            var version = q["version"];
            var targetFramework = q["targetFramework"];

            // NuGetFramework.Parse will throw only ArgumentNullException
            var nugetFramework = NuGetFramework.Parse(targetFramework);

            var model = new CompatibilityModel()
            {
                PackageSelector = new PackageSelectorModel()
                {
                    DefaultSource = source,
                    DefaultPackageId = packageId,
                    DefaultVersion = version,
                    DefaultTargetFramework = targetFramework
                }
            };
            var statusCode = 200;

            try
            {
                PackageFolderReader package;

                if (string.IsNullOrEmpty(hash))
                {
                    if (string.IsNullOrWhiteSpace(packageId))
                    {
                        statusCode = 400;
                        model.Error = "Package ID is required.";
                        goto RESPOND;
                    }
                    if (string.IsNullOrWhiteSpace(targetFramework))
                    {
                        statusCode = 400;
                        model.Error = "Target Framework is required.";
                        goto RESPOND;
                    }

                    NuGetVersion nugetVersion = null;
                    if (!string.IsNullOrWhiteSpace(version) && !NuGetVersion.TryParse(version, out nugetVersion))
                    {
                        statusCode = 400;
                        model.Error = "Version is not valid as NuGetVersion.";
                        goto RESPOND;
                    }

                    package = new PackageFolderReader(
                        await NuGetUtility.GetPackage(source, packageId, nugetVersion).ConfigureAwait(false));
                }
                else
                {
                    package = new PackageFolderReader(NuGetUtility.GetUploadedPackage(hash));
                    model.PackageSelector.UploadHash = hash;
                    model.PackageSelector.UploadedPackage = package.GetIdentity();
                }

                using (package)
                {
                    var identity = package.GetIdentity();
                    model.PackageSelector.DefaultPackageId = identity.Id;
                    model.PackageSelector.DefaultVersion = identity.Version.ToString();

                    var referenceItems = NuGetUtility.FindMostCompatibleReferenceGroup(package, nugetFramework);
                    if (referenceItems != null)
                        model.ReferenceAssemblies = referenceItems.Items;

                    var depenencyItems = NuGetUtility.FindMostCompatibleDependencyGroup(package, nugetFramework);
                    if (depenencyItems != null)
                        model.Dependencies = depenencyItems.Packages;
                }
            }
            catch (NuGetUtilityException ex)
            {
                statusCode = 500;
                model.Error = ex.Message;
                if (ex.InnerException != null)
                    model.Exception = ex.InnerException;
            }

        RESPOND:
            context.Response.StatusCode = statusCode;
            context.Response.View("Compatibility", model);
        }

        private static async Task Upload(IOwinContext context)
        {
            var httpContent = new StreamContent(context.Request.Body);
            foreach (var kvp in context.Request.Headers)
                httpContent.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

            if (!httpContent.IsMimeMultipartContent("form-data"))
            {
                context.Response.StatusCode = 415;
                context.Response.View("Error", new ErrorModel()
                {
                    Header = "Unsupported Media Type",
                    Detail = "You must upload nupkgs with multipart/form-data."
                });
                return;
            }

            var provider = await httpContent
                .ReadAsMultipartAsync(new MultipartFormDataStreamProvider(Path.GetTempPath()))
                .ConfigureAwait(false);
            var formData = provider.FormData;

            var hash = formData["hash"];
            var method = formData["method"];
            if (method == null)
            {
                context.Response.StatusCode = 400;
                context.Response.View("Error", new ErrorModel()
                {
                    Header = "Bad Request",
                    Detail = "\"method\" parameter is required."
                });
                return;
            }

            var file = provider.FileData
                .FirstOrDefault(f => f.Headers.ContentDisposition.Name.Trim('"') == "file");

            if (file != null && new FileInfo(file.LocalFileName).Length > 0)
            {
                try
                {
                    hash = await NuGetUtility.ExtractUploadedFile(file).ConfigureAwait(false);
                }
                catch (NuGetUtilityException ex)
                {
                    context.Response.StatusCode = 500;
                    context.Response.View("Error", new ErrorModel()
                    {
                        Header = "Error while extracting the package",
                        Detail = ex.ToString()
                    });
                    return;
                }
            }
            else if (string.IsNullOrEmpty(hash))
            {
                context.Response.StatusCode = 400;
                context.Response.View("Error", new ErrorModel()
                {
                    Header = "Bad Request",
                    Detail = "\"file\" parameter is required."
                });
                return;
            }

            var redirectUri = new UriBuilder(new Uri(context.Request.Uri, method));
            redirectUri.Query = string.Join("&",
                Enumerable.Range(0, formData.Count)
                    .Select(i => Tuple.Create(formData.GetKey(i), formData.Get(i)))
                    .Where(t => t.Item1 != "method" && t.Item1 != "hash" && !string.IsNullOrEmpty(t.Item2))
                    .Concat(new[] { Tuple.Create("hash", hash) })
                    .Select(t => string.Format("{0}={1}", Uri.EscapeDataString(t.Item1), Uri.EscapeDataString(t.Item2)))
            );

            context.Response.StatusCode = 303;
            context.Response.Headers.Set("Location", redirectUri.ToString());
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
                    context.Response.StatusCode = 400;
                    context.Response.View("Error", new ErrorModel() { Header = error });
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
                    context.Response.StatusCode = 500;
                    context.Response.View("Error", new ErrorModel()
                    {
                        Header = "Error while downloading the package",
                        Detail = ex.ToString()
                    });
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

        private static Task BrowseRepositories(IOwinContext context, string hash, string packageName, string path)
        {
            return BrowseImpl(context,
                new DirectoryInfo(Path.Combine("App_Data", "repositories", hash, packageName)), path);
        }

        private static Task BrowseUpload(IOwinContext context, string hash, string path)
        {
            return BrowseImpl(context, NuGetUtility.GetUploadedPackage(hash), path);
        }

        private static async Task BrowseImpl(IOwinContext context, DirectoryInfo root, string path)
        {
            var s = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Any(x => x.Length > 0 && x.All(c => c == '.')))
            {
                context.Response.StatusCode = 404;
                context.Response.View("Error", new ErrorModel() { Header = "Illegal Path" });
                return;
            }

            using (var package = new PackageFolderReader(root))
            {
                if (path == "" || path.EndsWith("/"))
                {
                    var dir = new DirectoryInfo(Path.Combine(root.FullName, Path.Combine(s)));
                    if (!dir.Exists)
                        goto NOT_FOUND;

                    context.Response.View("FileList", new FileListModel()
                    {
                        Identity = package.GetIdentity(),
                        Breadcrumbs = s,
                        Directories = dir.EnumerateDirectories().Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden)),
                        Files = dir.EnumerateFiles().Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden))
                    });
                }
                else
                {
                    var file = new FileInfo(Path.Combine(root.FullName, Path.Combine(s)));
                    if (!file.Exists)
                        goto NOT_FOUND;

                    context.Response.View("FilePreview", new FilePreviewModel()
                    {
                        Identity = package.GetIdentity(),
                        Breadcrumbs = s,
                        Content = await FilePreviewGenerator.GenerateHtml(file).ConfigureAwait(false)
                    });
                }
            }

            return;

        NOT_FOUND:
            context.Response.StatusCode = 404;
            context.Response.View("Error", new ErrorModel() { Header = "Not Found" });
        }
    }
}