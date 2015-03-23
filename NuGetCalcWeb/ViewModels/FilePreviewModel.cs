using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NuGet.PackagingCore;

namespace NuGetCalcWeb.ViewModels
{
    public class FilePreviewModel
    {
        public PackageIdentity Identity { get; set; }
        public string[] Breadcrumbs { get; set; }
        public string Content { get; set; }
    }
}