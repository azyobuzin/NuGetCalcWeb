using System.Threading.Tasks;
using Microsoft.Owin;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using NuGetCalcWeb.RazorSupport;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb.Middlewares
{
    public class CompatibilityMiddleware : OwinMiddleware
    {
        public CompatibilityMiddleware(OwinMiddleware next) : base(next) { }

        public override async Task Invoke(IOwinContext context)
        {
            var q = context.Request.Query;
            var hash = q["hash"];
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
            var statusCode = 200;

            try
            {
                PackageFolderReader package;

                if (string.IsNullOrEmpty(hash))
                {
                    if (string.IsNullOrWhiteSpace(packageId))
                    {
                        // Homepage of NuGetCalc Online
                        await context.Response.View("CompatibilityStatic", model.PackageSelector).ConfigureAwait(false);
                        return;
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

                    var packageDir = NuGetUtility.GetPackage(source, packageId, nugetVersion).Result; // avoid compile error on mono
                    package = new PackageFolderReader(packageDir);
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
                    // NuGetFramework.Parse will throw only ArgumentNullException
                    var nugetFramework = NuGetFramework.Parse(targetFramework);

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
            await context.Response.View("Compatibility", model).ConfigureAwait(false);
        }
    }
}
