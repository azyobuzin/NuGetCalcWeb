using NuGet.Packaging.Core;

namespace NuGetCalcWeb.ViewModels.FilePreview
{
    public class HeaderModel
    {
        public PackageIdentity Identity { get; set; }
        public string[] Breadcrumbs { get; set; }
    }
}