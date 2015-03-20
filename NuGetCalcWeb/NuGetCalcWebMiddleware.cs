using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Frameworks;
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
            try
            {
                switch (context.Request.Path.Value)
                {
                    case "/":
                        this.Index(context);
                        return;
                    case "/compatibility":
                        await this.Compatibility(context).ConfigureAwait(false);
                        return;
                }

                error.StatusCode = 404;
                error.Header = "Not Found";
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}: {1}", context.Request.Path, ex);
                error.StatusCode = 500;
                error.Header = "Internal Server Error";
                error.Detail = ex.ToString();
            }

            context.Response.StatusCode = error.StatusCode;

            var accept = context.Request.Headers.GetCommaSeparatedValues("Accept");
            var isHtmlRequired = accept != null
                && accept.Any(x => x.StartsWith("text/html", StringComparison.OrdinalIgnoreCase));
            if (isHtmlRequired)
                context.Response.View("Error", error);
            else
                context.Response.Json(error);
        }

        private void Index(IOwinContext context)
        {
            //TODO: caching
            context.Response.View("Index", new PackageSelectorModel());
        }

        private async Task Compatibility(IOwinContext context)
        {
            var q = context.Request.Query;
            var source = q["source"];
            var packageId = q["packageId"];
            var version = q["version"];
            var targetFramework = q["targetFramework"];

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

            if (string.IsNullOrWhiteSpace(packageId))
            {
                model.Error = "Package ID is required.";
                goto RESPOND;
            }
            if (string.IsNullOrWhiteSpace(targetFramework))
            {
                model.Error = "Target Framework is required.";
                goto RESPOND;
            }

            NuGetVersion nugetVersion = null;
            if (!string.IsNullOrWhiteSpace(version) && !NuGetVersion.TryParse(version, out nugetVersion))
            {
                model.Error = "Version is not valid as NuGetVersion.";
                goto RESPOND;
            }

            // NuGetFramework.Parse will throw only ArgumentNullException
            var nugetFramework = NuGetFramework.Parse(targetFramework);

            try
            {
                using (var package = await NuGetUtility.GetPackage(source, packageId, nugetVersion).ConfigureAwait(false))
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
                model.Error = ex.Message;
                if (ex.InnerException != null)
                    model.Exception = ex.InnerException;
            }

        RESPOND:
            context.Response.View("Compatibility", model);
        }
    }
}