﻿using NuGet.Packaging.Core;

namespace NuGetCalcWeb.ViewModels
{
    public class PackageSelectorModel
    {
        public string DefaultSource { get; set; }
        public string DefaultPackageId { get; set; }
        public string DefaultVersion { get; set; }
        public string DefaultTargetFramework { get; set; }
        public string UploadHash { get; set; }
        public PackageIdentity UploadedPackage { get; set; }

        public bool IsUploaded => this.UploadHash != null && this.UploadedPackage != null;
    }
}